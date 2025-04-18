using FNBReservation.Portal.Models;
using Microsoft.JSInterop;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FNBReservation.Portal.Services
{
    public class HttpClientReservationService : IReservationService
    {
        private readonly HttpClient _httpClient;
        private readonly JwtTokenService _jwtTokenService;
        private readonly IJSRuntime _jsRuntime;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HttpClientReservationService> _logger;
        private readonly Dictionary<string, string> _outletUuidCache = new Dictionary<string, string>();
        private bool _outletsLoaded = false;

        public HttpClientReservationService(
            HttpClient httpClient,
            JwtTokenService jwtTokenService,
            IJSRuntime jsRuntime,
            IConfiguration configuration,
            ILogger<HttpClientReservationService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
            _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Initialize the outlet cache asynchronously
            // We don't await here since this is a constructor, but the first call to GetOutletUUIDAsync will load outlets
            _ = Task.Run(async () => {
                try {
                    // Give the app a little time to start up before loading outlets
                    await Task.Delay(1000);
                    await LoadAllOutletsAsync();
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "Error initializing outlet cache");
                }
            });
        }

        // Safe method to log to console that won't be called during prerendering
        private async Task LogToConsoleAsync(string message)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("console.log", message);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("prerendering"))
            {
                // Silently ignore JS interop issues during prerendering
            }
            catch (Exception)
            {
                // Ignore other console logging errors - they shouldn't break the app
            }
        }

        public async Task<List<ReservationDto>> GetReservationsAsync(ReservationFilterDto filter)
        {
            try
            {
                List<ReservationDto> allReservations = new List<ReservationDto>();
                
                // If "all" outlets or null is specified, we need to fetch from each outlet
                if (string.IsNullOrEmpty(filter.OutletId) || filter.OutletId == "all")
                {
                    await LogToConsoleAsync("Fetching reservations from all outlets");
                    
                    // Ensure outlets are loaded
                    if (!_outletsLoaded)
                    {
                        await LoadAllOutletsAsync();
                    }
                    
                    // If we have outlet cache, fetch reservations for each outlet
                    if (_outletUuidCache.Count > 0)
                    {
                        foreach (var outletPair in _outletUuidCache)
                        {
                            try
                            {
                                string outletUUID = outletPair.Value;
                                var singleOutletReservations = await FetchReservationsForOutlet(outletUUID, filter);
                                await LogToConsoleAsync($"Found {singleOutletReservations.Count} reservations for outlet {outletPair.Key}");
                                allReservations.AddRange(singleOutletReservations);
                            }
                            catch (Exception ex)
                            {
                                // Log and continue with other outlets if one fails
                                await LogToConsoleAsync($"Error fetching reservations for outlet {outletPair.Key}: {ex.Message}");
                                _logger.LogError(ex, "Error fetching reservations for outlet {OutletId}", outletPair.Key);
                            }
                        }
                    }
                    else
                    {
                        // Fallback if no outlets in cache - try the admin API
                        await LogToConsoleAsync("No outlets in cache, trying admin API");
                        string url = "api/v1/admin/reservations";
                        
                        // Add query parameters
                        var queryParams = BuildQueryParams(filter);
                        if (queryParams.Count > 0)
                        {
                            url += $"?{string.Join("&", queryParams)}";
                        }
                        
                        var response = await _httpClient.GetAsync(url);
                        if (response.IsSuccessStatusCode)
                        {
                            var reservations = await response.Content.ReadFromJsonAsync<List<ReservationDto>>();
                            if (reservations != null)
                            {
                                allReservations.AddRange(reservations);
                            }
                        }
                        else
                        {
                            await LogToConsoleAsync($"Error fetching from admin API: {response.StatusCode}");
                            // If admin API fails, we have to return empty list
                        }
                    }
                }
                else
                {
                    // For specific outlet, use the regular approach
                    string outletUUID = await GetOutletUUIDAsync(filter.OutletId);
                    allReservations = await FetchReservationsForOutlet(outletUUID, filter);
                }

                // Apply any client-side filtering that may be necessary
                // This ensures consistent filtering even if some API endpoints don't support all filters
                return ApplyClientSideFilters(allReservations, filter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reservations with filter");
                await LogToConsoleAsync($"Exception getting reservations: {ex.Message}");
                return new List<ReservationDto>();
            }
        }

        // Helper method to build query parameters
        private List<string> BuildQueryParams(ReservationFilterDto filter)
        {
            var queryParams = new List<string>();
            
            if (!string.IsNullOrEmpty(filter.Status))
            {
                queryParams.Add($"status={Uri.EscapeDataString(filter.Status)}");
            }
            
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                queryParams.Add($"searchTerm={Uri.EscapeDataString(filter.SearchTerm)}");
            }
            
            return queryParams;
        }
        
        // Helper method to fetch reservations for a specific outlet
        private async Task<List<ReservationDto>> FetchReservationsForOutlet(string outletUUID, ReservationFilterDto filter)
        {
            string url = $"api/v1/outlets/{outletUUID}/reservations";
            
            // Add query parameters
            var queryParams = BuildQueryParams(filter);
            if (queryParams.Count > 0)
            {
                url += $"?{string.Join("&", queryParams)}";
            }
            
            await LogToConsoleAsync($"Fetching reservations from: {url}");
            
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var reservations = await response.Content.ReadFromJsonAsync<List<ReservationDto>>();
                    return reservations ?? new List<ReservationDto>();
                }
                catch (JsonException jsonEx)
                {
                    // Handle JSON deserialization errors
                    _logger.LogError(jsonEx, "JSON deserialization error for reservations");
                    await LogToConsoleAsync($"JSON deserialization error: {jsonEx.Message}");
                    
                    // Log the raw JSON to help debug
                    string content = await response.Content.ReadAsStringAsync();
                    await LogToConsoleAsync($"Raw JSON content (first 200 chars): {content.Substring(0, Math.Min(200, content.Length))}...");
                    
                    return new List<ReservationDto>();
                }
            }
            
            await LogToConsoleAsync($"Error fetching reservations: {response.StatusCode}");
            string errorContent = await response.Content.ReadAsStringAsync();
            await LogToConsoleAsync($"Error details: {errorContent}");
            return new List<ReservationDto>();
        }
        
        // Helper method to apply additional client-side filters if needed
        private List<ReservationDto> ApplyClientSideFilters(List<ReservationDto> reservations, ReservationFilterDto filter)
        {
            var filtered = reservations;
            
            // Apply status filter if it wasn't applied by the server
            if (!string.IsNullOrEmpty(filter.Status))
            {
                filtered = filtered.Where(r => r.Status == filter.Status).ToList();
            }
            
            // Apply search filter if it wasn't applied by the server
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.ToLower();
                filtered = filtered.Where(r =>
                    r.CustomerName.ToLower().Contains(searchTerm) ||
                    r.CustomerPhone.ToLower().Contains(searchTerm) ||
                    (r.CustomerEmail != null && r.CustomerEmail.ToLower().Contains(searchTerm)) ||
                    r.ReservationId.ToLower().Contains(searchTerm) ||
                    (r.Notes != null && r.Notes.ToLower().Contains(searchTerm))
                ).ToList();
            }
            
            return filtered;
        }

        // Helper method to get outlet UUID from outlet ID
        private async Task<string> GetOutletUUIDAsync(string outletId)
        {
            try
            {
                // Check if we have a cached UUID for this outletId
                if (_outletUuidCache.TryGetValue(outletId, out string cachedUuid))
                {
                    await LogToConsoleAsync($"Using cached UUID for outlet {outletId}: {cachedUuid}");
                    return cachedUuid;
                }

                // If outlets are not yet loaded, load them all at once
                if (!_outletsLoaded)
                {
                    await LoadAllOutletsAsync();
                    
                    // Check again if we have a cached UUID after loading outlets
                    if (_outletUuidCache.TryGetValue(outletId, out string newlyCachedUuid))
                    {
                        await LogToConsoleAsync($"Using newly cached UUID for outlet {outletId}: {newlyCachedUuid}");
                        return newlyCachedUuid;
                    }
                }
                
                // If we still don't have the UUID, use the outlet ID as fallback
                await LogToConsoleAsync($"No UUID found for outlet ID: {outletId}, using outlet ID as fallback");
                return outletId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting outlet UUID for ID: {OutletId}", outletId);
                await LogToConsoleAsync($"Exception getting outlet UUID: {ex.Message}");
                
                // In case of error, fall back to using the outlet ID
                return outletId;
            }
        }

        // Method to load all outlets and cache their UUIDs
        private async Task LoadAllOutletsAsync()
        {
            try
            {
                string url = "api/v1/admin/outlets";
                await LogToConsoleAsync($"Loading all outlets from: {url}");
                
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var outlets = await response.Content.ReadFromJsonAsync<List<OutletListItem>>();
                    if (outlets != null)
                    {
                        _outletUuidCache.Clear();
                        foreach (var outlet in outlets)
                        {
                            _outletUuidCache[outlet.OutletId] = outlet.Id;
                            await LogToConsoleAsync($"Cached outlet: {outlet.OutletId} -> UUID: {outlet.Id}");
                        }
                        
                        _outletsLoaded = true;
                        await LogToConsoleAsync($"Loaded {outlets.Count} outlets");
                        return;
                    }
                }
                
                await LogToConsoleAsync($"Failed to load outlets: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading all outlets");
                await LogToConsoleAsync($"Exception loading all outlets: {ex.Message}");
            }
        }

        // Helper class to deserialize outlet data
        private class OutletListItem
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }
            
            [JsonPropertyName("outletId")]
            public string OutletId { get; set; }
            
            [JsonPropertyName("name")]
            public string Name { get; set; }
            
            [JsonPropertyName("status")]
            public string Status { get; set; }
        }

        public async Task<ReservationDto?> GetReservationByIdAsync(string reservationId)
        {
            try
            {
                if (string.IsNullOrEmpty(reservationId))
                {
                    return null;
                }

                try
                {
                    // First try to fetch the reservation from each available outlet
                    // since we don't know which outlet this reservation belongs to
                    if (!_outletsLoaded)
                    {
                        await LoadAllOutletsAsync();
                    }

                    foreach (var outletPair in _outletUuidCache)
                    {
                        string outletUUID = outletPair.Value;
                        string url = $"api/v1/outlets/{outletUUID}/reservations/{reservationId}";
                        
                        await LogToConsoleAsync($"Trying reservation in outlet {outletPair.Key}: {url}");
                        
                        var response = await _httpClient.GetAsync(url);
                        
                        if (response.IsSuccessStatusCode)
                        {
                            var reservation = await response.Content.ReadFromJsonAsync<ReservationDto>();
                            await LogToConsoleAsync($"Found reservation in outlet {outletPair.Key}");
                            return reservation;
                        }
                    }
                    
                    // If we couldn't find it in any outlet, try the central reservations API
                    string centralUrl = $"api/v1/reservations/{reservationId}";
                    await LogToConsoleAsync($"Trying central reservations API: {centralUrl}");
                    
                    var centralResponse = await _httpClient.GetAsync(centralUrl);
                    
                    if (centralResponse.IsSuccessStatusCode)
                    {
                        var reservation = await centralResponse.Content.ReadFromJsonAsync<ReservationDto>();
                        await LogToConsoleAsync($"Found reservation in central API");
                        return reservation;
                    }
                    
                    await LogToConsoleAsync($"Reservation not found in any outlet or central API");
                    return null;
                }
                catch (Exception ex)
                {
                    await LogToConsoleAsync($"Error finding reservation: {ex.Message}");
                    _logger.LogError(ex, "Error finding reservation across outlets");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reservation by ID: {ReservationId}", reservationId);
                await LogToConsoleAsync($"Exception getting reservation: {ex.Message}");
                return null;
            }
        }

        public async Task<AvailabilityResponseDto> CheckAvailabilityAsync(AvailabilityRequestDto request)
        {
            try
            {
                // Get the outlet UUID from the outlet ID
                string outletUUID = await GetOutletUUIDAsync(request.OutletId);
                
                string url = $"api/v1/outlets/{outletUUID}/availability";
                
                await LogToConsoleAsync($"Checking availability at: {url}");
                
                var response = await _httpClient.PostAsJsonAsync(url, request);
                
                if (response.IsSuccessStatusCode)
                {
                    var availability = await response.Content.ReadFromJsonAsync<AvailabilityResponseDto>();
                    return availability ?? new AvailabilityResponseDto { Available = false, Message = "Failed to parse response" };
                }
                
                await LogToConsoleAsync($"Error checking availability: {response.StatusCode}");
                string errorContent = await response.Content.ReadAsStringAsync();
                await LogToConsoleAsync($"Error details: {errorContent}");
                return new AvailabilityResponseDto { Available = false, Message = $"Error: {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking availability for outlet: {OutletId}", request.OutletId);
                await LogToConsoleAsync($"Exception checking availability: {ex.Message}");
                return new AvailabilityResponseDto { Available = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<ReservationDto?> CreateReservationAsync(CreateReservationDto request)
        {
            try
            {
                // Get the outlet UUID from the outlet ID
                string outletUUID = await GetOutletUUIDAsync(request.OutletId);
                
                string url = $"api/v1/outlets/{outletUUID}/reservations";
                
                await LogToConsoleAsync($"Creating reservation at: {url}");
                
                var response = await _httpClient.PostAsJsonAsync(url, request);
                
                if (response.IsSuccessStatusCode)
                {
                    var reservation = await response.Content.ReadFromJsonAsync<ReservationDto>();
                    return reservation;
                }
                
                await LogToConsoleAsync($"Error creating reservation: {response.StatusCode}");
                string errorContent = await response.Content.ReadAsStringAsync();
                await LogToConsoleAsync($"Error details: {errorContent}");
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating reservation for outlet: {OutletId}", request.OutletId);
                await LogToConsoleAsync($"Exception creating reservation: {ex.Message}");
                return null;
            }
        }

        public async Task<ReservationDto?> UpdateReservationAsync(UpdateReservationDto request)
        {
            try
            {
                // We need the reservationId and outletId for the API endpoint
                if (string.IsNullOrEmpty(request.ReservationId))
                {
                    return null;
                }
                
                // First get the reservation to determine its outlet ID
                var existingReservation = await GetReservationByIdAsync(request.ReservationId);
                if (existingReservation == null)
                {
                    return null;
                }
                
                // Get the outlet UUID from the outlet ID
                string outletUUID = await GetOutletUUIDAsync(existingReservation.OutletId);
                
                string url = $"api/v1/outlets/{outletUUID}/reservations/{request.ReservationId}";
                
                await LogToConsoleAsync($"Updating reservation at: {url}");
                
                var response = await _httpClient.PutAsJsonAsync(url, request);
                
                if (response.IsSuccessStatusCode)
                {
                    var reservation = await response.Content.ReadFromJsonAsync<ReservationDto>();
                    return reservation;
                }
                
                await LogToConsoleAsync($"Error updating reservation: {response.StatusCode}");
                string errorContent = await response.Content.ReadAsStringAsync();
                await LogToConsoleAsync($"Error details: {errorContent}");
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating reservation: {ReservationId}", request.ReservationId);
                await LogToConsoleAsync($"Exception updating reservation: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> CancelReservationAsync(string reservationId, string reason)
        {
            try
            {
                if (string.IsNullOrEmpty(reservationId))
                {
                    return false;
                }
                
                // First get the reservation to determine its outlet ID
                var existingReservation = await GetReservationByIdAsync(reservationId);
                if (existingReservation == null)
                {
                    return false;
                }
                
                // Get the outlet UUID from the outlet ID
                string outletUUID = await GetOutletUUIDAsync(existingReservation.OutletId);
                
                string url = $"api/v1/outlets/{outletUUID}/reservations/{reservationId}/cancel";
                
                await LogToConsoleAsync($"Cancelling reservation at: {url}");
                
                var cancelRequest = new { Reason = reason };
                var response = await _httpClient.PutAsJsonAsync(url, cancelRequest);
                
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                
                await LogToConsoleAsync($"Error cancelling reservation: {response.StatusCode}");
                string errorContent = await response.Content.ReadAsStringAsync();
                await LogToConsoleAsync($"Error details: {errorContent}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling reservation: {ReservationId}", reservationId);
                await LogToConsoleAsync($"Exception cancelling reservation: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> MarkAsNoShowAsync(string reservationId)
        {
            try
            {
                if (string.IsNullOrEmpty(reservationId))
                {
                    return false;
                }
                
                // First get the reservation to determine its outlet ID
                var existingReservation = await GetReservationByIdAsync(reservationId);
                if (existingReservation == null)
                {
                    return false;
                }
                
                // Get the outlet UUID from the outlet ID
                string outletUUID = await GetOutletUUIDAsync(existingReservation.OutletId);
                
                string url = $"api/v1/outlets/{outletUUID}/reservations/{reservationId}/no-show";
                
                await LogToConsoleAsync($"Marking reservation as no-show at: {url}");
                
                var response = await _httpClient.PutAsync(url, null);
                
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                
                await LogToConsoleAsync($"Error marking reservation as no-show: {response.StatusCode}");
                string errorContent = await response.Content.ReadAsStringAsync();
                await LogToConsoleAsync($"Error details: {errorContent}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking reservation as no-show: {ReservationId}", reservationId);
                await LogToConsoleAsync($"Exception marking reservation as no-show: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> MarkAsCompletedAsync(string reservationId)
        {
            try
            {
                if (string.IsNullOrEmpty(reservationId))
                {
                    return false;
                }
                
                // First get the reservation to determine its outlet ID
                var existingReservation = await GetReservationByIdAsync(reservationId);
                if (existingReservation == null)
                {
                    return false;
                }
                
                // Get the outlet UUID from the outlet ID
                string outletUUID = await GetOutletUUIDAsync(existingReservation.OutletId);
                
                string url = $"api/v1/outlets/{outletUUID}/reservations/{reservationId}/complete";
                
                await LogToConsoleAsync($"Marking reservation as completed at: {url}");
                
                var response = await _httpClient.PutAsync(url, null);
                
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                
                await LogToConsoleAsync($"Error marking reservation as completed: {response.StatusCode}");
                string errorContent = await response.Content.ReadAsStringAsync();
                await LogToConsoleAsync($"Error details: {errorContent}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking reservation as completed: {ReservationId}", reservationId);
                await LogToConsoleAsync($"Exception marking reservation as completed: {ex.Message}");
                return false;
            }
        }

        // Implementation for IReservationService interface
        public async Task<bool> CheckInReservationAsync(string reservationId)
        {
            try
            {
                if (string.IsNullOrEmpty(reservationId))
                {
                    return false;
                }
                
                // First get the reservation to determine its outlet ID
                var existingReservation = await GetReservationByIdAsync(reservationId);
                if (existingReservation == null)
                {
                    return false;
                }
                
                // Get the outlet UUID from the outlet ID
                string outletUUID = await GetOutletUUIDAsync(existingReservation.OutletId);
                
                string url = $"api/v1/outlets/{outletUUID}/reservations/{reservationId}/check-in";
                
                await LogToConsoleAsync($"Checking in reservation at: {url}");
                
                var response = await _httpClient.PutAsync(url, null);
                
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                
                await LogToConsoleAsync($"Error checking in reservation: {response.StatusCode}");
                string errorContent = await response.Content.ReadAsStringAsync();
                await LogToConsoleAsync($"Error details: {errorContent}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking in reservation: {ReservationId}", reservationId);
                await LogToConsoleAsync($"Exception checking in reservation: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CheckOutReservationAsync(string reservationId)
        {
            try
            {
                if (string.IsNullOrEmpty(reservationId))
                {
                    return false;
                }
                
                // First get the reservation to determine its outlet ID
                var existingReservation = await GetReservationByIdAsync(reservationId);
                if (existingReservation == null)
                {
                    return false;
                }
                
                // Get the outlet UUID from the outlet ID
                string outletUUID = await GetOutletUUIDAsync(existingReservation.OutletId);
                
                string url = $"api/v1/outlets/{outletUUID}/reservations/{reservationId}/check-out";
                
                await LogToConsoleAsync($"Checking out reservation at: {url}");
                
                var response = await _httpClient.PutAsync(url, null);
                
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                
                await LogToConsoleAsync($"Error checking out reservation: {response.StatusCode}");
                string errorContent = await response.Content.ReadAsStringAsync();
                await LogToConsoleAsync($"Error details: {errorContent}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking out reservation: {ReservationId}", reservationId);
                await LogToConsoleAsync($"Exception checking out reservation: {ex.Message}");
                return false;
            }
        }

        public async Task<List<ReservationDto>> GetReservationsByOutletAndDateAsync(string outletId, DateTime date)
        {
            try
            {
                // Get the outlet UUID from the outlet ID
                string outletUUID = await GetOutletUUIDAsync(outletId);
                
                string formattedDate = date.ToString("yyyy-MM-dd");
                string url = $"api/v1/outlets/{outletUUID}/reservations?date={formattedDate}";
                
                await LogToConsoleAsync($"Fetching reservations by outlet and date from: {url}");
                
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var reservations = await response.Content.ReadFromJsonAsync<List<ReservationDto>>();
                    return reservations ?? new List<ReservationDto>();
                }
                
                await LogToConsoleAsync($"Error fetching reservations by outlet and date: {response.StatusCode}");
                string errorContent = await response.Content.ReadAsStringAsync();
                await LogToConsoleAsync($"Error details: {errorContent}");
                return new List<ReservationDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reservations by outlet: {OutletId} and date: {Date}", outletId, date);
                await LogToConsoleAsync($"Exception getting reservations by outlet and date: {ex.Message}");
                return new List<ReservationDto>();
            }
        }
    }
} 