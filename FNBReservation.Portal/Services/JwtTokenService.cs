using Microsoft.JSInterop;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http;

namespace FNBReservation.Portal.Services
{
    public class JwtTokenService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _refreshTokenEndpoint;

        public JwtTokenService(IJSRuntime jsRuntime, HttpClient httpClient, IConfiguration configuration)
        {
            _jsRuntime = jsRuntime;
            _httpClient = httpClient; // Use the basic HttpClient
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000/";
            _refreshTokenEndpoint = configuration["ApiSettings:AuthEndpoints:RefreshToken"] ?? "api/v1/auth/refresh-token";
        }

        public async Task<string> GetAccessTokenAsync()
        {
            try
            {
                // Check if the user is authenticated with cookies first
                var isAuthenticated = await _jsRuntime.InvokeAsync<bool>("authHelpers.isAuthenticated");
                
                if (isAuthenticated)
                {
                    // With HTTP-only cookies, we can't directly read the token value
                    // but we can check if user is authenticated
                    await _jsRuntime.InvokeVoidAsync("console.log", "GetAccessTokenAsync: User is authenticated via cookies");
                    
                    // Return a placeholder string to indicate authentication is valid
                    // The actual token isn't needed as it will be sent automatically with requests
                    return "http-only-token-present";
                }
                
                // For backward compatibility, try to get from localStorage
                var authData = await _jsRuntime.InvokeAsync<string>("authHelpers.getAuthData");
                if (!string.IsNullOrEmpty(authData))
                {
                    try 
                    {
                        var tokenData = JsonSerializer.Deserialize<TokenData>(authData);
                        if (tokenData != null && !string.IsNullOrEmpty(tokenData.AccessToken))
                        {
                            await _jsRuntime.InvokeVoidAsync("console.log", "GetAccessTokenAsync: Using token from localStorage");
                            return tokenData.AccessToken;
                        }
                    }
                    catch (Exception ex)
                    {
                        await _jsRuntime.InvokeVoidAsync("console.log", "Error parsing auth data: " + ex.Message);
                    }
                }
                
                await _jsRuntime.InvokeVoidAsync("console.log", "GetAccessTokenAsync: No authentication found");
                return null;
            }
            catch (InvalidOperationException)
            {
                // This happens during static rendering
                return null;
            }
            catch (Exception ex)
            {
                try
                {
                    await _jsRuntime.InvokeVoidAsync("console.log", "Error getting access token: " + ex.Message);
                }
                catch (InvalidOperationException)
                {
                    // Ignore JS interop errors during static rendering
                }
                return null;
            }
        }

        public async Task<string> GetRefreshTokenAsync()
        {
            try
            {
                // Check if the user is authenticated with cookies first
                var isAuthenticated = await _jsRuntime.InvokeAsync<bool>("authHelpers.isAuthenticated");
                
                if (isAuthenticated)
                {
                    // With HTTP-only cookies, we can't directly read the token value
                    // but we can check if user is authenticated
                    await _jsRuntime.InvokeVoidAsync("console.log", "GetRefreshTokenAsync: User is authenticated via cookies");
                    
                    // Return a placeholder string to indicate refresh token is present
                    return "http-only-refresh-token-present";
                }
                
                // For backward compatibility, try to get from localStorage
                var authData = await _jsRuntime.InvokeAsync<string>("authHelpers.getAuthData");
                if (!string.IsNullOrEmpty(authData))
                {
                    try 
                    {
                        var tokenData = JsonSerializer.Deserialize<TokenData>(authData);
                        if (tokenData != null && !string.IsNullOrEmpty(tokenData.RefreshToken))
                        {
                            await _jsRuntime.InvokeVoidAsync("console.log", "GetRefreshTokenAsync: Using refresh token from localStorage");
                            return tokenData.RefreshToken;
                        }
                    }
                    catch (Exception ex)
                    {
                        await _jsRuntime.InvokeVoidAsync("console.log", "Error parsing auth data: " + ex.Message);
                    }
                }
                
                await _jsRuntime.InvokeVoidAsync("console.log", "GetRefreshTokenAsync: No authentication found");
                return null;
            }
            catch (InvalidOperationException)
            {
                // This happens during static rendering
                return null;
            }
            catch (Exception ex)
            {
                try
                {
                    await _jsRuntime.InvokeVoidAsync("console.log", "Error getting refresh token: " + ex.Message);
                }
                catch
                {
                    // Ignore JS interop errors
                }
                return null;
            }
        }

        public async Task<bool> IsTokenValidAsync()
        {
            try 
            {
                // Check if authenticated with HTTP-only cookies
                var isAuthenticated = await _jsRuntime.InvokeAsync<bool>("authHelpers.isAuthenticated");
                
                if (isAuthenticated)
                {
                    // If we detect cookies, assume it's valid and let the server validate
                    // The server will return 401 if not valid and we'll handle it then
                    await _jsRuntime.InvokeVoidAsync("console.log", "IsTokenValidAsync: User has authentication cookies");
                    return true;
                }
                
                var token = await GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    await _jsRuntime.InvokeVoidAsync("console.log", "IsTokenValidAsync: No token found");
                    return false;
                }
                
                // If it's our placeholder for HTTP-only cookies, consider it valid
                if (token == "http-only-token-present")
                {
                    return true;
                }

                // Otherwise, validate the token from localStorage
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jwtToken = handler.ReadJwtToken(token);
                    
                    // Check if token has expired
                    var expiry = jwtToken.ValidTo;
                    var isValid = expiry > DateTime.UtcNow;
                    
                    await _jsRuntime.InvokeVoidAsync("console.log", $"Token expiry: {expiry.ToString("yyyy-MM-dd HH:mm:ss")}, Current UTC: {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}, Valid: {isValid}");
                    
                    return isValid;
                }
                catch (Exception ex)
                {
                    await _jsRuntime.InvokeVoidAsync("console.log", $"Error validating token: {ex.Message}");
                    return false;
                }
            }
            catch (InvalidOperationException)
            {
                // This happens during static rendering
                return false;
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"Unexpected error in IsTokenValidAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<RefreshTokenResponse> RefreshTokenAsync()
        {
            try
            {
                // For HTTP-only cookies, the backend will automatically get the refresh token from cookies
                // We don't need to send it in the request body
                
                string fullUrl = $"{_baseUrl.TrimEnd('/')}/{_refreshTokenEndpoint}";
                // Send empty content as refresh token is in cookies
                var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
                
                // Make sure credentials are included (cookies)
                var request = new HttpRequestMessage(HttpMethod.Post, fullUrl);
                request.Content = content;
                // Add header to indicate credentials should be included
                request.Headers.Add("X-Include-Credentials", "true");
                
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var refreshResponse = JsonSerializer.Deserialize<ApiRefreshResponse>(responseContent, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (refreshResponse != null)
                    {
                        // No need to update storage - the backend sets cookies
                        
                        return new RefreshTokenResponse 
                        { 
                            Success = true, 
                            AccessToken = null, // Not needed as we use HTTP-only cookies
                            RefreshToken = null  // Not needed as we use HTTP-only cookies
                        };
                    }
                }

                return new RefreshTokenResponse { Success = false, ErrorMessage = "Failed to refresh token" };
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.log", "Error refreshing token: " + ex.Message);
                return new RefreshTokenResponse { Success = false, ErrorMessage = "An error occurred during token refresh" };
            }
        }

        public async Task<UserInfo> GetUserInfoFromTokenAsync()
        {
            try
            {
                var token = await GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                    return null;
            
                // Handle the HTTP-only cookie placeholder case
                if (token == "http-only-token-present")
                {
                    await _jsRuntime.InvokeVoidAsync("console.log", "GetUserInfoFromTokenAsync: Using HTTP-only cookies, can't read token content");
                    
                    // Try to get user info from localStorage as a fallback
                    var authData = await _jsRuntime.InvokeAsync<string>("authHelpers.getAuthData");
                    if (!string.IsNullOrEmpty(authData))
                    {
                        // Parse the authData string - it's now coming as a string directly from JS
                        try 
                        {
                            var tokenData = JsonSerializer.Deserialize<TokenData>(authData);
                            if (tokenData != null && !string.IsNullOrEmpty(tokenData.Username))
                            {
                                return new UserInfo
                                {
                                    Username = tokenData.Username,
                                    Role = tokenData.Role,
                                    // Other properties if available
                                };
                            }
                        }
                        catch (Exception ex)
                        {
                            await _jsRuntime.InvokeVoidAsync("console.log", "Error parsing token data from localStorage: " + ex.Message);
                            return null;
                        }
                    }
                    
                    // If we can't get user info from localStorage, try to make an API call to get user info
                    // This would require implementing an endpoint on the backend
                    
                    return null;
                }
                
                // Continue with regular JWT token parsing for localStorage tokens
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                var userInfo = new UserInfo
                {
                    Username = jwtToken.Claims.FirstOrDefault(c => c.Type == "unique_name")?.Value 
                        ?? jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value,
                    Role = jwtToken.Claims.FirstOrDefault(c => c.Type == "role")?.Value 
                        ?? jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value,
                    UserId = jwtToken.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value 
                        ?? jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
                };

                return userInfo;
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"Error getting user info from token: {ex.Message}");
                return null;
            }
        }
    }

    public class TokenData
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
    }

    public class UserInfo
    {
        public string Username { get; set; }
        public string Role { get; set; }
        public string UserId { get; set; }
    }

    public class ApiRefreshResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
    }

    public class RefreshTokenResponse
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
} 