using FNBReservation.Portal.Models;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.JSInterop;
using Microsoft.Extensions.Configuration;

namespace FNBReservation.Portal.Services
{
    public class HttpClientOutletService : IOutletService
    {
        private readonly HttpClient _httpClient;
        private readonly JwtTokenService _jwtTokenService;
        private readonly IJSRuntime _jsRuntime;
        private readonly string _baseUrl;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly IPeakHourService _peakHourService;

        public HttpClientOutletService(HttpClient httpClient, JwtTokenService jwtTokenService, 
            IJSRuntime jsRuntime, IConfiguration configuration, IPeakHourService peakHourService)
        {
            _httpClient = httpClient;
            _jwtTokenService = jwtTokenService;
            _jsRuntime = jsRuntime;
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000/";
            _peakHourService = peakHourService;
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
                            // Update with the new token - get it from cookies via TokenService
                            var newToken = await _jwtTokenService.GetAccessTokenAsync();
                            
                            // Remove existing Authorization header
                            _httpClient.DefaultRequestHeaders.Remove("Authorization");
                            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", newToken);
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

        private async Task<HttpResponseMessage> SendRequestWithRefreshAsync(Func<Task<HttpResponseMessage>> requestFunc)
        {
            try
            {
                // Set authorization header before making the request
                await SetAuthorizationHeaderAsync();
                
                // Execute the original request
                var response = await requestFunc();
                
                // If unauthorized, try to refresh the token and retry
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _jsRuntime.InvokeVoidAsync("console.log", "Received 401 Unauthorized, attempting to refresh token");
                    
                    var refreshResult = await _jwtTokenService.RefreshTokenAsync();
                    if (refreshResult.Success)
                    {
                        await _jsRuntime.InvokeVoidAsync("console.log", "Token refreshed successfully, retrying request");
                        
                        // Reset authorization header with the refreshed token
                        await SetAuthorizationHeaderAsync();
                        
                        // Retry the request with the new token
                        response = await requestFunc();
                    }
                    else
                    {
                        await _jsRuntime.InvokeVoidAsync("console.log", $"Token refresh failed: {refreshResult.ErrorMessage}");
                    }
                }
                
                return response;
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"Error in SendRequestWithRefreshAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<OutletDto>> GetOutletsAsync(string? searchTerm = null)
        {
            try
            {
                string endpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/outlets";
                
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    endpoint += $"?search={Uri.EscapeDataString(searchTerm)}";
                }

                await _jsRuntime.InvokeVoidAsync("console.log", $"GetOutletsAsync: {endpoint}");
                
                // Use the new method that handles token refresh
                var response = await SendRequestWithRefreshAsync(() => _httpClient.GetAsync(endpoint));
                
                // Now we can ensure success and continue as before
                response.EnsureSuccessStatusCode();

                var outlets = await response.Content.ReadFromJsonAsync<List<OutletDto>>(_jsonOptions);
                
                // Load tables for each outlet
                if (outlets != null)
                {
                    for (int i = 0; i < outlets.Count; i++)
                    {
                        try
                        {
                            // Get tables for the outlet
                            if (Guid.TryParse(outlets[i].id, out Guid outletId))
                            {
                                string tableEndpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/outlets/{outlets[i].id}/tables";
                                
                                // Use the refresh method for this request too
                                var tableResponse = await SendRequestWithRefreshAsync(() => _httpClient.GetAsync(tableEndpoint));
                                
                                if (tableResponse.IsSuccessStatusCode)
                                {
                                    var tables = await tableResponse.Content.ReadFromJsonAsync<List<TableInfo>>(_jsonOptions);
                                    outlets[i].Tables = tables ?? new List<TableInfo>();
                                    await _jsRuntime.InvokeVoidAsync("console.log", $"Loaded {tables?.Count ?? 0} tables for outlet {outlets[i].Name}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            await _jsRuntime.InvokeVoidAsync("console.log", $"Error loading tables for outlet {outlets[i].Name}: {ex.Message}");
                            // Continue with the next outlet even if this one fails
                        }
                    }
                }
                
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
                string endpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/outlets/{outletId}";
                await _jsRuntime.InvokeVoidAsync("console.log", $"GetOutletByIdAsync: {endpoint}");
                
                // Use the new method that handles token refresh
                var response = await SendRequestWithRefreshAsync(() => _httpClient.GetAsync(endpoint));
                
                response.EnsureSuccessStatusCode();

                var outlet = await response.Content.ReadFromJsonAsync<OutletDto>(_jsonOptions);
                
                // Explicitly load tables for this outlet
                if (outlet != null && Guid.TryParse(outlet.id, out Guid guid))
                {
                    try
                    {
                        string tableEndpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/outlets/{outletId}/tables";
                        
                        // Use the refresh method for this request too
                        var tableResponse = await SendRequestWithRefreshAsync(() => _httpClient.GetAsync(tableEndpoint));
                        
                        if (tableResponse.IsSuccessStatusCode)
                        {
                            var tables = await tableResponse.Content.ReadFromJsonAsync<List<TableInfo>>(_jsonOptions);
                            outlet.Tables = tables ?? new List<TableInfo>();
                            await _jsRuntime.InvokeVoidAsync("console.log", $"Loaded {tables?.Count ?? 0} tables for outlet {outlet.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        await _jsRuntime.InvokeVoidAsync("console.log", $"Error loading tables for outlet {outlet.Name}: {ex.Message}");
                        // Continue since we still have the outlet data
                    }
                }
                
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
                string endpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/outlets/{outletId}/changes";
                await _jsRuntime.InvokeVoidAsync("console.log", $"GetOutletChangesAsync: {endpoint}");
                
                // Use the refresh method
                var response = await SendRequestWithRefreshAsync(() => _httpClient.GetAsync(endpoint));
                
                response.EnsureSuccessStatusCode();

                var changes = await response.Content.ReadFromJsonAsync<List<OutletChangeDto>>(_jsonOptions);
                return changes ?? new List<OutletChangeDto>();
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"Error in GetOutletChangesAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> CreateOutletAsync(OutletDto outlet)
        {
            try
            {
                string endpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/outlets";
                await _jsRuntime.InvokeVoidAsync("console.log", $"CreateOutletAsync: {endpoint}");
                
                var json = JsonSerializer.Serialize(outlet);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                // Use the refresh method
                var response = await SendRequestWithRefreshAsync(() => _httpClient.PostAsync(endpoint, content));
                
                if (response.IsSuccessStatusCode)
                {
                    await _jsRuntime.InvokeVoidAsync("console.log", "Outlet created successfully");
                    
                    // If any peak hours were included, create them too
                    if (outlet.PeakHours != null && outlet.PeakHours.Any())
                    {
                        try
                        {
                            // First get the newly created outlet ID from the response
                            var createdOutlet = await response.Content.ReadFromJsonAsync<OutletDto>(_jsonOptions);
                            if (createdOutlet != null && !string.IsNullOrEmpty(createdOutlet.id))
                            {
                                await _jsRuntime.InvokeVoidAsync("console.log", $"New outlet ID: {createdOutlet.id}, creating {outlet.PeakHours.Count} peak hours");
                                
                                foreach (var peakHour in outlet.PeakHours)
                                {
                                    // Ensure the peak hour is associated with the new outlet
                                    //peakHour.OutletId = createdOutlet.id;
                                    await _peakHourService.CreatePeakHourAsync(createdOutlet.id, peakHour);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            await _jsRuntime.InvokeVoidAsync("console.log", $"Error creating peak hours: {ex.Message}");
                            // Don't fail the whole operation if peak hours creation fails
                        }
                    }
                    
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await _jsRuntime.InvokeVoidAsync("console.log", $"Error creating outlet: {response.StatusCode}, {errorContent}");
                    return false;
                }
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
                if (string.IsNullOrEmpty(outlet.id))
                {
                    await _jsRuntime.InvokeVoidAsync("console.log", "Cannot update outlet: ID is missing");
                    return false;
                }
                
                string endpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/outlets/{outlet.id}";
                await _jsRuntime.InvokeVoidAsync("console.log", $"UpdateOutletAsync: {endpoint}");
                
                var json = JsonSerializer.Serialize(outlet);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                // Use the refresh method
                var response = await SendRequestWithRefreshAsync(() => _httpClient.PutAsync(endpoint, content));
                
                if (response.IsSuccessStatusCode)
                {
                    await _jsRuntime.InvokeVoidAsync("console.log", "Outlet updated successfully");
                    
                    // Handle any peak hours that were updated/added/removed
                    if (outlet.PeakHours != null)
                    {
                        try
                        {
                            // Get existing peak hours
                            var existingPeakHours = await _peakHourService.GetPeakHoursAsync(outlet.id);
                            await _jsRuntime.InvokeVoidAsync("console.log", $"Found {existingPeakHours.Count} existing peak hours, updating to {outlet.PeakHours.Count} peak hours");

                            // Process each peak hour in the update
                            foreach (var peakHour in outlet.PeakHours)
                            {
                                if (string.IsNullOrEmpty(peakHour.Id))
                                {
                                    // New peak hour to create
                                    await _peakHourService.CreatePeakHourAsync(outlet.id, peakHour);
                                }
                                else
                                {
                                    // Existing peak hour to update
                                    await _peakHourService.UpdatePeakHourAsync(outlet.id, peakHour.Id, peakHour);
                                }
                            }

                            // Find peak hours to delete (ones that exist in DB but not in the update)
                            var updatedIds = outlet.PeakHours.Where(p => !string.IsNullOrEmpty(p.Id)).Select(p => p.Id).ToList();
                            var toDelete = existingPeakHours.Where(p => !updatedIds.Contains(p.Id)).ToList();

                            // Delete peak hours that were removed
                            foreach (var peakHour in toDelete)
                            {
                                await _peakHourService.DeletePeakHourAsync(outlet.id, peakHour.Id);
                            }
                        }
                        catch (Exception ex)
                        {
                            await _jsRuntime.InvokeVoidAsync("console.log", $"Error updating peak hours: {ex.Message}");
                            // Don't fail the whole operation if peak hours update fails
                        }
                    }
                    
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await _jsRuntime.InvokeVoidAsync("console.log", $"Error updating outlet: {response.StatusCode}, {errorContent}");
                    return false;
                }
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
                string endpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/outlets/{outletId}";
                await _jsRuntime.InvokeVoidAsync("console.log", $"DeleteOutletAsync: {endpoint}");
                
                // Use the refresh method
                var response = await SendRequestWithRefreshAsync(() => _httpClient.DeleteAsync(endpoint));
                
                if (response.IsSuccessStatusCode)
                {
                    await _jsRuntime.InvokeVoidAsync("console.log", "Outlet deleted successfully");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await _jsRuntime.InvokeVoidAsync("console.log", $"Error deleting outlet: {response.StatusCode}, {errorContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"Error in DeleteOutletAsync: {ex.Message}");
                throw;
            }
        }
    }
} 