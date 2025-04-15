using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;

namespace FNBReservation.Portal.Services
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        private readonly IJSRuntime _jsRuntime;
        private readonly JwtTokenService _tokenService;

        public CustomAuthStateProvider(IJSRuntime jsRuntime, JwtTokenService tokenService = null)
        {
            _jsRuntime = jsRuntime;
            _tokenService = tokenService;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                // Try to get user info from token in HTTP-only cookie
                if (_tokenService != null)
                {
                    try
                    {
                        // Check if we have valid tokens in cookies
                        var isValid = await _tokenService.IsTokenValidAsync();
                        if (isValid)
                        {
                            // Get user info from the token
                            var userInfo = await _tokenService.GetUserInfoFromTokenAsync();
                            if (userInfo != null)
                            {
                                var claims = new List<Claim>
                                {
                                    new Claim(ClaimTypes.Name, userInfo.Username),
                                    new Claim(ClaimTypes.Role, userInfo.Role)
                                };

                                if (!string.IsNullOrEmpty(userInfo.UserId))
                                {
                                    claims.Add(new Claim(ClaimTypes.NameIdentifier, userInfo.UserId));
                                }

                                var identity = new ClaimsIdentity(claims, "FNBReservation");
                                var user = new ClaimsPrincipal(identity);
                                
                                return new AuthenticationState(user);
                            }
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // This happens during static rendering when JS interop is not available
                        return new AuthenticationState(_anonymous);
                    }
                }
                
                // Fallback to check local storage (temporary during migration to HTTP-only cookies)
                try
                {
                    var storedPrincipal = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authData");
                    
                    if (string.IsNullOrEmpty(storedPrincipal))
                    {
                        return new AuthenticationState(_anonymous);
                    }

                    try
                    {
                        var authData = JsonSerializer.Deserialize<AuthData>(storedPrincipal);
                        
                        if (authData == null)
                        {
                            return new AuthenticationState(_anonymous);
                        }

                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, authData.Username),
                            new Claim(ClaimTypes.Role, authData.Role)
                        };
                        
                        var identity = new ClaimsIdentity(claims, "FNBReservation");
                        var user = new ClaimsPrincipal(identity);
                        
                        return new AuthenticationState(user);
                    }
                    catch (Exception ex)
                    {
                        await _jsRuntime.InvokeVoidAsync("console.log", "Error deserializing auth data: " + ex.Message);
                        return new AuthenticationState(_anonymous);
                    }
                }
                catch (InvalidOperationException)
                {
                    // This happens during static rendering when JS interop is not available
                    return new AuthenticationState(_anonymous);
                }
            }
            catch
            {
                // If there's any error, return anonymous principal
                return new AuthenticationState(_anonymous);
            }
        }

        public async Task<ClaimsPrincipal> GetAuthenticatedUserFromStorageAsync()
        {
            try
            {
                // First try to get user info from HTTP-only cookie token
                if (_tokenService != null)
                {
                    try
                    {
                        // Check if we have valid tokens in cookies
                        var isValid = await _tokenService.IsTokenValidAsync();
                        if (isValid)
                        {
                            // Get user info from the token
                            var userInfo = await _tokenService.GetUserInfoFromTokenAsync();
                            if (userInfo != null)
                            {
                                var userClaims = new List<Claim>
                                {
                                    new Claim(ClaimTypes.Name, userInfo.Username),
                                    new Claim(ClaimTypes.Role, userInfo.Role)
                                };

                                if (!string.IsNullOrEmpty(userInfo.UserId))
                                {
                                    userClaims.Add(new Claim(ClaimTypes.NameIdentifier, userInfo.UserId));
                                }

                                var userIdentity = new ClaimsIdentity(userClaims, "FNBReservation");
                                var userPrincipal = new ClaimsPrincipal(userIdentity);
                                
                                // Update the authentication state and notify
                                UpdateAuthenticationState(new AuthenticationState(userPrincipal));
                                
                                return userPrincipal;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        await _jsRuntime.InvokeVoidAsync("console.log", "Error getting user from token: " + ex.Message);
                    }
                }
                
                // Fallback to localStorage for backward compatibility
                var storedPrincipal = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authData");
                
                await _jsRuntime.InvokeVoidAsync("console.log", "GetAuthState - Stored auth data: " + 
                    (string.IsNullOrEmpty(storedPrincipal) ? "none" : "exists"));
                
                if (string.IsNullOrEmpty(storedPrincipal))
                {
                    return _anonymous;
                }

                try
                {
                    var authData = JsonSerializer.Deserialize<AuthData>(storedPrincipal);
                    
                    if (authData == null)
                    {
                        await _jsRuntime.InvokeVoidAsync("console.log", "Auth data exists but could not be deserialized");
                        return _anonymous;
                    }

                    await _jsRuntime.InvokeVoidAsync("console.log", $"Auth data for user: {authData.Username}, Role: {authData.Role}");
                    
                    // Create a new identity and user principal
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, authData.Username),
                        new Claim(ClaimTypes.Role, authData.Role)
                    };

                    var identity = new ClaimsIdentity(claims, "FNBReservation");
                    var user = new ClaimsPrincipal(identity);
                    
                    // Update the authentication state and notify
                    UpdateAuthenticationState(new AuthenticationState(user));
                    
                    return user;
                }
                catch (Exception ex)
                {
                    await _jsRuntime.InvokeVoidAsync("console.log", "Error deserializing auth data: " + ex.Message);
                    return _anonymous;
                }
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.log", "Error in GetAuthenticatedUserFromStorage: " + ex.Message);
                return _anonymous;
            }
        }

        public void NotifyUserAuthentication(string username, string role, string accessToken = null, string refreshToken = null)
        {
            try
            {
                // Store minimal user info in localStorage for UI 
                // The actual tokens are stored as HTTP-only cookies by the backend
                var authData = new AuthData 
                { 
                    Username = username, 
                    Role = role
                    // No need to store tokens here anymore
                };
                var serializedData = JsonSerializer.Serialize(authData);
                
                try
                {
                    _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authData", serializedData);
                    _jsRuntime.InvokeVoidAsync("console.log", "User authenticated: " + username);
                }
                catch (InvalidOperationException)
                {
                    // This happens during static rendering - we'll skip the JS interop
                }
                
                // Create and update the claims identity
                var identity = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.Role, role)
                }, "FNBReservation");
                
                var user = new ClaimsPrincipal(identity);
                
                // Notify the auth state changed
                UpdateAuthenticationState(new AuthenticationState(user));
            }
            catch (Exception ex)
            {
                try
                {
                    _jsRuntime.InvokeVoidAsync("console.log", "Error in NotifyUserAuthentication: " + ex.Message);
                }
                catch
                {
                    // Ignore JS interop errors during static rendering
                }
            }
        }

        public void NotifyUserLogout()
        {
            try
            {
                // Clear localStorage
                try
                {
                    _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authData");
                    _jsRuntime.InvokeVoidAsync("console.log", "User logged out");
                }
                catch (InvalidOperationException)
                {
                    // This happens during static rendering - we'll skip the JS interop
                }
                
                // Update auth state
                UpdateAuthenticationState(new AuthenticationState(_anonymous));
            }
            catch (Exception ex)
            {
                try
                {
                    _jsRuntime.InvokeVoidAsync("console.log", "Error in NotifyUserLogout: " + ex.Message);
                }
                catch
                {
                    // Ignore JS interop errors during static rendering
                }
            }
        }

        // A public wrapper method to use the protected NotifyAuthenticationStateChanged method
        public void UpdateAuthenticationState(AuthenticationState state)
        {
            NotifyAuthenticationStateChanged(Task.FromResult(state));
        }
    }

    public class AuthData
    {
        public string Username { get; set; }
        public string Role { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
}