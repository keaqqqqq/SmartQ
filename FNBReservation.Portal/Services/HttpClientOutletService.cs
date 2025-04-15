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
        private readonly IJSRuntime _jsRuntime;
        private readonly string _baseUrl;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly IPeakHourService _peakHourService;

        public HttpClientOutletService(HttpClient httpClient, IJSRuntime jsRuntime, 
            IConfiguration configuration, IPeakHourService peakHourService)
        {
            _httpClient = httpClient;
            _jsRuntime = jsRuntime;
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000/";
            _peakHourService = peakHourService;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
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
                
                var response = await _httpClient.GetAsync(endpoint);
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
                                var tableResponse = await _httpClient.GetAsync(tableEndpoint);
                                
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
                
                var response = await _httpClient.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();

                var outlet = await response.Content.ReadFromJsonAsync<OutletDto>(_jsonOptions);
                
                // Explicitly load tables for this outlet
                if (outlet != null && Guid.TryParse(outlet.id, out Guid guid))
                {
                    try
                    {
                        string tableEndpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/outlets/{outletId}/tables";
                        var tableResponse = await _httpClient.GetAsync(tableEndpoint);
                        
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
                
                var response = await _httpClient.GetAsync(endpoint);
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
                
                // Create a version of the outlet that's compatible with the API
                var apiOutlet = new
                {
                    Name = outlet.Name,
                    Address = outlet.Address,
                    PhoneNumber = outlet.PhoneNumber,
                    Email = outlet.Email,
                    ManagerEmail = outlet.ManagerEmail,
                    Description = outlet.Description,
                    OpeningHours = outlet.OpeningHours,
                    ClosingHours = outlet.ClosingHours,
                    MaxCapacity = outlet.MaxCapacity,
                    Status = outlet.Status,
                    ServesFoodOnly = outlet.ServesFoodOnly,
                    ImageUrl = outlet.ImageUrl,
                    PeakHours = outlet.PeakHours,
                    CuisineType = outlet.CuisineType,
                    PriceRange = outlet.PriceRange
                };
                
                var json = JsonSerializer.Serialize(apiOutlet);
                await _jsRuntime.InvokeVoidAsync("console.log", $"CreateOutletAsync payload: {json}");
                
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(endpoint, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await _jsRuntime.InvokeVoidAsync("console.log", $"Error creating outlet: {errorContent}");
                    return false;
                }
                
                var createdOutlet = await response.Content.ReadFromJsonAsync<OutletDto>(_jsonOptions);
                
                // If we have tables to create, add them now
                if (createdOutlet != null && outlet.Tables != null && outlet.Tables.Count > 0)
                {
                    string tablesEndpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/outlets/{createdOutlet.id}/tables";
                    
                    foreach (var table in outlet.Tables)
                    {
                        try
                        {
                            // Create the table object for API
                            var apiTable = new
                            {
                                tableNumber = table.TableNumber,
                                capacity = table.Capacity,
                                isActive = table.IsActive,
                                section = table.Section ?? "Default"
                            };
                            
                            var tableJson = JsonSerializer.Serialize(apiTable);
                            var tableContent = new StringContent(tableJson, Encoding.UTF8, "application/json");
                            
                            var tableResponse = await _httpClient.PostAsync(tablesEndpoint, tableContent);
                            
                            if (!tableResponse.IsSuccessStatusCode)
                            {
                                var tableErrorContent = await tableResponse.Content.ReadAsStringAsync();
                                await _jsRuntime.InvokeVoidAsync("console.log", $"Error creating table {table.TableNumber}: {tableErrorContent}");
                            }
                        }
                        catch (Exception ex)
                        {
                            await _jsRuntime.InvokeVoidAsync("console.log", $"Exception while creating table {table.TableNumber}: {ex.Message}");
                        }
                    }
                }
                
                // If we have peak hours to create, add them now
                if (createdOutlet != null && outlet.PeakHoursList != null && outlet.PeakHoursList.Count > 0)
                {
                    foreach (var peakHour in outlet.PeakHoursList)
                    {
                        try
                        {
                            await _peakHourService.CreatePeakHourAsync(createdOutlet.id, peakHour);
                        }
                        catch (Exception ex)
                        {
                            await _jsRuntime.InvokeVoidAsync("console.log", $"Error creating peak hour {peakHour.Name}: {ex.Message}");
                        }
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"Error in CreateOutletAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateOutletAsync(OutletDto outlet)
        {
            try
            {
                string endpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/outlets/{outlet.id}";
                await _jsRuntime.InvokeVoidAsync("console.log", $"UpdateOutletAsync: {endpoint}");
                
                // Create a version of the outlet that's compatible with the API
                var apiOutlet = new
                {
                    Name = outlet.Name,
                    Address = outlet.Address,
                    PhoneNumber = outlet.PhoneNumber,
                    Email = outlet.Email,
                    ManagerEmail = outlet.ManagerEmail,
                    Description = outlet.Description,
                    OpeningHours = outlet.OpeningHours,
                    ClosingHours = outlet.ClosingHours,
                    MaxCapacity = outlet.MaxCapacity,
                    Status = outlet.Status,
                    ServesFoodOnly = outlet.ServesFoodOnly,
                    ImageUrl = outlet.ImageUrl,
                    PeakHours = outlet.PeakHours,
                    CuisineType = outlet.CuisineType,
                    PriceRange = outlet.PriceRange
                };
                
                var json = JsonSerializer.Serialize(apiOutlet);
                await _jsRuntime.InvokeVoidAsync("console.log", $"UpdateOutletAsync payload: {json}");
                
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync(endpoint, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await _jsRuntime.InvokeVoidAsync("console.log", $"Error updating outlet: {errorContent}");
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"Error in UpdateOutletAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteOutletAsync(string outletId)
        {
            try
            {
                string endpoint = $"{_baseUrl.TrimEnd('/')}/api/v1/admin/outlets/{outletId}";
                await _jsRuntime.InvokeVoidAsync("console.log", $"DeleteOutletAsync: {endpoint}");
                
                var response = await _httpClient.DeleteAsync(endpoint);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await _jsRuntime.InvokeVoidAsync("console.log", $"Error deleting outlet: {errorContent}");
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"Error in DeleteOutletAsync: {ex.Message}");
                return false;
            }
        }
    }
} 