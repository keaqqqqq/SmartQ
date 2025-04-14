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
                    if (refreshResult != null && !string.IsNullOrEmpty(refreshResult.AccessToken))
                    {
                        _httpClient.DefaultRequestHeaders.Remove("Authorization");
                        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", refreshResult.AccessToken);
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
    }
} 