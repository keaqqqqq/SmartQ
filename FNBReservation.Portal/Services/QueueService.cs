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
    public interface IQueueService
    {
        Task<List<QueueEntryDto>> GetWaitingQueueByOutletIdAsync(string outletId);
        Task<bool> CancelQueueEntryAsync(string outletId, string queueId, string reason = "No-show");
        Task<QueueEntryDto> CallNextInQueueAsync(string outletId, string tableId);
        Task<List<TableRecommendationDto>> GetTableRecommendationsAsync(string outletId, string queueId);
        Task<bool> AssignTableToQueueAsync(string outletId, string queueId, string tableId);
        Task<bool> MarkQueueAsSeatedAsync(string outletId, string queueId);
        Task<bool> MarkQueueAsCompletedAsync(string outletId, string queueId);
        Task<List<TableTypeInfo>> GetQueueTablesAsync(string outletId);
    }
    
    public class QueueService : IQueueService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseApiUrl;
        private readonly JwtTokenService _jwtTokenService;
        private readonly ILogger<QueueService> _logger;
        
        public QueueService(HttpClient httpClient, IConfiguration configuration, JwtTokenService jwtTokenService = null, ILogger<QueueService> logger = null)
        {
            _httpClient = httpClient;
            _baseApiUrl = configuration["ApiSettings:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5000";
            _logger = logger;
            _jwtTokenService = jwtTokenService;
            
            _logger?.LogInformation($"QueueService initialized with base API URL: {_baseApiUrl}");
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
        
        public async Task<List<QueueEntryDto>> GetWaitingQueueByOutletIdAsync(string outletId)
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();
                
                var url = $"{_baseApiUrl}/api/v1/outlets/{outletId}/queue/waiting";
                _logger?.LogInformation($"Fetching waiting queue from: {url}");
                
                var response = await _httpClient.GetAsync(url);
                
                _logger?.LogInformation($"Response status code: {(int)response.StatusCode} {response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogError($"API returned error: {errorContent}");
                    return new List<QueueEntryDto>();
                }
                
                var content = await response.Content.ReadAsStringAsync();
                _logger?.LogInformation($"API response content length: {content.Length}");
                
                if (string.IsNullOrEmpty(content) || content == "[]")
                {
                    _logger?.LogWarning("API returned empty response for waiting queue");
                    return new List<QueueEntryDto>();
                }
                
                try
                {
                    var queueEntries = await response.Content.ReadFromJsonAsync<List<QueueEntryDto>>() ?? new List<QueueEntryDto>();
                    _logger?.LogInformation($"Successfully deserialized {queueEntries.Count} queue entries");
                    
                    if (queueEntries.Count > 0)
                    {
                        _logger?.LogInformation($"First queue entry: ID={queueEntries[0].QueueId}, Customer={queueEntries[0].CustomerName}");
                    }
                    
                    return queueEntries;
                }
                catch (JsonException jsonEx)
                {
                    _logger?.LogError(jsonEx, $"JSON deserialization error: {jsonEx.Message}");
                    _logger?.LogError($"Response content that failed to deserialize: {content}");
                    
                    // Try with case-insensitive option
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };
                        var queueEntries = JsonSerializer.Deserialize<List<QueueEntryDto>>(content, options) ?? new List<QueueEntryDto>();
                        _logger?.LogInformation($"Successfully deserialized {queueEntries.Count} queue entries with case-insensitive option");
                        return queueEntries;
                    }
                    catch (Exception ex2)
                    {
                        _logger?.LogError(ex2, "Failed to deserialize with case-insensitive option");
                        return new List<QueueEntryDto>();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Failed to get waiting queue for outlet {outletId}");
                return new List<QueueEntryDto>();
            }
        }
        
        public async Task<bool> CancelQueueEntryAsync(string outletId, string queueId, string reason = "No-show")
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();
                
                var url = $"{_baseApiUrl}/api/v1/outlets/{outletId}/queue/{queueId}/cancel";
                _logger?.LogInformation($"Canceling queue entry: {url} with reason: {reason}");
                
                // Create content with reason
                var content = new StringContent(
                    JsonSerializer.Serialize(new { reason = reason }), 
                    System.Text.Encoding.UTF8, 
                    "application/json");
                
                // Use POST instead of DELETE for the cancel endpoint
                var response = await _httpClient.PostAsync(url, content);
                
                _logger?.LogInformation($"Response status code: {(int)response.StatusCode} {response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogError($"API returned error: {errorContent}");
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Failed to cancel queue entry {queueId} for outlet {outletId}");
                return false;
            }
        }

        public async Task<QueueEntryDto> CallNextInQueueAsync(string outletId, string tableId)
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();
                
                var url = $"{_baseApiUrl}/api/v1/outlets/{outletId}/queue/call-next";
                _logger?.LogInformation($"Calling next in queue: {url} with tableId: {tableId}");
                
                // Create JSON content with tableId
                var content = new StringContent(
                    JsonSerializer.Serialize(new { tableId = tableId }), 
                    System.Text.Encoding.UTF8, 
                    "application/json");
                
                var response = await _httpClient.PostAsync(url, content);
                
                _logger?.LogInformation($"Response status code: {(int)response.StatusCode} {response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogError($"API returned error: {errorContent}");
                    return null;
                }
                
                try
                {
                    var queueEntry = await response.Content.ReadFromJsonAsync<QueueEntryDto>();
                    _logger?.LogInformation($"Successfully called next queue entry: ID={queueEntry?.QueueId}, Customer={queueEntry?.CustomerName}");
                    return queueEntry;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to deserialize queue entry response");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Failed to call next in queue for outlet {outletId}");
                return null;
            }
        }

        public async Task<List<TableRecommendationDto>> GetTableRecommendationsAsync(string outletId, string queueId)
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();
                
                var url = $"{_baseApiUrl}/api/v1/outlets/{outletId}/queue/table-recommendation/{queueId}";
                _logger?.LogInformation($"Getting table recommendations: {url}");
                
                var response = await _httpClient.GetAsync(url);
                
                _logger?.LogInformation($"Response status code: {(int)response.StatusCode} {response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogError($"API returned error: {errorContent}");
                    return new List<TableRecommendationDto>();
                }
                
                try
                {
                    var recommendations = await response.Content.ReadFromJsonAsync<List<TableRecommendationDto>>();
                    _logger?.LogInformation($"Successfully retrieved {recommendations?.Count ?? 0} table recommendations");
                    return recommendations ?? new List<TableRecommendationDto>();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to deserialize table recommendations response");
                    return new List<TableRecommendationDto>();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Failed to get table recommendations for queue {queueId} in outlet {outletId}");
                return new List<TableRecommendationDto>();
            }
        }

        public async Task<bool> AssignTableToQueueAsync(string outletId, string queueId, string tableId)
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();
                
                var url = $"{_baseApiUrl}/api/v1/outlets/{outletId}/queue/assign-table";
                _logger?.LogInformation($"Assigning table to queue: {url}");
                
                var content = new StringContent(
                    JsonSerializer.Serialize(new { queueId = queueId, tableId = tableId }), 
                    System.Text.Encoding.UTF8, 
                    "application/json");
                
                var response = await _httpClient.PostAsync(url, content);
                
                _logger?.LogInformation($"Response status code: {(int)response.StatusCode} {response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogError($"API returned error: {errorContent}");
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Failed to assign table {tableId} to queue {queueId} for outlet {outletId}");
                return false;
            }
        }

        public async Task<bool> MarkQueueAsSeatedAsync(string outletId, string queueId)
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();
                
                var url = $"{_baseApiUrl}/api/v1/outlets/{outletId}/queue/{queueId}/seated";
                _logger?.LogInformation($"Marking queue as seated: {url}");
                
                var response = await _httpClient.PostAsync(url, null);
                
                _logger?.LogInformation($"Response status code: {(int)response.StatusCode} {response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogError($"API returned error: {errorContent}");
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Failed to mark queue {queueId} as seated for outlet {outletId}");
                return false;
            }
        }

        public async Task<bool> MarkQueueAsCompletedAsync(string outletId, string queueId)
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();
                
                var url = $"{_baseApiUrl}/api/v1/outlets/{outletId}/queue/{queueId}/completed";
                _logger?.LogInformation($"Marking queue as completed: {url}");
                
                var response = await _httpClient.PostAsync(url, null);
                
                _logger?.LogInformation($"Response status code: {(int)response.StatusCode} {response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogError($"API returned error: {errorContent}");
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Failed to mark queue {queueId} as completed for outlet {outletId}");
                return false;
            }
        }

        public async Task<List<TableTypeInfo>> GetQueueTablesAsync(string outletId)
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();
                
                var url = $"{_baseApiUrl}/api/v1/outlets/{outletId}/table-types/queue";
                _logger?.LogInformation($"Fetching queue tables from: {url}");
                
                var response = await _httpClient.GetAsync(url);
                
                _logger?.LogInformation($"Response status code: {(int)response.StatusCode} {response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogError($"API returned error: {errorContent}");
                    return new List<TableTypeInfo>();
                }
                
                try
                {
                    var tables = await response.Content.ReadFromJsonAsync<List<TableTypeInfo>>();
                    _logger?.LogInformation($"Successfully retrieved {tables?.Count ?? 0} queue tables");
                    return tables ?? new List<TableTypeInfo>();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to deserialize queue tables response");
                    return new List<TableTypeInfo>();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Failed to get queue tables for outlet {outletId}");
                return new List<TableTypeInfo>();
            }
        }
    }
} 