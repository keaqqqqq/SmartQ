using Microsoft.JSInterop;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

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
            _httpClient = httpClient;
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000/";
            _refreshTokenEndpoint = configuration["ApiSettings:AuthEndpoints:RefreshToken"] ?? "api/v1/auth/refresh-token";
        }

        public async Task<string> GetAccessTokenAsync()
        {
            try
            {
                var authData = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authData");
                if (string.IsNullOrEmpty(authData))
                {
                    return null;
                }

                var tokenData = JsonSerializer.Deserialize<AuthData>(authData);
                return tokenData?.AccessToken;
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
                var authData = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authData");
                if (string.IsNullOrEmpty(authData))
                {
                    return null;
                }

                var tokenData = JsonSerializer.Deserialize<AuthData>(authData);
                return tokenData?.RefreshToken;
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
                    return false;
                }

                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jwtToken = handler.ReadJwtToken(token);
                    
                    // Check if token has expired
                    var expiry = jwtToken.ValidTo;
                    return expiry > DateTime.UtcNow;
                }
                catch
                {
                    return false;
                }
            }
            catch (InvalidOperationException)
            {
                // This happens during static rendering
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<RefreshTokenResponse> RefreshTokenAsync()
        {
            try
            {
                var refreshToken = await GetRefreshTokenAsync();
                if (string.IsNullOrEmpty(refreshToken))
                {
                    return new RefreshTokenResponse { Success = false, ErrorMessage = "No refresh token available" };
                }

                var refreshRequest = new
                {
                    RefreshToken = refreshToken
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(refreshRequest),
                    System.Text.Encoding.UTF8,
                    "application/json");

                string fullUrl = $"{_baseUrl.TrimEnd('/')}/{_refreshTokenEndpoint}";
                var response = await _httpClient.PostAsync(fullUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var refreshResponse = JsonSerializer.Deserialize<ApiRefreshResponse>(responseContent, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (refreshResponse != null && !string.IsNullOrEmpty(refreshResponse.AccessToken))
                    {
                        // Update the stored tokens
                        await UpdateTokensInStorageAsync(refreshResponse.AccessToken, refreshResponse.RefreshToken);

                        return new RefreshTokenResponse 
                        { 
                            Success = true, 
                            AccessToken = refreshResponse.AccessToken,
                            RefreshToken = refreshResponse.RefreshToken
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

        public async Task UpdateTokensInStorageAsync(string accessToken, string refreshToken)
        {
            try
            {
                try
                {
                    var authData = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authData");
                    if (string.IsNullOrEmpty(authData))
                    {
                        return;
                    }

                    var tokenData = JsonSerializer.Deserialize<AuthData>(authData);
                    if (tokenData == null)
                    {
                        return;
                    }

                    tokenData.AccessToken = accessToken;
                    tokenData.RefreshToken = refreshToken;

                    var serializedData = JsonSerializer.Serialize(tokenData);
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authData", serializedData);
                    
                    await _jsRuntime.InvokeVoidAsync("console.log", "Tokens updated in storage");
                }
                catch (InvalidOperationException)
                {
                    // This happens during static rendering - we'll skip the JS interop
                }
            }
            catch (Exception ex)
            {
                try
                {
                    await _jsRuntime.InvokeVoidAsync("console.log", "Error updating tokens in storage: " + ex.Message);
                }
                catch
                {
                    // Ignore JS interop errors during static rendering
                }
            }
        }

        public async Task<UserInfo> GetUserInfoFromTokenAsync()
        {
            try
            {
                var token = await GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    return null;
                }

                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jwtToken = handler.ReadJwtToken(token);
                    
                    var userInfo = new UserInfo
                    {
                        Username = jwtToken.Claims.FirstOrDefault(c => c.Type == "unique_name" || c.Type == ClaimTypes.Name)?.Value,
                        Role = jwtToken.Claims.FirstOrDefault(c => c.Type == "role" || c.Type == ClaimTypes.Role)?.Value,
                        UserId = jwtToken.Claims.FirstOrDefault(c => c.Type == "nameid" || c.Type == ClaimTypes.NameIdentifier)?.Value
                    };
                    
                    return userInfo;
                }
                catch (Exception ex)
                {
                    try
                    {
                        await _jsRuntime.InvokeVoidAsync("console.log", "Error extracting user info from token: " + ex.Message);
                    }
                    catch (InvalidOperationException)
                    {
                        // This happens during static rendering - ignore JS interop
                    }
                    return null;
                }
            }
            catch (InvalidOperationException)
            {
                // This happens during static rendering
                return null;
            }
            catch
            {
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