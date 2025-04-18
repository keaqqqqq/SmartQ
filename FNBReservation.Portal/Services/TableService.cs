using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FNBReservation.Portal.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FNBReservation.Portal.Services
{
    public interface ITableService
    {
        Task<List<TableInfo>> GetTablesByOutletIdAsync(Guid outletId);
        Task<TableInfo> GetTableByIdAsync(Guid outletId, Guid tableId);
        Task<List<SectionInfo>> GetSectionsByOutletIdAsync(Guid outletId);
        Task<TableInfo> CreateTableAsync(Guid outletId, CreateTableRequest request);
        Task<TableInfo> UpdateTableAsync(Guid outletId, Guid tableId, UpdateTableRequest request);
        Task DeleteTableAsync(Guid outletId, Guid tableId);
        Task<List<TableTypeInfo>> GetTableTypesByOutletIdAsync(string outletId, string tableType);
    }
    
    public class TableService : ITableService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseApiUrl;
        private readonly JwtTokenService _jwtTokenService;
        private readonly ILogger<TableService> _logger;
        
        public TableService(HttpClient httpClient, IConfiguration configuration, JwtTokenService jwtTokenService = null, ILogger<TableService> logger = null)
        {
            _httpClient = httpClient;
            _baseApiUrl = configuration["ApiSettings:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5000";
            _logger = logger;
            _jwtTokenService = jwtTokenService;
            
            _logger?.LogInformation($"TableService initialized with base API URL: {_baseApiUrl}");
        }
        
        private async Task EnsureAuthorizationHeaderAsync()
        {
            if (_jwtTokenService == null)
                return;
                
            try
            {
                // Remove existing Authorization header if present
                if (_httpClient.DefaultRequestHeaders.Contains("Authorization"))
                {
                    _httpClient.DefaultRequestHeaders.Remove("Authorization");
                }
                
                // Get and set a new token
                var token = await _jwtTokenService.GetAccessTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }
                
                // Handle token expiration and refresh if needed
                var isTokenValid = await _jwtTokenService.IsTokenValidAsync();
                if (!isTokenValid)
                {
                    var refreshResult = await _jwtTokenService.RefreshTokenAsync();
                    if (refreshResult != null && refreshResult.Success)
                    {
                        // After successful refresh, get the token again (it's in HTTP-only cookies now)
                        token = await _jwtTokenService.GetAccessTokenAsync();
                        if (!string.IsNullOrEmpty(token))
                        {
                            _httpClient.DefaultRequestHeaders.Remove("Authorization");
                            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error setting authorization header");
            }
        }
        
        public async Task<List<TableInfo>> GetTablesByOutletIdAsync(Guid outletId)
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();
                
                var response = await _httpClient.GetAsync($"{_baseApiUrl}/api/v1/admin/outlets/{outletId}/tables");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<TableInfo>>() ?? new List<TableInfo>();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return new List<TableInfo>();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Failed to get tables for outlet {outletId}");
                throw new Exception($"Failed to get tables for outlet {outletId}: {ex.Message}", ex);
            }
        }
        
        public async Task<TableInfo> GetTableByIdAsync(Guid outletId, Guid tableId)
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();
                
                var response = await _httpClient.GetAsync($"{_baseApiUrl}/api/v1/admin/outlets/{outletId}/tables/{tableId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<TableInfo>() 
                    ?? throw new Exception($"Table with ID {tableId} not found");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                throw new Exception($"Table with ID {tableId} not found");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Failed to get table {tableId}");
                throw new Exception($"Failed to get table {tableId}: {ex.Message}", ex);
            }
        }
        
        public async Task<List<SectionInfo>> GetSectionsByOutletIdAsync(Guid outletId)
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();
                
                var response = await _httpClient.GetAsync($"{_baseApiUrl}/api/v1/admin/outlets/{outletId}/tables/sections");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<SectionInfo>>() ?? new List<SectionInfo>();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return new List<SectionInfo>();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Failed to get sections for outlet {outletId}");
                throw new Exception($"Failed to get sections for outlet {outletId}: {ex.Message}", ex);
            }
        }
        
        public async Task<TableInfo> CreateTableAsync(Guid outletId, CreateTableRequest request)
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();
                
                var url = $"{_baseApiUrl}/api/v1/admin/outlets/{outletId}/tables";
                _logger?.LogInformation($"Creating table with API endpoint: {url}");
                _logger?.LogInformation($"Request payload: {JsonSerializer.Serialize(request)}");
                
                var response = await _httpClient.PostAsJsonAsync(url, request);
                
                // Log response status code
                _logger?.LogInformation($"Response status code: {(int)response.StatusCode} {response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogError($"API returned error: {errorContent}");
                }
                
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<TableInfo>() 
                    ?? throw new Exception("Failed to create table");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
            {
                _logger?.LogError(ex, "Bad request when creating table");
                throw new Exception("Invalid table data provided");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger?.LogError(ex, $"Outlet with ID {outletId} not found or tables endpoint not available");
                throw new Exception($"API endpoint for outlet {outletId} tables not found. Ensure the outlet exists and API is available.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to create table");
                throw new Exception($"Failed to create table: {ex.Message}", ex);
            }
        }
        
        public async Task<TableInfo> UpdateTableAsync(Guid outletId, Guid tableId, UpdateTableRequest request)
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();
                
                var response = await _httpClient.PutAsJsonAsync($"{_baseApiUrl}/api/v1/admin/outlets/{outletId}/tables/{tableId}", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<TableInfo>() 
                    ?? throw new Exception($"Failed to update table {tableId}");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                throw new Exception($"Table with ID {tableId} not found");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
            {
                throw new Exception("Invalid table data provided");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Failed to update table {tableId}");
                throw new Exception($"Failed to update table {tableId}: {ex.Message}", ex);
            }
        }
        
        public async Task DeleteTableAsync(Guid outletId, Guid tableId)
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();
                
                var response = await _httpClient.DeleteAsync($"{_baseApiUrl}/api/v1/admin/outlets/{outletId}/tables/{tableId}");
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                throw new Exception($"Table with ID {tableId} not found");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Failed to delete table {tableId}");
                throw new Exception($"Failed to delete table {tableId}: {ex.Message}", ex);
            }
        }
        
        public async Task<List<TableTypeInfo>> GetTableTypesByOutletIdAsync(string outletId, string tableType)
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();
                
                // Build URL for the API call
                var url = $"{_baseApiUrl}/api/v1/outlets/{outletId}/table-types/{tableType}";
                _logger?.LogInformation($"Fetching table types from: {url}");
                
                // Make the HTTP request and capture the response
                _logger?.LogInformation($"Making HTTP GET request to: {url}");
                var response = await _httpClient.GetAsync(url);
                
                // Log response status code
                _logger?.LogInformation($"Response status code: {(int)response.StatusCode} {response.StatusCode}");
                
                // If the response was not successful, log the error and return an empty list
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogError($"API returned error: {errorContent}");
                    
                    // If the endpoint with outletId doesn't work, try fallback to get tables via admin API
                    if ((int)response.StatusCode == 404 || (int)response.StatusCode == 400)
                    {
                        _logger?.LogInformation("Trying fallback to admin tables API endpoint");
                        // Try to interpret outletId as Guid
                        if (Guid.TryParse(outletId, out Guid outletGuid))
                        {
                            try
                            {
                                var adminUrl = $"{_baseApiUrl}/api/v1/admin/outlets/{outletGuid}/tables";
                                _logger?.LogInformation($"Making fallback HTTP GET request to: {adminUrl}");
                                var adminResponse = await _httpClient.GetAsync(adminUrl);
                                
                                if (adminResponse.IsSuccessStatusCode)
                                {
                                    var adminContent = await adminResponse.Content.ReadAsStringAsync();
                                    _logger?.LogInformation($"Fallback API successful. Response content length: {adminContent.Length}");
                                    
                                    var tables = await adminResponse.Content.ReadFromJsonAsync<List<TableInfo>>() ?? new List<TableInfo>();
                                    // Convert TableInfo to TableTypeInfo
                                    var tableTypeInfos = tables.Select(t => new TableTypeInfo
                                    {
                                        Id = t.Id.ToString(),
                                        TableNumber = t.TableNumber,
                                        Capacity = t.Capacity,
                                        Status = "available", // Default status
                                        Section = t.Section,
                                        IsActive = t.IsActive
                                    }).ToList();
                                    
                                    _logger?.LogInformation($"Converted {tableTypeInfos.Count} tables from admin API");
                                    return tableTypeInfos;
                                }
                                else
                                {
                                    var adminErrorContent = await adminResponse.Content.ReadAsStringAsync();
                                    _logger?.LogError($"Admin API fallback returned error: {adminErrorContent}");
                                }
                            }
                            catch (Exception fallbackEx)
                            {
                                _logger?.LogError(fallbackEx, "Error in fallback admin API call");
                            }
                        }
                    }
                    
                    return new List<TableTypeInfo>();
                }
                
                // Read the response content as a string
                var content = await response.Content.ReadAsStringAsync();
                _logger?.LogInformation($"API response content length: {content.Length}");
                
                if (string.IsNullOrEmpty(content) || content == "[]")
                {
                    _logger?.LogWarning("API returned empty response for tables");
                    return new List<TableTypeInfo>();
                }
                
                try
                {
                    // Try to deserialize the response content into a list of TableTypeInfo objects
                    var tableTypes = await response.Content.ReadFromJsonAsync<List<TableTypeInfo>>() ?? new List<TableTypeInfo>();
                    _logger?.LogInformation($"Successfully deserialized {tableTypes.Count} table types");
                    
                    // Log the first table if there are any
                    if (tableTypes.Count > 0)
                    {
                        _logger?.LogInformation($"First table: ID={tableTypes[0].Id}, Number={tableTypes[0].TableNumber}, Status={tableTypes[0].Status}, Capacity={tableTypes[0].Capacity}");
                    }
                    
                    return tableTypes;
                }
                catch (JsonException jsonEx)
                {
                    _logger?.LogError(jsonEx, $"JSON deserialization error: {jsonEx.Message}");
                    _logger?.LogError($"Response content that failed to deserialize: {content}");
                    
                    // Try to deserialize with different property names
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };
                        var tableTypes = JsonSerializer.Deserialize<List<TableTypeInfo>>(content, options) ?? new List<TableTypeInfo>();
                        _logger?.LogInformation($"Successfully deserialized {tableTypes.Count} table types with case-insensitive option");
                        return tableTypes;
                    }
                    catch (Exception ex2)
                    {
                        _logger?.LogError(ex2, "Failed to deserialize with case-insensitive option");
                        return new List<TableTypeInfo>();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Failed to get table types for outlet {outletId}");
                return new List<TableTypeInfo>();
            }
        }
    }
    
    public class TableTypeInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        [JsonPropertyName("tableNumber")]
        public string TableNumber { get; set; }
        
        [JsonPropertyName("capacity")]
        public int Capacity { get; set; }
        
        [JsonPropertyName("status")]
        public string Status { get; set; }
        
        [JsonPropertyName("section")]
        public string Section { get; set; }
        
        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }
    }
} 