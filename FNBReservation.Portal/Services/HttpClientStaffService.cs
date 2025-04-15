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
using System.Linq;

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
                
                // Ensure we have a valid UUID for the outletId
                string guidOutletId = EnsureValidGuid(outletId);
                await _jsRuntime.InvokeVoidAsync("console.log", $"Original outletId: {outletId}, converted to UUID: {guidOutletId}");
                
                string endpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/outlets/{guidOutletId}/staff";
                
                // Add search term as query parameter if provided
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    // Properly encode the search term
                    var encodedSearchTerm = Uri.EscapeDataString(searchTerm);
                    endpoint += $"?q={encodedSearchTerm}";
                    
                    // Log the search query for debugging
                    await _jsRuntime.InvokeVoidAsync("console.log", $"Search query: {searchTerm}, encoded as: {encodedSearchTerm}, using parameter 'q'");
                }
                
                await _jsRuntime.InvokeVoidAsync("console.log", $"Calling endpoint: {endpoint}");
                
                // Use the refresh method
                var response = await SendRequestWithRefreshAsync(() => _httpClient.GetAsync(endpoint));
                
                // Process response
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    await _jsRuntime.InvokeVoidAsync("console.log", $"API Response: {jsonResponse}");
                    
                    var result = JsonSerializer.Deserialize<List<StaffDto>>(jsonResponse, _jsonOptions);
                    await _jsRuntime.InvokeVoidAsync("console.log", $"Deserialized {result?.Count ?? 0} staff members");
                    
                    // If search was performed, do additional client-side filtering if necessary
                    if (!string.IsNullOrWhiteSpace(searchTerm) && result != null)
                    {
                        var searchTermLower = searchTerm.ToLowerInvariant();
                        // If server doesn't fully support search, we can filter here too
                        result = result.Where(staff => 
                            staff.FullName?.ToLowerInvariant().Contains(searchTermLower) == true ||
                            staff.Username?.ToLowerInvariant().Contains(searchTermLower) == true || 
                            staff.Email?.ToLowerInvariant().Contains(searchTermLower) == true ||
                            staff.Phone?.ToLowerInvariant().Contains(searchTermLower) == true ||
                            staff.UserId?.ToLowerInvariant().Contains(searchTermLower) == true
                        ).ToList();
                        
                        await _jsRuntime.InvokeVoidAsync("console.log", $"After client-side filtering: {result.Count} staff members");
                    }
                    
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

        public async Task<List<StaffDto>> GetAllStaffAsync(string? searchTerm = null)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"GetAllStaffAsync called, searchTerm: {searchTerm}");
                
                // Try to use the admin/staff endpoint first (which appears to be working from the screenshot)
                string allStaffEndpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/staff";
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var encodedSearchTerm = Uri.EscapeDataString(searchTerm);
                    allStaffEndpoint += $"?q={encodedSearchTerm}";
                    await _jsRuntime.InvokeVoidAsync("console.log", $"Search query: {searchTerm}, encoded as: {encodedSearchTerm}, using parameter 'q'");
                }
                
                await _jsRuntime.InvokeVoidAsync("console.log", $"Trying direct all staff endpoint: {allStaffEndpoint}");
                
                // Use the refresh method
                var directResponse = await SendRequestWithRefreshAsync(() => _httpClient.GetAsync(allStaffEndpoint));
                
                if (directResponse.IsSuccessStatusCode)
                {
                    var jsonResponse = await directResponse.Content.ReadAsStringAsync();
                    var staffList = JsonSerializer.Deserialize<List<StaffDto>>(jsonResponse, _jsonOptions);
                    await _jsRuntime.InvokeVoidAsync("console.log", $"Direct endpoint succeeded, retrieved {staffList?.Count ?? 0} staff");
                    return staffList ?? new List<StaffDto>();
                }
                
                // If direct endpoint failed, log the error
                var errorContent = await directResponse.Content.ReadAsStringAsync();
                await _jsRuntime.InvokeVoidAsync("console.log", $"Direct endpoint failed, status: {directResponse.StatusCode}, error: {errorContent}");
                
                // If direct endpoint failed, try fetching outlet by outlet
                await _jsRuntime.InvokeVoidAsync("console.log", "Direct endpoint failed, falling back to outlet-by-outlet approach");
                
                // Get all outlets
                string outletsEndpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/outlets";
                
                // Use the refresh method
                var outletsResponse = await SendRequestWithRefreshAsync(() => _httpClient.GetAsync(outletsEndpoint));
                
                if (!outletsResponse.IsSuccessStatusCode)
                {
                    var outletsErrorContent = await outletsResponse.Content.ReadAsStringAsync();
                    string errorMessage = $"Error fetching outlets: {outletsResponse.StatusCode}, Details: {outletsErrorContent}";
                    await _jsRuntime.InvokeVoidAsync("console.log", errorMessage);
                    throw new Exception(errorMessage);
                }
                
                var outletsJson = await outletsResponse.Content.ReadAsStringAsync();
                var outlets = JsonSerializer.Deserialize<List<OutletDto>>(outletsJson, _jsonOptions);
                
                if (outlets == null || !outlets.Any())
                {
                    await _jsRuntime.InvokeVoidAsync("console.log", "No outlets found");
                    return new List<StaffDto>();
                }
                
                await _jsRuntime.InvokeVoidAsync("console.log", $"Retrieved {outlets.Count} outlets, fetching staff for each");
                
                var allStaff = new List<StaffDto>();
                foreach (var outlet in outlets)
                {
                    try
                    {
                        var staffList = await GetStaffAsync(outlet.id, searchTerm);
                        allStaff.AddRange(staffList);
                        await _jsRuntime.InvokeVoidAsync("console.log", $"Added {staffList.Count} staff from outlet {outlet.Name}");
                    }
                    catch (Exception ex)
                    {
                        await _jsRuntime.InvokeVoidAsync("console.error", $"Error fetching staff for outlet {outlet.Name}: {ex.Message}");
                        // Continue to the next outlet
                    }
                }
                
                // If search was provided and we're combining results from multiple outlets,
                // do client-side filtering to ensure consistent results
                if (!string.IsNullOrWhiteSpace(searchTerm) && allStaff.Any())
                {
                    string searchTermLower = searchTerm.ToLowerInvariant();
                    allStaff = allStaff.Where(s => 
                        (s.FullName?.ToLowerInvariant().Contains(searchTermLower) == true) ||
                        (s.Email?.ToLowerInvariant().Contains(searchTermLower) == true) ||
                        (s.Phone?.ToLowerInvariant().Contains(searchTermLower) == true) ||
                        (s.Username?.ToLowerInvariant().Contains(searchTermLower) == true) ||
                        (s.Role?.ToLowerInvariant().Contains(searchTermLower) == true)
                    ).ToList();
                }
                
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
                
                // Add this debug line to check if password is included
                await _jsRuntime.InvokeVoidAsync("console.log", $"Password included: {!string.IsNullOrEmpty(staff.Password)}");
                
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
                
                // Create a copy of staff to modify for the request
                var staffForUpdate = new
                {
                    FullName = staff.FullName,
                    Username = staff.Username,
                    Email = staff.Email,
                    Phone = staff.Phone,
                    Role = staff.Role,
                    IsActive = staff.IsActive,
                    
                    // The API requires a password field - if empty, provide a placeholder valid password
                    // This tells the API we're not trying to change the password
                    Password = !string.IsNullOrEmpty(staff.Password) 
                        ? staff.Password 
                        : "KeepExistingPassword@123" // Use a valid password format as a placeholder
                };
                
                var content = new StringContent(
                    JsonSerializer.Serialize(staffForUpdate, _jsonOptions),
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
                    
                    // Check if this is the specific password validation error
                    if (errorContent.Contains("Password") && errorContent.Contains("required"))
                    {
                        throw new HttpRequestException($"The API requires a password even for updates that don't change the password. Please contact the API developer to adjust the validation rules.");
                    }
                    
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