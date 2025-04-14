using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;

namespace FNBReservation.Portal.Services
{
    /// <summary>
    /// Base class for API client services that handles authentication and token refresh
    /// </summary>
    public abstract class ApiClientService
    {
        protected readonly HttpClient _httpClient;
        protected readonly ILogger _logger;
        protected readonly JsonSerializerOptions _jsonOptions;

        protected ApiClientService(IHttpClientFactory httpClientFactory, ILogger logger)
        {
            // Use the named HttpClient that has the ApiAuthorizationHandler configured
            _httpClient = httpClientFactory.CreateClient("API");
            _logger = logger;
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        // Common GET method
        protected async Task<T> GetAsync<T>(string endpoint)
        {
            try
            {
                _logger.LogDebug("Sending GET request to {Endpoint}", endpoint);
                var response = await _httpClient.GetAsync(endpoint);
                
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error during GET request to {Endpoint}: {Message}", endpoint, ex.Message);
                throw;
            }
        }

        // Common POST method
        protected async Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            try
            {
                _logger.LogDebug("Sending POST request to {Endpoint}", endpoint);
                var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(endpoint, content);
                
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error during POST request to {Endpoint}: {Message}", endpoint, ex.Message);
                throw;
            }
        }

        // Common PUT method
        protected async Task<TResponse> PutAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            try
            {
                _logger.LogDebug("Sending PUT request to {Endpoint}", endpoint);
                var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync(endpoint, content);
                
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error during PUT request to {Endpoint}: {Message}", endpoint, ex.Message);
                throw;
            }
        }

        // Common DELETE method
        protected async Task DeleteAsync(string endpoint)
        {
            try
            {
                _logger.LogDebug("Sending DELETE request to {Endpoint}", endpoint);
                var response = await _httpClient.DeleteAsync(endpoint);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error during DELETE request to {Endpoint}: {Message}", endpoint, ex.Message);
                throw;
            }
        }
    }
} 