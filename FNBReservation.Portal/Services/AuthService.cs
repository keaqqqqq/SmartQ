using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace FNBReservation.Portal.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly IJSRuntime _jsRuntime;
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _baseUrl;
        private readonly string _loginEndpoint;
        private readonly string _forgotPasswordEndpoint;
        private readonly string _resetPasswordEndpoint;
        private readonly string _logoutEndpoint;

        public AuthService(
            HttpClient httpClient,
            IJSRuntime jsRuntime,
            AuthenticationStateProvider authStateProvider,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor = null)
        {
            _httpClient = httpClient;
            _jsRuntime = jsRuntime;
            _authStateProvider = authStateProvider;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;

            _baseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000/";
            _loginEndpoint = _configuration["ApiSettings:AuthEndpoints:Login"] ?? "api/v1/auth/login";
            _forgotPasswordEndpoint = _configuration["ApiSettings:AuthEndpoints:ForgotPassword"] ?? "api/v1/auth/forgot-password";
            _resetPasswordEndpoint = _configuration["ApiSettings:AuthEndpoints:ResetPassword"] ?? "api/v1/auth/reset-password";
            _logoutEndpoint = _configuration["ApiSettings:AuthEndpoints:Logout"] ?? "api/v1/auth/logout";
        }

        public async Task<LoginResult> Login(string username, string password, bool rememberMe)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("console.log", "Login attempt for: " + username);

                var loginModel = new
                {
                    Username = username,
                    Password = password,
                    RememberMe = rememberMe
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(loginModel),
                    Encoding.UTF8,
                    "application/json");

                string fullUrl = $"{_baseUrl.TrimEnd('/')}/{_loginEndpoint}";
                await _jsRuntime.InvokeVoidAsync("console.log", "Login URL: " + fullUrl);

                var response = await _httpClient.PostAsync(fullUrl, content);

                await _jsRuntime.InvokeVoidAsync("console.log", "Login response status: " + response.StatusCode);

                // Read the response content as string for logging
                var responseContent = await response.Content.ReadAsStringAsync();
                await _jsRuntime.InvokeVoidAsync("console.log", "Login response content: " + responseContent);

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        // Parse the API response
                        var apiResponse = JsonSerializer.Deserialize<ApiLoginResponse>(responseContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        // If the response has a username and token, it's a successful login
                        if (apiResponse != null && (!string.IsNullOrEmpty(apiResponse.Username) || !string.IsNullOrEmpty(apiResponse.AccessToken)))
                        {
                            string userUsername = apiResponse.Username;
                            string userRole = apiResponse.Role ?? "User";
                            
                            // Notify the auth state provider about successful authentication
                            if (_authStateProvider is CustomAuthStateProvider customProvider)
                            {
                                customProvider.NotifyUserAuthentication(userUsername, userRole);
                                await _jsRuntime.InvokeVoidAsync("console.log", "Authentication state updated for: " + userUsername);
                            }

                            return new LoginResult
                            {
                                Success = true,
                                Username = userUsername,
                                Role = userRole
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        await _jsRuntime.InvokeVoidAsync("console.log", "Error parsing login response: " + ex.Message);
                    }
                }

                // If we get here, login failed or response parsing failed
                try
                {
                    var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent);
                    return new LoginResult { Success = false, ErrorMessage = errorResponse?.Message ?? "Invalid credentials" };
                }
                catch
                {
                    await _jsRuntime.InvokeVoidAsync("console.log", "Failed to parse error response");
                    return new LoginResult { Success = false, ErrorMessage = "Invalid credentials" };
                }
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.log", "Login exception: " + ex.Message);
                return new LoginResult { Success = false, ErrorMessage = "An error occurred during login" };
            }
        }

        public async Task<bool> ForgotPassword(string email)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("console.log", "Forgot password request for: " + email);

                var forgotPasswordModel = new
                {
                    Email = email
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(forgotPasswordModel),
                    Encoding.UTF8,
                    "application/json");

                string fullUrl = $"{_baseUrl.TrimEnd('/')}/{_forgotPasswordEndpoint}";
                await _jsRuntime.InvokeVoidAsync("console.log", "Forgot password URL: " + fullUrl);

                var response = await _httpClient.PostAsync(fullUrl, content);

                await _jsRuntime.InvokeVoidAsync("console.log", "Forgot password response: " + response.StatusCode);

                // We return true even if the email doesn't exist (to prevent email enumeration)
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.log", "Forgot password exception: " + ex.Message);
                return false;
            }
        }

        public async Task<ResetPasswordResult> ResetPassword(string token, string newPassword)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("console.log", "Reset password request with token: " + token);

                var resetPasswordModel = new
                {
                    Token = token,
                    NewPassword = newPassword
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(resetPasswordModel),
                    Encoding.UTF8,
                    "application/json");

                string fullUrl = $"{_baseUrl.TrimEnd('/')}/{_resetPasswordEndpoint}";
                await _jsRuntime.InvokeVoidAsync("console.log", "Reset password URL: " + fullUrl);

                var response = await _httpClient.PostAsync(fullUrl, content);

                await _jsRuntime.InvokeVoidAsync("console.log", "Reset password response: " + response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    return new ResetPasswordResult { Success = true };
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                await _jsRuntime.InvokeVoidAsync("console.log", "Reset password error: " + errorContent);

                try
                {
                    var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorContent);
                    return new ResetPasswordResult
                    {
                        Success = false,
                        ErrorMessage = errorResponse?.Message ?? "Failed to reset password"
                    };
                }
                catch
                {
                    return new ResetPasswordResult { Success = false, ErrorMessage = "Failed to reset password" };
                }
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.log", "Reset password exception: " + ex.Message);
                return new ResetPasswordResult { Success = false, ErrorMessage = "An error occurred during password reset" };
            }
        }

        public async Task Logout()
        {
            try
            {
                // If we have access to HttpContext, sign out
                if (_httpContextAccessor?.HttpContext != null)
                {
                    await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    await _jsRuntime.InvokeVoidAsync("console.log", "User signed out from cookie auth");
                }

                // Always clear localStorage as well
                if (_authStateProvider is CustomAuthStateProvider customProvider)
                {
                    customProvider.NotifyUserLogout();
                }

                // Call backend API to invalidate any tokens
                string fullUrl = $"{_baseUrl.TrimEnd('/')}/{_logoutEndpoint}";
                await _jsRuntime.InvokeVoidAsync("console.log", "Logout URL: " + fullUrl);

                await _httpClient.PostAsync(fullUrl, null);
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.log", "Logout exception: " + ex.Message);
            }
        }
    }

    // Class that matches the actual API response structure
    public class ApiLoginResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
        public string Role { get; set; }
        public string Username { get; set; }
    }

    public class LoginResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string Role { get; set; }
        public string Username { get; set; }
    }

    public class ResetPasswordResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class ErrorResponse
    {
        public string Message { get; set; }
    }
}