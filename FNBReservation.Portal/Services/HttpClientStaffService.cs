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
        private readonly IJSRuntime _jsRuntime;
        private readonly string _baseUrl;
        private readonly ILogger<HttpClientStaffService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public HttpClientStaffService(
            HttpClient httpClient,
            IJSRuntime jsRuntime, 
            IConfiguration configuration,
            ILogger<HttpClientStaffService> logger)
        {
            _httpClient = httpClient;
            _jsRuntime = jsRuntime;
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000/";
            _logger = logger;
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
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
                var response = await _httpClient.GetAsync(endpoint);
                
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
                var directResponse = await _httpClient.GetAsync(allStaffEndpoint);
                
                if (directResponse.IsSuccessStatusCode)
                {
                    var jsonResponse = await directResponse.Content.ReadAsStringAsync();
                    await _jsRuntime.InvokeVoidAsync("console.log", $"API Response: {jsonResponse}");
                    
                    var result = JsonSerializer.Deserialize<List<StaffDto>>(jsonResponse, _jsonOptions);
                    await _jsRuntime.InvokeVoidAsync("console.log", $"Deserialized {result?.Count ?? 0} staff members");
                    
                    return result ?? new List<StaffDto>();
                }
                else
                {
                    var errorContent = await directResponse.Content.ReadAsStringAsync();
                    await _jsRuntime.InvokeVoidAsync("console.error", 
                        $"API error for all staff: {directResponse.StatusCode}, Details: {errorContent}");
                    
                    // Fallback approach: try to get staff from all outlets
                    await _jsRuntime.InvokeVoidAsync("console.log", "Falling back to getting staff from all outlets");
                    
                    // First get all outlets
                    string outletsEndpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/outlets";
                    var outletsResponse = await _httpClient.GetAsync(outletsEndpoint);
                    
                    if (outletsResponse.IsSuccessStatusCode)
                    {
                        var outletsJsonResponse = await outletsResponse.Content.ReadAsStringAsync();
                        var outlets = JsonSerializer.Deserialize<List<OutletDto>>(outletsJsonResponse, _jsonOptions);
                        
                        if (outlets == null || outlets.Count == 0)
                        {
                            await _jsRuntime.InvokeVoidAsync("console.log", "No outlets found, returning empty staff list");
                            return new List<StaffDto>();
                        }
                        
                        await _jsRuntime.InvokeVoidAsync("console.log", $"Found {outlets.Count} outlets, getting staff for each");
                        
                        // Collect staff from all outlets
                        var allStaff = new List<StaffDto>();
                        foreach (var outlet in outlets)
                        {
                            try
                            {
                                var staffList = await GetStaffAsync(outlet.id, searchTerm);
                                allStaff.AddRange(staffList);
                            }
                            catch (Exception ex)
                            {
                                await _jsRuntime.InvokeVoidAsync("console.error", 
                                    $"Error getting staff for outlet {outlet.id}: {ex.Message}");
                            }
                        }
                        
                        // Remove duplicates based on UserId
                        var uniqueStaff = allStaff
                            .GroupBy(s => s.UserId)
                            .Select(g => g.First())
                            .ToList();
                        
                        await _jsRuntime.InvokeVoidAsync("console.log", 
                            $"Combined {allStaff.Count} staff entries, {uniqueStaff.Count} unique staff members after deduplication");
                        
                        return uniqueStaff;
                    }
                    else
                    {
                        var outletsErrorContent = await outletsResponse.Content.ReadAsStringAsync();
                        await _jsRuntime.InvokeVoidAsync("console.error", 
                            $"API error for outlets: {outletsResponse.StatusCode}, Details: {outletsErrorContent}");
                        
                        // If we can't get outlets either, return empty list
                        return new List<StaffDto>();
                    }
                }
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
                
                if (string.IsNullOrEmpty(outletId) || string.IsNullOrEmpty(staffId))
                {
                    await _jsRuntime.InvokeVoidAsync("console.error", "Invalid outletId or staffId provided");
                    return null;
                }
                
                // Ensure we have valid UUIDs
                string guidOutletId = EnsureValidGuid(outletId);
                string guidStaffId = EnsureValidGuid(staffId);
                
                string endpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/outlets/{guidOutletId}/staff/{guidStaffId}";
                await _jsRuntime.InvokeVoidAsync("console.log", $"Calling endpoint: {endpoint}");
                
                var response = await _httpClient.GetAsync(endpoint);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var staff = JsonSerializer.Deserialize<StaffDto>(jsonResponse, _jsonOptions);
                    
                    await _jsRuntime.InvokeVoidAsync("console.log", $"Successfully retrieved staff member: {staff?.FullName}");
                    return staff;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await _jsRuntime.InvokeVoidAsync("console.error", 
                        $"API error: {response.StatusCode}, Details: {errorContent}");
                    
                    return null;
                }
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.error", $"Exception in GetStaffByIdAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> CreateStaffAsync(string outletId, StaffDto staff)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"CreateStaffAsync called for outlet: {outletId}");
                
                if (string.IsNullOrEmpty(outletId))
                {
                    await _jsRuntime.InvokeVoidAsync("console.error", "Invalid outletId provided");
                    return false;
                }
                
                string guidOutletId = EnsureValidGuid(outletId);
                string endpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/outlets/{guidOutletId}/staff";
                
                var jsonContent = JsonSerializer.Serialize(staff, _jsonOptions);
                await _jsRuntime.InvokeVoidAsync("console.log", $"Request payload: {jsonContent}");
                
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(endpoint, content);
                
                if (response.IsSuccessStatusCode)
                {
                    await _jsRuntime.InvokeVoidAsync("console.log", "Staff created successfully");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await _jsRuntime.InvokeVoidAsync("console.error", 
                        $"API error: {response.StatusCode}, Details: {errorContent}");
                    
                    return false;
                }
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.error", $"Exception in CreateStaffAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateStaffAsync(string outletId, StaffDto staff)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"UpdateStaffAsync called for outlet: {outletId}, staffId: {staff.Id}");
                
                if (string.IsNullOrEmpty(outletId) || string.IsNullOrEmpty(staff.Id))
                {
                    await _jsRuntime.InvokeVoidAsync("console.error", "Invalid outletId or staffId provided");
                    return false;
                }
                
                string guidOutletId = EnsureValidGuid(outletId);
                string guidStaffId = EnsureValidGuid(staff.Id);
                
                string endpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/outlets/{guidOutletId}/staff/{guidStaffId}";
                
                var jsonContent = JsonSerializer.Serialize(staff, _jsonOptions);
                await _jsRuntime.InvokeVoidAsync("console.log", $"Request payload: {jsonContent}");
                
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync(endpoint, content);
                
                if (response.IsSuccessStatusCode)
                {
                    await _jsRuntime.InvokeVoidAsync("console.log", "Staff updated successfully");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await _jsRuntime.InvokeVoidAsync("console.error", 
                        $"API error: {response.StatusCode}, Details: {errorContent}");
                    
                    return false;
                }
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.error", $"Exception in UpdateStaffAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteStaffAsync(string outletId, string staffId)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"DeleteStaffAsync called for outlet: {outletId}, staffId: {staffId}");
                
                if (string.IsNullOrEmpty(outletId) || string.IsNullOrEmpty(staffId))
                {
                    await _jsRuntime.InvokeVoidAsync("console.error", "Invalid outletId or staffId provided");
                    return false;
                }
                
                string guidOutletId = EnsureValidGuid(outletId);
                string guidStaffId = EnsureValidGuid(staffId);
                
                string endpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/outlets/{guidOutletId}/staff/{guidStaffId}";
                
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
                    
                    return false;
                }
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.error", $"Exception in DeleteStaffAsync: {ex.Message}");
                return false;
            }
        }
    }
} 