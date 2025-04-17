using FNBReservation.Portal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace FNBReservation.Portal.Models
{
    public class StaffCustomerViewModel
    {
        private readonly ICustomerService _customerService;
        private readonly CurrentUserService _userService;
        private readonly IJSRuntime _jsRuntime;
        private readonly ISnackbar _snackbar;
        private readonly IDialogService _dialogService;
        private bool _isInitialized = false;
        private string _currentOutletId;

        public StaffCustomerViewModel(
            ICustomerService customerService,
            CurrentUserService userService,
            IJSRuntime jsRuntime,
            ISnackbar snackbar,
            IDialogService dialogService)
        {
            _customerService = customerService;
            _userService = userService;
            _jsRuntime = jsRuntime;
            _snackbar = snackbar;
            _dialogService = dialogService;
        }

        public List<CustomerDto> AllCustomers { get; private set; } = new();
        public List<CustomerDto> ActiveCustomers { get; private set; } = new();
        public List<CustomerDto> BannedCustomers { get; private set; } = new();
        public bool IsLoading { get; private set; } = true;
        public string SearchTerm { get; set; } = "";
        public int ActiveTabIndex { get; set; } = 0;
        public string CurrentUsername { get; private set; } = "Staff";
        public Exception LoadException { get; private set; } = null;
        
        public async Task InitializeAsync()
        {
            try
            {
                _isInitialized = true;
                
                // Get the current user's outlet ID
                _currentOutletId = await _userService.GetCurrentOutletIdAsync();
                if (string.IsNullOrEmpty(_currentOutletId))
                {
                    await LogToConsole("error", "Failed to get current outlet ID. Using sample value for testing.");
                    // Use sample outlet ID from the requirements for testing purposes
                    _currentOutletId = "73a3ef70-e570-4edd-85d5-f7a2802bc008";
                }
                
                // Get the current username
                CurrentUsername = await _userService.GetCurrentUsernameAsync();
                
                // Initialize the customer service
                if (_customerService is HttpClientCustomerService httpClientService)
                {
                    await httpClientService.InitializeAsync();
                }
                
                await LoadCustomersAsync();
            }
            catch (Exception ex)
            {
                LoadException = ex;
                await LogToConsole("error", $"Error during initialization: {ex.Message}");
                _snackbar.Add($"Error initializing: {ex.Message}", Severity.Error);
            }
        }
        
        public async Task LoadCustomersAsync()
        {
            IsLoading = true;
            
            try
            {
                await LogToConsole("log", $"Loading customers for outlet {_currentOutletId}");
                
                // Load all customer lists in parallel for better performance
                var allTask = _customerService.GetOutletCustomersAsync(_currentOutletId, SearchTerm);
                var activeTask = _customerService.GetOutletActiveCustomersAsync(_currentOutletId, SearchTerm);
                var bannedTask = _customerService.GetOutletBannedCustomersAsync(_currentOutletId, SearchTerm);
                
                await Task.WhenAll(allTask, activeTask, bannedTask);
                
                AllCustomers = allTask.Result;
                ActiveCustomers = activeTask.Result;
                BannedCustomers = bannedTask.Result;
                
                await LogToConsole("log", $"Loaded {AllCustomers.Count} total customers, {ActiveCustomers.Count} active, {BannedCustomers.Count} banned");
                
                // Log detailed info about banned customers to debug the issue
                if (BannedCustomers.Any())
                {
                    await LogToConsole("log", "=== Banned Customers Details ===");
                    foreach (var customer in BannedCustomers)
                    {
                        // Fix any invalid GUIDs in the customer ID
                        if (string.IsNullOrEmpty(customer.CustomerId) || customer.CustomerId == "00000000-0000-0000-0000-000000000000")
                        {
                            // If we have a name and phone, try to find a matching customer in AllCustomers
                            var matchingCustomer = AllCustomers.FirstOrDefault(c => 
                                c.Name == customer.Name && 
                                c.PhoneNumber == customer.PhoneNumber &&
                                !string.IsNullOrEmpty(c.CustomerId) && 
                                c.CustomerId != "00000000-0000-0000-0000-000000000000");
                            
                            if (matchingCustomer != null)
                            {
                                customer.CustomerId = matchingCustomer.CustomerId;
                                await LogToConsole("log", $"Fixed missing ID for customer {customer.Name} by matching with AllCustomers list");
                            }
                        }
                        
                        // Ensure the IsBanned flag is set for banned customers
                        if (!customer.IsBanned)
                        {
                            customer.IsBanned = true;
                            await LogToConsole("log", $"Fixed IsBanned flag for customer: {customer.Name}");
                        }
                        
                        await LogToConsole("log", $"Customer: {customer.Name}, ID: {customer.CustomerId}, " +
                                              $"IsBanned: {customer.IsBanned}, " +
                                              $"Reason: {customer.BanReason ?? "N/A"}, " +
                                              $"BannedDate: {customer.BannedDate?.ToString("yyyy-MM-dd") ?? "N/A"}, " +
                                              $"BannedBy: {customer.BannedBy ?? "N/A"}");
                    }
                    await LogToConsole("log", "=============================");
                }
                else
                {
                    await LogToConsole("warning", "No banned customers were found in the response.");
                }
                
                // Ensure banned customers have proper IDs and data
                foreach (var bannedCustomer in BannedCustomers)
                {
                    await EnsureValidCustomerData(bannedCustomer);
                }
            }
            catch (Exception ex)
            {
                LoadException = ex;
                await LogToConsole("error", $"Error loading customers: {ex.Message}");
                _snackbar.Add($"Error loading customers: {ex.Message}", Severity.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        private async Task EnsureValidCustomerData(CustomerDto customer)
        {
            try
            {
                // Convert empty or all-zero GUIDs to a valid GUID
                if (string.IsNullOrEmpty(customer.CustomerId) || customer.CustomerId == "00000000-0000-0000-0000-000000000000")
                {
                    if (!string.IsNullOrEmpty(customer.Name) && !string.IsNullOrEmpty(customer.PhoneNumber))
                    {
                        // Generate a deterministic GUID based on name and phone number
                        string seed = $"{customer.Name}:{customer.PhoneNumber}";
                        var md5 = System.Security.Cryptography.MD5.Create();
                        byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(seed);
                        byte[] hashBytes = md5.ComputeHash(inputBytes);
                        
                        customer.CustomerId = new Guid(hashBytes).ToString();
                        await LogToConsole("log", $"Generated deterministic ID for customer {customer.Name}: {customer.CustomerId}");
                    }
                    else
                    {
                        // If we can't generate a deterministic ID, use a random one
                        customer.CustomerId = Guid.NewGuid().ToString();
                        await LogToConsole("log", $"Generated random ID for customer with missing name/phone: {customer.CustomerId}");
                    }
                }
                
                // Ensure ban data is valid
                if (customer.IsBanned)
                {
                    if (string.IsNullOrEmpty(customer.BanReason))
                    {
                        customer.BanReason = "Not specified";
                    }
                    
                    if (!customer.BannedDate.HasValue)
                    {
                        customer.BannedDate = DateTime.Now;
                    }
                    
                    if (string.IsNullOrEmpty(customer.BannedBy))
                    {
                        customer.BannedBy = "Admin";
                    }
                }
            }
            catch (Exception ex)
            {
                await LogToConsole("error", $"Error ensuring valid customer data: {ex.Message}");
            }
        }
        
        public async Task SearchCustomersAsync()
        {
            try
            {
                await LogToConsole("log", $"Searching for customers with term: '{SearchTerm}'");
                
                await LoadCustomersAsync();
                
                if (string.IsNullOrWhiteSpace(SearchTerm))
                {
                    return;
                }
                
                if (AllCustomers.Count == 0)
                {
                    _snackbar.Add($"No customers found matching '{SearchTerm}'", Severity.Info);
                }
                else
                {
                    _snackbar.Add($"Found {AllCustomers.Count} customers matching '{SearchTerm}'", Severity.Success);
                }
            }
            catch (Exception ex)
            {
                await LogToConsole("error", $"Search error: {ex.Message}");
                _snackbar.Add($"Search error: {ex.Message}", Severity.Error);
            }
        }
        
        public async Task ClearSearchAsync()
        {
            SearchTerm = "";
            await LogToConsole("log", "Customer search cleared");
            await LoadCustomersAsync();
        }
        
        public async Task SearchOnEnterAsync(KeyboardEventArgs e)
        {
            if (e.Key == "Enter")
            {
                await LogToConsole("log", "Customer search triggered by Enter key");
                await SearchCustomersAsync();
            }
        }
        
        public async Task<CustomerDto> GetCustomerDetailsAsync(string customerId)
        {
            try
            {
                await LogToConsole("log", $"Getting details for customer {customerId}");
                
                var customer = await _customerService.GetOutletCustomerByIdAsync(_currentOutletId, customerId);
                
                if (customer == null)
                {
                    _snackbar.Add("Customer not found", Severity.Warning);
                    return null;
                }
                
                // Get customer reservations if not already loaded
                if (customer.ReservationHistory.Count == 0)
                {
                    await LogToConsole("log", "Customer has no reservation history, fetching separately");
                    var reservations = await _customerService.GetOutletCustomerReservationsAsync(_currentOutletId, customerId);
                    
                    if (reservations != null && reservations.Any())
                    {
                        customer.ReservationHistory = reservations.Select(r => new ReservationHistoryItem
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
                }
                
                return customer;
            }
            catch (Exception ex)
            {
                await LogToConsole("error", $"Error getting customer details: {ex.Message}");
                _snackbar.Add($"Error getting customer details: {ex.Message}", Severity.Error);
                return null;
            }
        }
        
        public async Task<bool> BanCustomerAsync(string customerId, string reason, string notes, DateTime? expiryDate)
        {
            try
            {
                var result = await _customerService.BanCustomerAsync(customerId, reason, notes, expiryDate);
                
                if (result)
                {
                    await LoadCustomersAsync();
                    _snackbar.Add("Customer banned successfully", Severity.Success);
                }
                else
                {
                    _snackbar.Add("Failed to ban customer", Severity.Error);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                await LogToConsole("error", $"Error banning customer: {ex.Message}");
                _snackbar.Add($"Error banning customer: {ex.Message}", Severity.Error);
                return false;
            }
        }
        
        public async Task<bool> UnbanCustomerAsync(string customerId)
        {
            try
            {
                var result = await _customerService.UnbanCustomerAsync(customerId);
                
                if (result)
                {
                    await LoadCustomersAsync();
                    _snackbar.Add("Customer unbanned successfully", Severity.Success);
                }
                else
                {
                    _snackbar.Add("Failed to unban customer", Severity.Error);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                await LogToConsole("error", $"Error unbanning customer: {ex.Message}");
                _snackbar.Add($"Error unbanning customer: {ex.Message}", Severity.Error);
                return false;
            }
        }
        
        private async Task LogToConsole(string level, string message)
        {
            try
            {
                if (_isInitialized)
                {
                    await _jsRuntime.InvokeVoidAsync($"console.{level}", $"[StaffCustomerViewModel] {message}");
                }
            }
            catch (Exception)
            {
                // Ignore JS interop errors during pre-rendering
            }
        }
    }
} 