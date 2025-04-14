using FNBReservation.Portal.Models;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.JSInterop;

namespace FNBReservation.Portal.Services
{
    public class HttpClientOutletService : IOutletService
    {
        private readonly HttpClient _httpClient;
        private readonly JwtTokenService _jwtTokenService;
        private readonly IJSRuntime _jsRuntime;
        private readonly string _baseUrl;
        private readonly JsonSerializerOptions _jsonOptions;

        public HttpClientOutletService(HttpClient httpClient, JwtTokenService jwtTokenService, 
            IJSRuntime jsRuntime, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _jwtTokenService = jwtTokenService;
            _jsRuntime = jsRuntime;
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000/";
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        private async Task SetAuthorizationHeaderAsync()
        {
            try
            {
                // Clear existing Authorization header to ensure we don't use old tokens
                if (_httpClient.DefaultRequestHeaders.Contains("Authorization"))
                {
                    _httpClient.DefaultRequestHeaders.Remove("Authorization");
                }
                
                var token = await _jwtTokenService.GetAccessTokenAsync();
                await _jsRuntime.InvokeVoidAsync("console.log", $"SetAuthorizationHeaderAsync: Token retrieved: {!string.IsNullOrEmpty(token)}");
                
                if (!string.IsNullOrEmpty(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    await _jsRuntime.InvokeVoidAsync("console.log", "Authorization header set successfully");
                    
                    // Check if the token is valid
                    var isTokenValid = await _jwtTokenService.IsTokenValidAsync();
                    if (!isTokenValid)
                    {
                        await _jsRuntime.InvokeVoidAsync("console.log", "Token is invalid, attempting to refresh...");
                        var refreshResult = await _jwtTokenService.RefreshTokenAsync();
                        
                        if (refreshResult.Success)
                        {
                            // Update with the new token
                            _httpClient.DefaultRequestHeaders.Remove("Authorization");
                            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", refreshResult.AccessToken);
                            await _jsRuntime.InvokeVoidAsync("console.log", "Authorization header updated with refreshed token");
                        }
                        else
                        {
                            await _jsRuntime.InvokeVoidAsync("console.log", $"Token refresh failed: {refreshResult.ErrorMessage}");
                            throw new UnauthorizedAccessException("Invalid authentication token and refresh failed");
                        }
                    }
                }
                else
                {
                    await _jsRuntime.InvokeVoidAsync("console.log", "No token available - API call will be unauthorized");
                }
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"Error in SetAuthorizationHeaderAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<OutletDto>> GetOutletsAsync(string? searchTerm = null)
        {
            try
            {
                await SetAuthorizationHeaderAsync();

                string endpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/outlets";
                
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    endpoint += $"?search={Uri.EscapeDataString(searchTerm)}";
                }

                await _jsRuntime.InvokeVoidAsync("console.log", $"GetOutletsAsync: {endpoint}");
                
                var response = await _httpClient.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();

                var outlets = await response.Content.ReadFromJsonAsync<List<OutletDto>>(_jsonOptions);
                return outlets ?? new List<OutletDto>();
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"Error in GetOutletsAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<OutletDto?> GetOutletByIdAsync(string outletId)
        {
            try
            {
                await SetAuthorizationHeaderAsync();

                string endpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/outlets/{outletId}";
                await _jsRuntime.InvokeVoidAsync("console.log", $"GetOutletByIdAsync: {endpoint}");
                
                var response = await _httpClient.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();

                var outlet = await response.Content.ReadFromJsonAsync<OutletDto>(_jsonOptions);
                return outlet;
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"Error in GetOutletByIdAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<OutletChangeDto>> GetOutletChangesAsync(string outletId)
        {
            try
            {
                await SetAuthorizationHeaderAsync();

                string endpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/outlets/{outletId}/changes";
                await _jsRuntime.InvokeVoidAsync("console.log", $"GetOutletChangesAsync: {endpoint}");
                
                var response = await _httpClient.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();

                var changes = await response.Content.ReadFromJsonAsync<List<OutletChangeDto>>(_jsonOptions);
                return changes ?? new List<OutletChangeDto>();
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"Error in GetOutletChangesAsync: {ex.Message}");
                
                // For now, return empty list as this might be a new feature not yet implemented in backend
                return new List<OutletChangeDto>();
            }
        }

        public async Task<bool> CreateOutletAsync(OutletDto outlet)
        {
            try
            {
                await SetAuthorizationHeaderAsync();

                string endpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/outlets";
                await _jsRuntime.InvokeVoidAsync("console.log", $"CreateOutletAsync: {endpoint}");

                // Convert OutletDto to CreateOutletDto format which the API expects
                var createOutletDto = new
                {
                    Name = outlet.Name,
                    Location = outlet.Location,
                    OperatingHours = outlet.OperatingHours,
                    MaxAdvanceReservationTime = outlet.MaxAdvanceReservationTime,
                    MinAdvanceReservationTime = outlet.MinAdvanceReservationTime,
                    Contact = outlet.Contact,
                    QueueEnabled = outlet.QueueEnabled,
                    SpecialRequirements = outlet.SpecialRequirements,
                    Status = outlet.Status,
                    Latitude = outlet.Latitude,
                    Longitude = outlet.Longitude,
                    ReservationAllocationPercent = outlet.ReservationAllocationPercent,
                    DefaultDiningDurationMinutes = outlet.DefaultDiningDurationMinutes
                };

                var jsonContent = JsonSerializer.Serialize(createOutletDto);
                await _jsRuntime.InvokeVoidAsync("console.log", $"Request payload: {jsonContent}");
                
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync(endpoint, content);
                
                // Log response details for debugging
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await _jsRuntime.InvokeVoidAsync("console.log", $"Error response: {errorContent}");
                }
                
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"Error in CreateOutletAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdateOutletAsync(OutletDto outlet)
        {
            try
            {
                await SetAuthorizationHeaderAsync();

                string endpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/outlets/{outlet.id}";
                await _jsRuntime.InvokeVoidAsync("console.log", $"UpdateOutletAsync: {endpoint}");

                // Convert OutletDto to UpdateOutletDto format which the API expects
                var updateOutletDto = new
                {
                    Name = outlet.Name,
                    Location = outlet.Location,
                    OperatingHours = outlet.OperatingHours,
                    MaxAdvanceReservationTime = outlet.MaxAdvanceReservationTime,
                    MinAdvanceReservationTime = outlet.MinAdvanceReservationTime,
                    Contact = outlet.Contact,
                    QueueEnabled = outlet.QueueEnabled,
                    SpecialRequirements = outlet.SpecialRequirements,
                    Status = outlet.Status,
                    Latitude = outlet.Latitude,
                    Longitude = outlet.Longitude,
                    ReservationAllocationPercent = outlet.ReservationAllocationPercent,
                    DefaultDiningDurationMinutes = outlet.DefaultDiningDurationMinutes
                };

                var jsonContent = JsonSerializer.Serialize(updateOutletDto);
                await _jsRuntime.InvokeVoidAsync("console.log", $"Request payload: {jsonContent}");
                
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PutAsync(endpoint, content);
                
                // Log response details for debugging
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await _jsRuntime.InvokeVoidAsync("console.log", $"Error response: {errorContent}");
                }
                
                response.EnsureSuccessStatusCode();

                return true;
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"Error in UpdateOutletAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteOutletAsync(string outletId)
        {
            try
            {
                await SetAuthorizationHeaderAsync();

                // The outletId parameter should be the UUID id, not the business ID
                string endpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/outlets/{outletId}";
                await _jsRuntime.InvokeVoidAsync("console.log", $"DeleteOutletAsync: {endpoint}");
                
                var response = await _httpClient.DeleteAsync(endpoint);
                
                // Log response details for debugging
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await _jsRuntime.InvokeVoidAsync("console.log", $"Error response: {errorContent}");
                }
                
                response.EnsureSuccessStatusCode();

                return true;
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"Error in DeleteOutletAsync: {ex.Message}");
                throw;
            }
        }
    }
} 