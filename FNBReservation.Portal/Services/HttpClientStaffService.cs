using FNBReservation.Portal.Models;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.JSInterop;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FNBReservation.Portal.Services
{
    public class HttpClientStaffService : IStaffService
    {
        private readonly HttpClient _httpClient;
        private readonly JwtTokenService _jwtTokenService;
        private readonly IJSRuntime _jsRuntime;
        private readonly string _baseUrl;
        private readonly ILogger<HttpClientStaffService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public HttpClientStaffService(
            HttpClient httpClient, 
            JwtTokenService jwtTokenService,
            IJSRuntime jsRuntime, 
            IConfiguration configuration,
            ILogger<HttpClientStaffService> logger)
        {
            _httpClient = httpClient;
            _jwtTokenService = jwtTokenService;
            _jsRuntime = jsRuntime;
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000/";
            _logger = logger;
            
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
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                    await _jsRuntime.InvokeVoidAsync("console.log", "Authorization header set successfully");
                }
                else
                {
                    await _jsRuntime.InvokeVoidAsync("console.log", "Failed to set authorization header - token is empty");
                }
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.error", $"Error setting authorization header: {ex.Message}");
            }
        }

        // Helper method to ensure we have a proper UUID format
        private string EnsureValidGuid(string id)
        {
            if (Guid.TryParse(id, out Guid guid))
            {
                return guid.ToString();
            }
            
            // If not a valid UUID, use a default UUID format for the string
            try
            {
                // Create a deterministic UUID based on the string
                using var md5 = System.Security.Cryptography.MD5.Create();
                byte[] inputBytes = Encoding.ASCII.GetBytes(id);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                
                // Convert the byte array to a GUID
                return new Guid(hashBytes).ToString();
            }
            catch (Exception)
            {
                // Fallback to a new random UUID if conversion fails
                return Guid.NewGuid().ToString();
            }
        }

        public async Task<List<StaffDto>> GetStaffAsync(string outletId, string? searchTerm = null)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"GetStaffAsync called for outlet: {outletId}, searchTerm: {searchTerm}");
                
                if (string.IsNullOrEmpty(outletId) || outletId == "undefined")
                {
                    await _jsRuntime.InvokeVoidAsync("console.error", "Invalid outletId provided: empty or undefined");
                    return new List<StaffDto>();
                }
                
                await SetAuthorizationHeaderAsync();
                
                // Ensure we have a valid UUID for the outletId
                string guidOutletId = EnsureValidGuid(outletId);
                await _jsRuntime.InvokeVoidAsync("console.log", $"Original outletId: {outletId}, converted to UUID: {guidOutletId}");
                
                string endpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/outlets/{guidOutletId}/staff";
                
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    endpoint += $"?search={Uri.EscapeDataString(searchTerm)}";
                }
                
                await _jsRuntime.InvokeVoidAsync("console.log", $"Calling endpoint: {endpoint}");
                var response = await _httpClient.GetAsync(endpoint);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    await _jsRuntime.InvokeVoidAsync("console.log", $"API Response: {jsonResponse}");
                    
                    var result = JsonSerializer.Deserialize<List<StaffDto>>(jsonResponse, _jsonOptions);
                    await _jsRuntime.InvokeVoidAsync("console.log", $"Deserialized {result?.Count ?? 0} staff members");
                    return result ?? new List<StaffDto>();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await _jsRuntime.InvokeVoidAsync("console.error", 
                        $"API error: {response.StatusCode}, Details: {errorContent}");
                    
                    // If not found, return empty list rather than throwing an exception
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        await _jsRuntime.InvokeVoidAsync("console.log", $"No staff found for outlet {outletId}, returning empty list");
                        return new List<StaffDto>();
                    }
                    
                    throw new HttpRequestException($"Error calling staff API: {response.StatusCode}, Details: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.error", $"Exception in GetStaffAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<StaffDto>> GetAllStaffAsync(string? searchTerm = null)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"GetAllStaffAsync called, searchTerm: {searchTerm}");
                await SetAuthorizationHeaderAsync();
                
                // Try to use the admin/staff endpoint first (which appears to be working from the screenshot)
                string allStaffEndpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/staff";
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    allStaffEndpoint += $"?search={Uri.EscapeDataString(searchTerm)}";
                }
                
                await _jsRuntime.InvokeVoidAsync("console.log", $"Trying direct all staff endpoint: {allStaffEndpoint}");
                var directResponse = await _httpClient.GetAsync(allStaffEndpoint);
                
                if (directResponse.IsSuccessStatusCode)
                {
                    var jsonResponse = await directResponse.Content.ReadAsStringAsync();
                    await _jsRuntime.InvokeVoidAsync("console.log", $"API Response from direct endpoint: {jsonResponse}");
                    
                    var result = JsonSerializer.Deserialize<List<StaffDto>>(jsonResponse, _jsonOptions);
                    await _jsRuntime.InvokeVoidAsync("console.log", $"Deserialized {result?.Count ?? 0} staff members from direct endpoint");
                    return result ?? new List<StaffDto>();
                }
                
                // If direct endpoint fails, fallback to our outlet-by-outlet approach
                await _jsRuntime.InvokeVoidAsync("console.log", "Direct endpoint failed, falling back to outlet-by-outlet approach");
                
                // Since there may not be a dedicated endpoint for all staff,
                // we'll use the outlets endpoint to get all outlets first
                string outletsEndpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/outlets";
                await _jsRuntime.InvokeVoidAsync("console.log", $"Fetching all outlets: {outletsEndpoint}");
                
                var outletsResponse = await _httpClient.GetAsync(outletsEndpoint);
                
                if (!outletsResponse.IsSuccessStatusCode)
                {
                    var errorContent = await outletsResponse.Content.ReadAsStringAsync();
                    await _jsRuntime.InvokeVoidAsync("console.error", 
                        $"Error fetching outlets: {outletsResponse.StatusCode}, Details: {errorContent}");
                    
                    throw new HttpRequestException($"Error fetching outlets: {outletsResponse.StatusCode}, Details: {errorContent}");
                }
                
                var outletsJson = await outletsResponse.Content.ReadAsStringAsync();
                await _jsRuntime.InvokeVoidAsync("console.log", $"Outlets API response: {outletsJson}");
                var outlets = JsonSerializer.Deserialize<List<OutletDto>>(outletsJson, _jsonOptions);
                
                if (outlets == null || outlets.Count == 0)
                {
                    await _jsRuntime.InvokeVoidAsync("console.log", "No outlets found, returning empty staff list");
                    return new List<StaffDto>();
                }
                
                await _jsRuntime.InvokeVoidAsync("console.log", $"Found {outlets.Count} outlets, fetching staff for each outlet");
                
                // Now fetch staff for each outlet and combine
                var allStaff = new List<StaffDto>();
                
                foreach (var outlet in outlets)
                {
                    try
                    {
                        // Get the outlet ID (prefer UUID id over string OutletId)
                        string outletId = !string.IsNullOrEmpty(outlet.id) ? outlet.id : outlet.OutletId;
                        
                        if (string.IsNullOrEmpty(outletId))
                        {
                            await _jsRuntime.InvokeVoidAsync("console.log", $"Skipping outlet '{outlet.Name}' - No valid outlet ID found");
                            continue;
                        }
                        
                        // Always ensure we have a valid UUID format
                        string guidOutletId = EnsureValidGuid(outletId);
                        await _jsRuntime.InvokeVoidAsync("console.log", $"Using outlet ID: {outletId}, converted to UUID: {guidOutletId}");
                        
                        string staffEndpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/outlets/{guidOutletId}/staff";
                        
                        if (!string.IsNullOrWhiteSpace(searchTerm))
                        {
                            staffEndpoint += $"?search={Uri.EscapeDataString(searchTerm)}";
                        }
                        
                        await _jsRuntime.InvokeVoidAsync("console.log", $"Fetching staff for outlet {outlet.Name}: {staffEndpoint}");
                        
                        var staffResponse = await _httpClient.GetAsync(staffEndpoint);
                        
                        if (staffResponse.IsSuccessStatusCode)
                        {
                            var staffJson = await staffResponse.Content.ReadAsStringAsync();
                            var staffList = JsonSerializer.Deserialize<List<StaffDto>>(staffJson, _jsonOptions);
                            
                            if (staffList != null && staffList.Count > 0)
                            {
                                await _jsRuntime.InvokeVoidAsync("console.log", $"Found {staffList.Count} staff for outlet {outlet.Name}");
                                allStaff.AddRange(staffList);
                            }
                            else
                            {
                                await _jsRuntime.InvokeVoidAsync("console.log", $"No staff found for outlet {outlet.Name}");
                            }
                        }
                        else if (staffResponse.StatusCode != System.Net.HttpStatusCode.NotFound)
                        {
                            var errorContent = await staffResponse.Content.ReadAsStringAsync();
                            await _jsRuntime.InvokeVoidAsync("console.error", 
                                $"Error fetching staff for outlet {outlet.Name}: {staffResponse.StatusCode}, Details: {errorContent}");
                        }
                        else
                        {
                            await _jsRuntime.InvokeVoidAsync("console.log", $"No staff found for outlet {outlet.Name} (404 Not Found)");
                        }
                    }
                    catch (Exception ex)
                    {
                        await _jsRuntime.InvokeVoidAsync("console.error", 
                            $"Error fetching staff for outlet {outlet.Name}: {ex.Message}");
                        // Continue with other outlets even if one fails
                    }
                }
                
                await _jsRuntime.InvokeVoidAsync("console.log", $"Total staff members from all outlets: {allStaff.Count}");
                return allStaff;
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.error", $"Exception in GetAllStaffAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<StaffDto?> GetStaffByIdAsync(string outletId, string staffId)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"GetStaffByIdAsync called for outlet: {outletId}, staffId: {staffId}");
                await SetAuthorizationHeaderAsync();
                
                // Ensure we have valid UUIDs for both IDs
                string guidOutletId = EnsureValidGuid(outletId);
                string guidStaffId = EnsureValidGuid(staffId);
                
                string endpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/outlets/{guidOutletId}/staff/{guidStaffId}";
                
                await _jsRuntime.InvokeVoidAsync("console.log", $"Calling endpoint: {endpoint}");
                var response = await _httpClient.GetAsync(endpoint);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    await _jsRuntime.InvokeVoidAsync("console.log", $"API Response: {jsonResponse}");
                    
                    var result = JsonSerializer.Deserialize<StaffDto>(jsonResponse, _jsonOptions);
                    return result;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await _jsRuntime.InvokeVoidAsync("console.log", $"Staff not found with ID: {staffId}");
                    return null;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await _jsRuntime.InvokeVoidAsync("console.error", 
                        $"API error: {response.StatusCode}, Details: {errorContent}");
                    
                    throw new HttpRequestException($"Error calling staff API: {response.StatusCode}, Details: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.error", $"Exception in GetStaffByIdAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> CreateStaffAsync(string outletId, StaffDto staff)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"CreateStaffAsync called for outlet: {outletId}, staff name: {staff.FullName}");
                await SetAuthorizationHeaderAsync();
                
                // Ensure we have a valid UUID for the outletId
                string guidOutletId = EnsureValidGuid(outletId);
                
                string endpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/outlets/{guidOutletId}/staff";
                
                var content = new StringContent(
                    JsonSerializer.Serialize(staff, _jsonOptions),
                    Encoding.UTF8,
                    "application/json");
                
                await _jsRuntime.InvokeVoidAsync("console.log", $"Calling endpoint: {endpoint}");
                await _jsRuntime.InvokeVoidAsync("console.log", $"Request body: {await content.ReadAsStringAsync()}");
                
                var response = await _httpClient.PostAsync(endpoint, content);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    await _jsRuntime.InvokeVoidAsync("console.log", $"API Response: {jsonResponse}");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await _jsRuntime.InvokeVoidAsync("console.error", 
                        $"API error: {response.StatusCode}, Details: {errorContent}");
                    
                    throw new HttpRequestException($"Error creating staff: {response.StatusCode}, Details: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.error", $"Exception in CreateStaffAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdateStaffAsync(string outletId, StaffDto staff)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"UpdateStaffAsync called for outlet: {outletId}, staffId: {staff.StaffId}");
                await SetAuthorizationHeaderAsync();
                
                // Ensure we have valid UUIDs for both IDs
                string guidOutletId = EnsureValidGuid(outletId);
                string guidStaffId = EnsureValidGuid(staff.StaffId);
                
                string endpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/outlets/{guidOutletId}/staff/{guidStaffId}";
                
                var content = new StringContent(
                    JsonSerializer.Serialize(staff, _jsonOptions),
                    Encoding.UTF8,
                    "application/json");
                
                await _jsRuntime.InvokeVoidAsync("console.log", $"Calling endpoint: {endpoint}");
                await _jsRuntime.InvokeVoidAsync("console.log", $"Request body: {await content.ReadAsStringAsync()}");
                
                var response = await _httpClient.PutAsync(endpoint, content);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    await _jsRuntime.InvokeVoidAsync("console.log", $"API Response: {jsonResponse}");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await _jsRuntime.InvokeVoidAsync("console.error", 
                        $"API error: {response.StatusCode}, Details: {errorContent}");
                    
                    throw new HttpRequestException($"Error updating staff: {response.StatusCode}, Details: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.error", $"Exception in UpdateStaffAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteStaffAsync(string outletId, string staffId)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"DeleteStaffAsync called for outlet: {outletId}, staffId: {staffId}");
                await SetAuthorizationHeaderAsync();
                
                // Ensure we have valid UUIDs for both IDs
                string guidOutletId = EnsureValidGuid(outletId);
                string guidStaffId = EnsureValidGuid(staffId);
                
                string endpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/outlets/{guidOutletId}/staff/{guidStaffId}";
                
                await _jsRuntime.InvokeVoidAsync("console.log", $"Calling endpoint: {endpoint}");
                var response = await _httpClient.DeleteAsync(endpoint);
                
                if (response.IsSuccessStatusCode)
                {
                    await _jsRuntime.InvokeVoidAsync("console.log", "Staff deleted successfully");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await _jsRuntime.InvokeVoidAsync("console.error", 
                        $"API error: {response.StatusCode}, Details: {errorContent}");
                    
                    throw new HttpRequestException($"Error deleting staff: {response.StatusCode}, Details: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.error", $"Exception in DeleteStaffAsync: {ex.Message}");
                throw;
            }
        }
    }
} 