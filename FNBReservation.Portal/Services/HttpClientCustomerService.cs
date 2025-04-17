using System.Text.Json;
using Microsoft.JSInterop;
using FNBReservation.Portal.Models;
using Microsoft.Extensions.Logging;

namespace FNBReservation.Portal.Services
{
    public class HttpClientCustomerService : ApiClientService, ICustomerService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly ILogger<HttpClientCustomerService> _logger;
        private readonly JwtTokenService _jwtTokenService;
        private readonly IConfiguration _configuration;
        private readonly string _baseUrl;
        private bool _isInitialized = false;

        public HttpClientCustomerService(
            HttpClient httpClient,
            JwtTokenService jwtTokenService,
            IJSRuntime jsRuntime,
            IConfiguration configuration,
            ILogger<HttpClientCustomerService> logger)
            : base(new DummyHttpClientFactory(httpClient), logger)
        {
            _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
            _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Get base URL and ensure it doesn't end with a trailing slash
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";
            _baseUrl = _baseUrl.TrimEnd('/');
        }
        
        // Helper class to create an IHttpClientFactory that returns a specific HttpClient
        private class DummyHttpClientFactory : IHttpClientFactory
        {
            private readonly HttpClient _httpClient;

            public DummyHttpClientFactory(HttpClient httpClient)
            {
                _httpClient = httpClient;
            }

            public HttpClient CreateClient(string name)
            {
                return _httpClient;
            }
        }
        
        // Helper method for safe JS interop
        private async Task LogToConsoleAsync(string level, string message)
        {
            try
            {
                // Only invoke JS functions if we've been initialized and this isn't during static rendering
                if (_isInitialized)
                {
                    await _jsRuntime.InvokeVoidAsync($"console.{level}", message);
                }
            }
            catch (InvalidOperationException)
            {
                // Ignore JS interop errors during pre-rendering
            }
            catch (Exception ex)
            {
                // Log error without using JS interop
                _logger.LogError(ex, "Error trying to log to console: {Message}", message);
            }
        }

        // Initialize the service after component has rendered
        public async Task InitializeAsync()
        {
            _isInitialized = true;
            await LogToConsoleAsync("log", "HttpClientCustomerService initialized");
        }
        
        public async Task<List<CustomerDto>> GetCustomersAsync(string? searchTerm = null)
        {
            try
            {
                await LogToConsoleAsync("log", $"Getting customers with search term: {searchTerm}");
                
                // Debug the base URL
                await LogToConsoleAsync("log", $"Base URL from config: {_baseUrl}");
                
                string url = $"{_baseUrl}/api/v1/admin/customers";
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    url += $"?searchTerm={Uri.EscapeDataString(searchTerm)}";
                }
                
                // Log the complete URL
                await LogToConsoleAsync("log", $"Calling API URL: {url}");
                
                var response = await _httpClient.GetAsync(url);
                
                // Log response status code
                await LogToConsoleAsync("log", $"Response status code: {(int)response.StatusCode} ({response.StatusCode})");
                
                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    await LogToConsoleAsync("error", $"API error response: {errorContent}");
                    return new List<CustomerDto>();
                }
                
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync();
                await LogToConsoleAsync("log", $"Customer response received with length: {responseContent.Length}");
                
                var customerListResponse = JsonSerializer.Deserialize<ApiResponse>(responseContent, _jsonOptions);
                
                if (customerListResponse?.Customers == null)
                {
                    await LogToConsoleAsync("log", "No customers found in response");
                    return new List<CustomerDto>();
                }
                
                // Map the API response to our DTO model
                var customers = customerListResponse.Customers.Select(MapApiCustomerToDto).ToList();
                
                await LogToConsoleAsync("log", $"Mapped {customers.Count} customers");
                return customers;
            }
            catch (Exception ex)
            {
                await LogToConsoleAsync("error", $"Error getting customers: {ex.Message}");
                _logger.LogError(ex, "Error getting customers");
                return new List<CustomerDto>();
            }
        }
        
        public async Task<CustomerDto?> GetCustomerByIdAsync(string customerId)
        {
            try
            {
                await LogToConsoleAsync("log", $"Getting customer by ID: {customerId}");
                
                string url = $"{_baseUrl}/api/v1/admin/customers/{customerId}";
                await LogToConsoleAsync("log", $"Calling API URL: {url}");
                
                var response = await _httpClient.GetAsync(url);
                
                // Log response status code
                await LogToConsoleAsync("log", $"Response status code: {(int)response.StatusCode} ({response.StatusCode})");
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await LogToConsoleAsync("log", "Customer not found");
                    return null;
                }
                
                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    await LogToConsoleAsync("error", $"API error response: {errorContent}");
                    return null;
                }
                
                var responseContent = await response.Content.ReadAsStringAsync();
                await LogToConsoleAsync("log", $"Customer response: {responseContent}");
                
                // First, try to deserialize as a detailed customer (with reservation history)
                var detailedCustomer = JsonSerializer.Deserialize<ApiCustomerDetail>(responseContent, _jsonOptions);
                
                if (detailedCustomer != null)
                {
                    await LogToConsoleAsync("log", $"Successfully deserialized customer with reservation history. History count: {detailedCustomer.ReservationHistory?.Count ?? 0}");
                    return MapApiCustomerToDto(detailedCustomer);
                }
                
                // If that fails, try to deserialize as a regular customer
                var apiCustomer = JsonSerializer.Deserialize<ApiCustomer>(responseContent, _jsonOptions);
                
                if (apiCustomer == null)
                {
                    await LogToConsoleAsync("log", "Failed to deserialize customer");
                    return null;
                }
                
                // If there's reservation history in the JSON but not in our model, try to get it separately
                if (responseContent.Contains("reservationHistory") && !responseContent.Contains("\"reservationHistory\":[]") && !responseContent.Contains("\"reservationHistory\": []"))
                {
                    await LogToConsoleAsync("log", "Found reservation history in JSON but not in model, trying manual extraction");
                    try
                    {
                        // Try to get reservations separately
                        await GetCustomerReservationsAsync(customerId, apiCustomer);
                    }
                    catch (Exception ex)
                    {
                        await LogToConsoleAsync("error", $"Error extracting reservations: {ex.Message}");
                    }
                }
                
                return MapApiCustomerToDto(apiCustomer);
            }
            catch (Exception ex)
            {
                await LogToConsoleAsync("error", $"Error getting customer: {ex.Message}");
                _logger.LogError(ex, "Error getting customer by ID {CustomerId}", customerId);
                return null;
            }
        }
        
        private async Task GetCustomerReservationsAsync(string customerId, ApiCustomer customer)
        {
            string url = $"{_baseUrl}/api/v1/admin/customers/{customerId}/reservations";
            await LogToConsoleAsync("log", $"Getting customer reservations from: {url}");
            
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                await LogToConsoleAsync("error", $"Failed to get reservations: {response.StatusCode}");
                return;
            }
            
            var content = await response.Content.ReadAsStringAsync();
            await LogToConsoleAsync("log", $"Reservations response: {content}");
            
            // If customer is already ApiCustomerDetail, we don't need to do anything
            if (customer is ApiCustomerDetail detailedCustomer)
            {
                return;
            }
            
            try
            {
                var reservations = JsonSerializer.Deserialize<List<ApiReservation>>(content, _jsonOptions);
                if (reservations != null && reservations.Any())
                {
                    // Create a new detailed customer with the reservations
                    var newDetailedCustomer = new ApiCustomerDetail
                    {
                        Id = customer.Id,
                        Name = customer.Name,
                        Phone = customer.Phone,
                        Email = customer.Email,
                        Status = customer.Status,
                        TotalReservations = customer.TotalReservations,
                        NoShows = customer.NoShows,
                        NoShowRate = customer.NoShowRate,
                        LastVisit = customer.LastVisit,
                        FirstVisit = customer.FirstVisit,
                        BanInfo = customer.BanInfo,
                        ReservationHistory = reservations
                    };
                    
                    // Replace the customer object
                    customer = newDetailedCustomer;
                    
                    await LogToConsoleAsync("log", $"Added {reservations.Count} reservations to customer");
                }
            }
            catch (Exception ex)
            {
                await LogToConsoleAsync("error", $"Error parsing reservations: {ex.Message}");
            }
        }
        
        public async Task<CustomerDto?> GetCustomerByPhoneAsync(string phoneNumber)
        {
            try
            {
                // Since there is no direct endpoint for this in the AdminCustomerController,
                // we'll get all customers and filter by phone number
                var allCustomers = await GetCustomersAsync(phoneNumber);
                return allCustomers.FirstOrDefault(c => c.PhoneNumber == phoneNumber);
            }
            catch (Exception ex)
            {
                await LogToConsoleAsync("error", $"Error getting customer by phone: {ex.Message}");
                _logger.LogError(ex, "Error getting customer by phone {PhoneNumber}", phoneNumber);
                return null;
            }
        }
        
        public async Task<bool> BanCustomerAsync(string customerId, string reason = "", string notes = "", DateTime? expiryDate = null)
        {
            try
            {
                await LogToConsoleAsync("log", $"Banning customer: {customerId}, Reason: {reason}");
                
                var banRequest = new BanCustomerRequest
                {
                    CustomerId = Guid.Parse(customerId),
                    Reason = reason,
                    DurationDays = expiryDate.HasValue 
                        ? (int)(expiryDate.Value - DateTime.Now).TotalDays 
                        : 0  // 0 means permanent ban
                };
                
                var content = new StringContent(
                    JsonSerializer.Serialize(banRequest, _jsonOptions),
                    System.Text.Encoding.UTF8,
                    "application/json");
                
                var response = await _httpClient.PostAsync(
                    $"{_baseUrl}/api/v1/admin/customers/{customerId}/ban", 
                    content);
                
                response.EnsureSuccessStatusCode();
                
                await LogToConsoleAsync("log", "Customer ban successful");
                return true;
            }
            catch (Exception ex)
            {
                await LogToConsoleAsync("error", $"Error banning customer: {ex.Message}");
                _logger.LogError(ex, "Error banning customer {CustomerId}", customerId);
                return false;
            }
        }
        
        public async Task<bool> BanNewCustomerAsync(string name, string phoneNumber, string email, string reason, string notes, DateTime? expiryDate = null)
        {
            // This method is not directly supported by the API, so we'll need to 
            // check if the customer exists first and then ban them
            try
            {
                await LogToConsoleAsync("log", $"Attempting to ban new customer: {name}, {phoneNumber}");
                
                var customer = await GetCustomerByPhoneAsync(phoneNumber);
                
                if (customer != null)
                {
                    // Customer exists, ban them
                    return await BanCustomerAsync(customer.CustomerId, reason, notes, expiryDate);
                }
                
                // Customer doesn't exist in the system yet
                // This is not supported by the current API
                await LogToConsoleAsync("log", "Customer does not exist and cannot be created/banned through this API");
                return false;
            }
            catch (Exception ex)
            {
                await LogToConsoleAsync("error", $"Error banning new customer: {ex.Message}");
                _logger.LogError(ex, "Error banning new customer with phone {PhoneNumber}", phoneNumber);
                return false;
            }
        }
        
        public async Task<bool> UnbanCustomerAsync(string customerId)
        {
            try
            {
                await LogToConsoleAsync("log", $"Removing ban for customer: {customerId}");
                
                var response = await _httpClient.PostAsync(
                    $"{_baseUrl}/api/v1/admin/customers/{customerId}/remove-ban", 
                    null);
                
                response.EnsureSuccessStatusCode();
                
                await LogToConsoleAsync("log", "Customer ban removed successfully");
                return true;
            }
            catch (Exception ex)
            {
                await LogToConsoleAsync("error", $"Error removing customer ban: {ex.Message}");
                _logger.LogError(ex, "Error removing ban for customer {CustomerId}", customerId);
                return false;
            }
        }
        
        public async Task<bool> UpdateCustomerBanAsync(string customerId, string reason, string notes, DateTime? expiryDate = null)
        {
            // This is not directly supported by the API, so we'll need to remove the ban and then ban again
            try
            {
                await LogToConsoleAsync("log", $"Updating ban for customer: {customerId}");
                
                // First remove the existing ban
                var unbanned = await UnbanCustomerAsync(customerId);
                if (!unbanned)
                {
                    await LogToConsoleAsync("log", "Failed to remove existing ban");
                    return false;
                }
                
                // Then apply the new ban
                return await BanCustomerAsync(customerId, reason, notes, expiryDate);
            }
            catch (Exception ex)
            {
                await LogToConsoleAsync("error", $"Error updating customer ban: {ex.Message}");
                _logger.LogError(ex, "Error updating ban for customer {CustomerId}", customerId);
                return false;
            }
        }
        
        public async Task<bool> AddCustomerNoteAsync(string customerId, string note)
        {
            // Not directly supported by current API
            await LogToConsoleAsync("log", "Adding customer notes is not supported by the current API");
            return false;
        }
        
        // Helper methods to map between API models and our DTOs
        private CustomerDto MapApiCustomerToDto(ApiCustomer apiCustomer)
        {
            var dto = new CustomerDto
            {
                CustomerId = apiCustomer.Id.ToString(),
                Name = apiCustomer.Name,
                PhoneNumber = apiCustomer.Phone,
                Email = apiCustomer.Email,
                IsBanned = apiCustomer.Status?.ToLower() == "banned",
                TotalReservations = apiCustomer.TotalReservations,
                NoShows = apiCustomer.NoShows,
                LastVisit = apiCustomer.LastVisit,
                FirstVisit = apiCustomer.FirstVisit
            };
            
            // Map ban info if available
            if (apiCustomer.BanInfo != null)
            {
                dto.BanReason = apiCustomer.BanInfo.Reason;
                dto.BannedDate = apiCustomer.BanInfo.BannedAt;
                dto.BannedBy = apiCustomer.BanInfo.BannedByName;
                dto.BanExpiryDate = apiCustomer.BanInfo.EndsAt;
            }
            
            // Map reservation history if available for detailed customer
            if (apiCustomer is ApiCustomerDetail detailedCustomer && detailedCustomer.ReservationHistory != null)
            {
                dto.ReservationHistory = detailedCustomer.ReservationHistory.Select(r => new ReservationHistoryItem
                {
                    ReservationId = r.ReservationId.ToString(),
                    ReservationDate = r.Date,
                    OutletId = r.OutletId.ToString(),
                    OutletName = r.OutletName,
                    GuestCount = r.PartySize,
                    Status = r.Status,
                    Notes = r.SpecialRequests
                }).ToList();
            }
            
            return dto;
        }
        
        // API response model classes to match the API's structure
        private class ApiResponse
        {
            public List<ApiCustomer> Customers { get; set; } = new List<ApiCustomer>();
            public int TotalCount { get; set; }
            public int Page { get; set; }
            public int PageSize { get; set; }
            public int TotalPages { get; set; }
            public string? SearchTerm { get; set; }
        }
        
        private class ApiCustomer
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public string? Email { get; set; }
            public string Status { get; set; } = "Active";
            public int TotalReservations { get; set; }
            public int NoShows { get; set; }
            public decimal NoShowRate { get; set; }
            public DateTime? LastVisit { get; set; }
            public DateTime? FirstVisit { get; set; }
            public ApiBanInfo? BanInfo { get; set; }
        }
        
        private class ApiCustomerDetail : ApiCustomer
        {
            public List<ApiReservation> ReservationHistory { get; set; } = new List<ApiReservation>();
        }
        
        private class ApiReservation
        {
            public Guid ReservationId { get; set; }
            public string ReservationCode { get; set; } = string.Empty;
            public DateTime Date { get; set; }
            public Guid OutletId { get; set; }
            public string OutletName { get; set; } = string.Empty;
            public int PartySize { get; set; }
            public string Status { get; set; } = string.Empty;
            public string? SpecialRequests { get; set; }
        }
        
        private class ApiBanInfo
        {
            public Guid Id { get; set; }
            public Guid CustomerId { get; set; }
            public string Reason { get; set; } = string.Empty;
            public DateTime BannedAt { get; set; }
            public int DurationDays { get; set; }
            public DateTime? EndsAt { get; set; }
            public Guid BannedById { get; set; }
            public string BannedByName { get; set; } = string.Empty;
        }
        
        private class BanCustomerRequest
        {
            public Guid CustomerId { get; set; }
            public string Reason { get; set; } = string.Empty;
            public int DurationDays { get; set; } // 0 means permanent
        }
    }
} 