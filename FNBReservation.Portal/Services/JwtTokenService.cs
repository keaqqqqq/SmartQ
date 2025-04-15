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
                // Get access token from cookie instead of localStorage
                var accessToken = await _jsRuntime.InvokeAsync<string>("getCookie", "accessToken");
                
                if (string.IsNullOrEmpty(accessToken))
                {
                    await _jsRuntime.InvokeVoidAsync("console.log", "GetAccessTokenAsync: No accessToken in cookies");
                    return null;
                }
                
                // Check if token is empty
                if (string.IsNullOrEmpty(accessToken))
                {
                    await _jsRuntime.InvokeVoidAsync("console.log", "GetAccessTokenAsync: Access token is null or empty");
                }
                else
                {
                    await _jsRuntime.InvokeVoidAsync("console.log", $"GetAccessTokenAsync: Access token found (length: {accessToken.Length})");
                }
                
                return accessToken;
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
                // Get refresh token from cookie instead of localStorage
                var refreshToken = await _jsRuntime.InvokeAsync<string>("getCookie", "refreshToken");
                
                if (string.IsNullOrEmpty(refreshToken))
                {
                    await _jsRuntime.InvokeVoidAsync("console.log", "GetRefreshTokenAsync: No refreshToken in cookies");
                    return null;
                }
                
                return refreshToken;
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
                var token = await GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    await _jsRuntime.InvokeVoidAsync("console.log", "IsTokenValidAsync: No token found");
                    return false;
                }

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