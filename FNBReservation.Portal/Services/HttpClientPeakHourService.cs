using FNBReservation.Portal.Models;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.JSInterop;
using Microsoft.Extensions.Configuration;

namespace FNBReservation.Portal.Services
{
    public class HttpClientPeakHourService : IPeakHourService
    {
        private readonly HttpClient _httpClient;
        private readonly JwtTokenService _jwtTokenService;
        private readonly IJSRuntime _jsRuntime;
        private readonly string _baseUrl;
        private readonly JsonSerializerOptions _jsonOptions;

        public HttpClientPeakHourService(HttpClient httpClient, JwtTokenService jwtTokenService,
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

        public async Task<List<PeakHour>> GetPeakHoursAsync(string outletId)
        {
            try
            {
                await SetAuthorizationHeaderAsync();

                string endpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/outlets/{outletId}/peak-hours";
                await _jsRuntime.InvokeVoidAsync("console.log", $"GetPeakHoursAsync: {endpoint}");

                var response = await _httpClient.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();

                var peakHoursDto = await response.Content.ReadFromJsonAsync<List<PeakHourSettingDto>>(_jsonOptions);
                
                // Convert from API DTO to our model
                var peakHours = peakHoursDto?.Select(MapToPeakHour).ToList() ?? new List<PeakHour>();
                return peakHours;
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"Error in GetPeakHoursAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<PeakHour> GetPeakHourByIdAsync(string outletId, string peakHourId)
        {
            try
            {
                await SetAuthorizationHeaderAsync();

                string endpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/outlets/{outletId}/peak-hours/{peakHourId}";
                await _jsRuntime.InvokeVoidAsync("console.log", $"GetPeakHourByIdAsync: {endpoint}");

                var response = await _httpClient.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();

                var peakHourDto = await response.Content.ReadFromJsonAsync<PeakHourSettingDto>(_jsonOptions);
                
                if (peakHourDto == null)
                {
                    return null;
                }
                
                return MapToPeakHour(peakHourDto);
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"Error in GetPeakHourByIdAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<PeakHour>> GetActivePeakHoursAsync(string outletId, DateTime? date = null)
        {
            try
            {
                await SetAuthorizationHeaderAsync();

                string endpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/outlets/{outletId}/peak-hours/active";
                
                if (date.HasValue)
                {
                    var formattedDate = date.Value.ToString("yyyy-MM-dd");
                    endpoint += $"?date={formattedDate}";
                }
                
                await _jsRuntime.InvokeVoidAsync("console.log", $"GetActivePeakHoursAsync: {endpoint}");

                var response = await _httpClient.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();

                var peakHoursDto = await response.Content.ReadFromJsonAsync<List<PeakHourSettingDto>>(_jsonOptions);
                
                // Convert from API DTO to our model
                var peakHours = peakHoursDto?.Select(MapToPeakHour).ToList() ?? new List<PeakHour>();
                return peakHours;
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"Error in GetActivePeakHoursAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<PeakHour> CreatePeakHourAsync(string outletId, PeakHour peakHour)
        {
            try
            {
                await SetAuthorizationHeaderAsync();

                string endpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/outlets/{outletId}/peak-hours";
                await _jsRuntime.InvokeVoidAsync("console.log", $"CreatePeakHourAsync: {endpoint}");

                // Convert to API DTO
                var createPeakHourDto = new
                {
                    Name = peakHour.Name,
                    DaysOfWeek = peakHour.DaysOfWeek,
                    StartTime = peakHour.StartTime,
                    EndTime = peakHour.EndTime,
                    ReservationAllocationPercent = peakHour.ReservationAllocationPercent,
                    IsActive = peakHour.IsActive
                };

                var jsonContent = JsonSerializer.Serialize(createPeakHourDto);
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
                
                var createdPeakHourDto = await response.Content.ReadFromJsonAsync<PeakHourSettingDto>(_jsonOptions);
                return MapToPeakHour(createdPeakHourDto);
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"Error in CreatePeakHourAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<PeakHour> UpdatePeakHourAsync(string outletId, string peakHourId, PeakHour peakHour)
        {
            try
            {
                await SetAuthorizationHeaderAsync();

                string endpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/outlets/{outletId}/peak-hours/{peakHourId}";
                await _jsRuntime.InvokeVoidAsync("console.log", $"UpdatePeakHourAsync: {endpoint}");

                // Convert to API DTO
                var updatePeakHourDto = new
                {
                    Name = peakHour.Name,
                    DaysOfWeek = peakHour.DaysOfWeek,
                    StartTime = peakHour.StartTime,
                    EndTime = peakHour.EndTime,
                    ReservationAllocationPercent = peakHour.ReservationAllocationPercent,
                    IsActive = peakHour.IsActive
                };

                var jsonContent = JsonSerializer.Serialize(updatePeakHourDto);
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
                
                var updatedPeakHourDto = await response.Content.ReadFromJsonAsync<PeakHourSettingDto>(_jsonOptions);
                return MapToPeakHour(updatedPeakHourDto);
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"Error in UpdatePeakHourAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeletePeakHourAsync(string outletId, string peakHourId)
        {
            try
            {
                await SetAuthorizationHeaderAsync();

                string endpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/outlets/{outletId}/peak-hours/{peakHourId}";
                await _jsRuntime.InvokeVoidAsync("console.log", $"DeletePeakHourAsync: {endpoint}");
                
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
                await _jsRuntime.InvokeVoidAsync("console.log", $"Error in DeletePeakHourAsync: {ex.Message}");
                throw;
            }
        }

        // Helper method to convert API DTO to our model
        private PeakHour MapToPeakHour(PeakHourSettingDto dto)
        {
            return new PeakHour
            {
                Id = dto.Id.ToString(),
                Name = dto.Name,
                DaysOfWeek = dto.DaysOfWeek,
                StartTime = dto.StartTime.ToString(@"hh\:mm\:ss"),
                EndTime = dto.EndTime.ToString(@"hh\:mm\:ss"),
                ReservationAllocationPercent = dto.ReservationAllocationPercent,
                IsActive = dto.IsActive
            };
        }
    }

    // This is a DTO class to match the backend API model
    public class PeakHourSettingDto
    {
        public Guid Id { get; set; }
        public Guid OutletId { get; set; }
        public string Name { get; set; }
        public string DaysOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int ReservationAllocationPercent { get; set; }
        public bool IsActive { get; set; }
    }
} 