using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace FNBReservation.Portal.Services
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly HttpClient _httpClient;
        private readonly IJSRuntime _jsRuntime;
        private readonly ILogger<CustomAuthStateProvider> _logger;
        private readonly JwtTokenService _tokenService;
        private ClaimsPrincipal _currentUser = new ClaimsPrincipal(new ClaimsIdentity());

        public CustomAuthStateProvider(
            HttpClient httpClient,
            IJSRuntime jsRuntime,
            ILogger<CustomAuthStateProvider> logger,
            JwtTokenService tokenService = null)
        {
            _httpClient = httpClient;
            _jsRuntime = jsRuntime;
            _logger = logger;
            _tokenService = tokenService;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                // First check if we have a token in local storage
                if (_tokenService != null)
                {
                    var hasValidToken = await _tokenService.IsTokenValidAsync();
                    if (hasValidToken)
                    {
                        var userInfo = await _tokenService.GetUserInfoAsync();
                        if (userInfo != null && !string.IsNullOrEmpty(userInfo.Username))
                        {
                            var claims = new List<Claim>
                            {
                                new Claim(ClaimTypes.Name, userInfo.Username),
                                new Claim(ClaimTypes.Email, userInfo.Email ?? string.Empty)
                            };
                            
                            // Add roles if available
                            if (userInfo.Roles != null)
                            {
                                foreach (var role in userInfo.Roles)
                                {
                                    claims.Add(new Claim(ClaimTypes.Role, role));
                                }
                            }
                            
                            var identity = new ClaimsIdentity(claims, "FNB Authentication");
                            _currentUser = new ClaimsPrincipal(identity);
                            
                            _logger.LogInformation("User authenticated from token: {Username}", userInfo.Username);
                            return new AuthenticationState(_currentUser);
                        }
                    }
                }

                // If token doesn't exist or is invalid, make a request to the user-info endpoint to check authentication status
                var response = await _httpClient.GetAsync("api/user-info");
                
                if (response.IsSuccessStatusCode)
                {
                    var userInfoJson = await response.Content.ReadAsStringAsync();
                    var userInfo = JsonSerializer.Deserialize<UserInfo>(userInfoJson, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (userInfo != null && !string.IsNullOrEmpty(userInfo.Username))
                    {
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, userInfo.Username),
                            new Claim(ClaimTypes.Email, userInfo.Email ?? string.Empty)
                        };
                        
                        // Add roles if available
                        if (userInfo.Roles != null)
                        {
                            foreach (var role in userInfo.Roles)
                            {
                                claims.Add(new Claim(ClaimTypes.Role, role));
                            }
                        }
                        
                        var identity = new ClaimsIdentity(claims, "FNB Authentication");
                        _currentUser = new ClaimsPrincipal(identity);
                        
                        _logger.LogInformation("User authenticated from API: {Username}", userInfo.Username);
                        
                        // Save the user info to local storage if token service exists
                        if (_tokenService != null)
                        {
                            await _tokenService.SaveUserInfoAsync(userInfo);
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("User not authenticated. Status code: {StatusCode}", response.StatusCode);
                    _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining authentication state");
                _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
            }
            
            return new AuthenticationState(_currentUser);
        }

        public void UpdateAuthenticationState(AuthenticationState state)
        {
            _currentUser = state.User;
            NotifyAuthenticationStateChanged(Task.FromResult(state));
        }

        public void MarkUserAsAuthenticated(string username)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, username)
            }, "FNB Authentication");
            
            var user = new ClaimsPrincipal(identity);
            _currentUser = user;
            
            var authState = Task.FromResult(new AuthenticationState(user));
            NotifyAuthenticationStateChanged(authState);
        }

        public void NotifyUserAuthentication(string username, string role, string accessToken, string refreshToken)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role ?? "User")
            }, "FNB Authentication");
            
            var user = new ClaimsPrincipal(identity);
            _currentUser = user;
            
            var authState = Task.FromResult(new AuthenticationState(user));
            NotifyAuthenticationStateChanged(authState);
            
            // Save tokens if token service exists
            if (_tokenService != null)
            {
                Task.Run(async () => 
                {
                    await _tokenService.SaveTokensAsync(accessToken, refreshToken);
                    await _tokenService.SaveUserInfoAsync(new UserInfo 
                    { 
                        Username = username,
                        Roles = new List<string> { role ?? "User" }
                    });
                });
            }
        }

        public void MarkUserAsLoggedOut()
        {
            var identity = new ClaimsIdentity();
            var user = new ClaimsPrincipal(identity);
            _currentUser = user;
            
            var authState = Task.FromResult(new AuthenticationState(user));
            NotifyAuthenticationStateChanged(authState);
            
            // Clear tokens if token service exists
            if (_tokenService != null)
            {
                Task.Run(async () => await _tokenService.ClearTokensAsync());
            }
        }

        public void NotifyUserLogout()
        {
            MarkUserAsLoggedOut();
        }

        public async Task<UserInfo> GetAuthenticatedUserFromStorageAsync()
        {
            try
            {
                if (_tokenService != null)
                {
                    var userInfo = await _tokenService.GetUserInfoAsync();
                    if (userInfo != null && !string.IsNullOrEmpty(userInfo.Username))
                    {
                        return userInfo;
                    }
                }

                // Fallback to checking the current authentication state
                var authState = await GetAuthenticationStateAsync();
                var user = authState.User;
                
                if (user.Identity?.IsAuthenticated == true)
                {
                    var username = user.Identity.Name;
                    var email = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? string.Empty;
                    var roles = user.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
                    
                    return new UserInfo
                    {
                        Username = username,
                        Email = email,
                        Roles = roles
                    };
                }
                
                return new UserInfo();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving authenticated user from storage");
                return new UserInfo();
            }
        }
    }
}