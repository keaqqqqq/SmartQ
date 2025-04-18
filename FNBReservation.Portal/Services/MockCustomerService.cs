using FNBReservation.Portal.Models;

namespace FNBReservation.Portal.Services
{
    // Simple class to match the API response for reservations
    public class ApiReservation
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

    public interface ICustomerService
    {
        Task InitializeAsync();
        Task<List<CustomerDto>> GetCustomersAsync(string? searchTerm = null);
        Task<CustomerDto?> GetCustomerByIdAsync(string customerId);
        Task<CustomerDto?> GetCustomerByPhoneAsync(string phoneNumber);
        Task<bool> BanCustomerAsync(string customerId, string reason = "", string notes = "", DateTime? expiryDate = null);
        Task<bool> BanNewCustomerAsync(string name, string phoneNumber, string email, string reason, string notes, DateTime? expiryDate = null);
        Task<bool> UnbanCustomerAsync(string customerId);
        Task<bool> UpdateCustomerBanAsync(string customerId, string reason, string notes, DateTime? expiryDate = null);
        Task<bool> AddCustomerNoteAsync(string customerId, string note);
        
        // New staff-specific methods for outlet customer management
        Task<List<CustomerDto>> GetOutletCustomersAsync(string outletId, string? searchTerm = null);
        Task<List<CustomerDto>> GetOutletActiveCustomersAsync(string outletId, string? searchTerm = null);
        Task<List<CustomerDto>> GetOutletBannedCustomersAsync(string outletId, string? searchTerm = null);
        Task<CustomerDto?> GetOutletCustomerByIdAsync(string outletId, string customerId);
        Task<List<ApiReservation>> GetOutletCustomerReservationsAsync(string outletId, string customerId);
    }

    public class MockCustomerService : ICustomerService
    {
        private List<CustomerDto> _customers;

        public MockCustomerService()
        {
            _customers = GenerateMockCustomers();
        }

        public Task InitializeAsync()
        {
            // Nothing to initialize for mock service
            return Task.CompletedTask;
        }

        public Task<List<CustomerDto>> GetCustomersAsync(string? searchTerm = null)
        {
            var customers = _customers;

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                customers = customers
                    .Where(c =>
                        c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        c.PhoneNumber.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        (c.Email ?? "").Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        c.CustomerId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return Task.FromResult(customers);
        }

        public Task<CustomerDto?> GetCustomerByIdAsync(string customerId)
        {
            var customer = _customers.FirstOrDefault(c => c.CustomerId == customerId);
            return Task.FromResult(customer);
        }

        public Task<CustomerDto?> GetCustomerByPhoneAsync(string phoneNumber)
        {
            var customer = _customers.FirstOrDefault(c => c.PhoneNumber == phoneNumber);
            return Task.FromResult(customer);
        }

        public Task<bool> BanCustomerAsync(string customerId, string reason = "", string notes = "", DateTime? expiryDate = null)
        {
            var customer = _customers.FirstOrDefault(c => c.CustomerId == customerId);
            if (customer == null)
            {
                return Task.FromResult(false);
            }

            customer.IsBanned = true;
            customer.BanReason = reason;
            customer.BannedDate = DateTime.Now;
            customer.BannedBy = "Admin";
            customer.BanExpiryDate = expiryDate;

            if (!string.IsNullOrWhiteSpace(notes))
            {
                var currentNotes = string.IsNullOrEmpty(customer.Notes) ? "" : customer.Notes + "\n\n";
                customer.Notes = currentNotes + $"[{DateTime.Now:G}] [Ban Note] {notes}";
            }

            return Task.FromResult(true);
        }

        public Task<bool> BanNewCustomerAsync(string name, string phoneNumber, string email, string reason, string notes, DateTime? expiryDate = null)
        {
            // Check if customer with this phone number already exists
            var existingCustomer = _customers.FirstOrDefault(c => c.PhoneNumber == phoneNumber);

            if (existingCustomer != null)
            {
                // Ban the existing customer
                return BanCustomerAsync(existingCustomer.CustomerId, reason, notes, expiryDate);
            }

            // Create a new customer
            var newCustomer = new CustomerDto
            {
                CustomerId = $"cust-{_customers.Count + 1}",
                Name = name,
                PhoneNumber = phoneNumber,
                Email = email,
                IsBanned = true,
                BanReason = reason,
                BannedDate = DateTime.Now,
                BannedBy = "Admin",
                BanExpiryDate = expiryDate,
                TotalReservations = 0,
                NoShows = 0
            };

            if (!string.IsNullOrWhiteSpace(notes))
            {
                newCustomer.Notes = $"[{DateTime.Now:G}] [Ban Note] {notes}";
            }

            _customers.Add(newCustomer);
            return Task.FromResult(true);
        }

        public Task<bool> UnbanCustomerAsync(string customerId)
        {
            var customer = _customers.FirstOrDefault(c => c.CustomerId == customerId);
            if (customer == null)
            {
                return Task.FromResult(false);
            }

            customer.IsBanned = false;
            customer.BanExpiryDate = null;

            var currentNotes = string.IsNullOrEmpty(customer.Notes) ? "" : customer.Notes + "\n\n";
            customer.Notes = currentNotes + $"[{DateTime.Now:G}] Ban removed by Admin";

            return Task.FromResult(true);
        }

        public Task<bool> UpdateCustomerBanAsync(string customerId, string reason, string notes, DateTime? expiryDate = null)
        {
            var customer = _customers.FirstOrDefault(c => c.CustomerId == customerId);
            if (customer == null || !customer.IsBanned)
            {
                return Task.FromResult(false);
            }

            customer.BanReason = reason;
            customer.BanExpiryDate = expiryDate;

            if (!string.IsNullOrWhiteSpace(notes))
            {
                var currentNotes = string.IsNullOrEmpty(customer.Notes) ? "" : customer.Notes + "\n\n";
                customer.Notes = currentNotes + $"[{DateTime.Now:G}] [Ban Update] {notes}";
            }

            return Task.FromResult(true);
        }

        public Task<bool> AddCustomerNoteAsync(string customerId, string note)
        {
            var customer = _customers.FirstOrDefault(c => c.CustomerId == customerId);
            if (customer == null || string.IsNullOrWhiteSpace(note))
            {
                return Task.FromResult(false);
            }

            var currentNotes = string.IsNullOrEmpty(customer.Notes) ? "" : customer.Notes + "\n\n";
            customer.Notes = currentNotes + $"[{DateTime.Now:G}] {note}";

            return Task.FromResult(true);
        }

        public Task<List<CustomerDto>> GetOutletCustomersAsync(string outletId, string? searchTerm = null)
        {
            // For mock service, we'll just use the same customer list but pretend they're from the given outlet
            var filteredList = _customers.ToList();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                filteredList = filteredList
                    .Where(c =>
                        c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        c.PhoneNumber.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        (c.Email ?? "").Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        c.CustomerId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Assign the outlet ID to each customer's first reservation for testing purposes
            foreach (var customer in filteredList)
            {
                if (customer.ReservationHistory.Any())
                {
                    customer.ReservationHistory.First().OutletId = outletId;
                }
            }

            return Task.FromResult(filteredList);
        }

        public Task<List<CustomerDto>> GetOutletActiveCustomersAsync(string outletId, string? searchTerm = null)
        {
            var filteredList = _customers.Where(c => !c.IsBanned).ToList();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                filteredList = filteredList
                    .Where(c =>
                        c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        c.PhoneNumber.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        (c.Email ?? "").Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        c.CustomerId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Assign the outlet ID to each customer's first reservation for testing purposes
            foreach (var customer in filteredList)
            {
                if (customer.ReservationHistory.Any())
                {
                    customer.ReservationHistory.First().OutletId = outletId;
                }
            }

            return Task.FromResult(filteredList);
        }

        public Task<List<CustomerDto>> GetOutletBannedCustomersAsync(string outletId, string? searchTerm = null)
        {
            var filteredList = _customers.Where(c => c.IsBanned).ToList();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                filteredList = filteredList
                    .Where(c =>
                        c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        c.PhoneNumber.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        (c.Email ?? "").Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        c.CustomerId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Assign the outlet ID to each customer's first reservation for testing purposes
            foreach (var customer in filteredList)
            {
                if (customer.ReservationHistory.Any())
                {
                    customer.ReservationHistory.First().OutletId = outletId;
                }
            }

            return Task.FromResult(filteredList);
        }

        public Task<CustomerDto?> GetOutletCustomerByIdAsync(string outletId, string customerId)
        {
            var customer = _customers.FirstOrDefault(c => c.CustomerId == customerId);
            
            // In a real implementation, we would check if this customer belongs to the specified outlet
            if (customer != null && customer.ReservationHistory.Any())
            {
                // Set the outlet ID for testing purposes
                customer.ReservationHistory.ForEach(r => r.OutletId = outletId);
            }
            
            return Task.FromResult(customer);
        }

        public Task<List<ApiReservation>> GetOutletCustomerReservationsAsync(string outletId, string customerId)
        {
            var customer = _customers.FirstOrDefault(c => c.CustomerId == customerId);
            
            if (customer == null || !customer.ReservationHistory.Any())
            {
                return Task.FromResult(new List<ApiReservation>());
            }
            
            // Convert ReservationHistoryItem to ApiReservation
            var reservations = customer.ReservationHistory.Select(r => new ApiReservation
            {
                ReservationId = Guid.Parse(r.ReservationId),
                ReservationCode = r.ReservationId,
                Date = r.ReservationDate,
                OutletId = Guid.Parse(outletId),
                OutletName = r.OutletName,
                PartySize = r.GuestCount,
                Status = r.Status,
                SpecialRequests = r.Notes
            }).ToList();
            
            return Task.FromResult(reservations);
        }

        private List<CustomerDto> GenerateMockCustomers()
        {
            var customers = new List<CustomerDto>
            {
                new CustomerDto
                {
                    CustomerId = "cust-123",
                    Name = "John Smith",
                    PhoneNumber = "+1234567890",
                    Email = "john.smith@example.com",
                    IsBanned = false,
                    TotalReservations = 12,
                    NoShows = 1,
                    LastVisit = new DateTime(2025, 2, 11),
                    FirstVisit = new DateTime(2024, 5, 3),
                    Notes = "Prefers window seating",
                    ReservationHistory = new List<ReservationHistoryItem>
                    {
                        new ReservationHistoryItem
                        {
                            ReservationId = "res-101",
                            ReservationDate = new DateTime(2025, 2, 11, 19, 0, 0),
                            OutletId = "A15",
                            OutletName = "Ocean View Restaurant - Downtown",
                            GuestCount = 4,
                            Status = "Completed",
                            Notes = "Anniversary dinner"
                        },
                        new ReservationHistoryItem
                        {
                            ReservationId = "res-089",
                            ReservationDate = new DateTime(2025, 1, 24, 20, 0, 0),
                            OutletId = "A15",
                            OutletName = "Ocean View Restaurant - Downtown",
                            GuestCount = 2,
                            Status = "Completed"
                        },
                        new ReservationHistoryItem
                        {
                            ReservationId = "res-076",
                            ReservationDate = new DateTime(2025, 1, 5, 18, 30, 0),
                            OutletId = "A16",
                            OutletName = "Ocean View Restaurant - Beachside",
                            GuestCount = 3,
                            Status = "No-Show"
                        }
                    }
                },
                new CustomerDto
                {
                    CustomerId = "cust-456",
                    Name = "Emily Johnson",
                    PhoneNumber = "+1234567891",
                    Email = "emily.j@example.com",
                    IsBanned = false,
                    TotalReservations = 8,
                    NoShows = 0,
                    LastVisit = new DateTime(2025, 2, 6),
                    FirstVisit = new DateTime(2024, 7, 12),
                    Notes = "Allergic to shellfish",
                    ReservationHistory = new List<ReservationHistoryItem>
                    {
                        new ReservationHistoryItem
                        {
                            ReservationId = "res-099",
                            ReservationDate = new DateTime(2025, 2, 6, 19, 30, 0),
                            OutletId = "A16",
                            OutletName = "Ocean View Restaurant - Beachside",
                            GuestCount = 2,
                            Status = "Completed",
                            Notes = "Birthday celebration"
                        }
                    }
                },
                new CustomerDto
                {
                    CustomerId = "cust-789",
                    Name = "Michael Brown",
                    PhoneNumber = "+1234567892",
                    Email = "mbrown@example.com",
                    IsBanned = true,
                    BanReason = "Repeated No-Shows",
                    BannedDate = new DateTime(2025, 1, 14),
                    BannedBy = "Admin",
                    TotalReservations = 3,
                    NoShows = 2,
                    LastVisit = new DateTime(2025, 1, 16),
                    FirstVisit = new DateTime(2024, 12, 20),
                    Notes = "[1/14/2025 10:30:45 AM] [Ban Note] Customer has missed 2 out of 3 reservations without canceling.",
                    ReservationHistory = new List<ReservationHistoryItem>
                    {
                        new ReservationHistoryItem
                        {
                            ReservationId = "res-087",
                            ReservationDate = new DateTime(2025, 1, 16, 20, 0, 0),
                            OutletId = "A15",
                            OutletName = "Ocean View Restaurant - Downtown",
                            GuestCount = 5,
                            Status = "Completed"
                        },
                        new ReservationHistoryItem
                        {
                            ReservationId = "res-074",
                            ReservationDate = new DateTime(2025, 1, 4, 19, 0, 0),
                            OutletId = "A17",
                            OutletName = "Ocean View Restaurant - Harborfront",
                            GuestCount = 4,
                            Status = "No-Show"
                        },
                        new ReservationHistoryItem
                        {
                            ReservationId = "res-065",
                            ReservationDate = new DateTime(2024, 12, 20, 18, 0, 0),
                            OutletId = "A15",
                            OutletName = "Ocean View Restaurant - Downtown",
                            GuestCount = 2,
                            Status = "No-Show"
                        }
                    }
                },
                new CustomerDto
                {
                    CustomerId = "cust-101",
                    Name = "Sarah Williams",
                    PhoneNumber = "+1234567893",
                    Email = "sarah.w@example.com",
                    IsBanned = false,
                    TotalReservations = 6,
                    NoShows = 0,
                    LastVisit = new DateTime(2025, 2, 13),
                    FirstVisit = new DateTime(2024, 11, 2),
                    Notes = "VIP customer - Always request for quiet area",
                    ReservationHistory = new List<ReservationHistoryItem>
                    {
                        new ReservationHistoryItem
                        {
                            ReservationId = "res-105",
                            ReservationDate = new DateTime(2025, 2, 13, 19, 0, 0),
                            OutletId = "A16",
                            OutletName = "Ocean View Restaurant - Beachside",
                            GuestCount = 6,
                            Status = "Completed",
                            Notes = "Business dinner"
                        }
                    }
                }
            };

            return customers;
        }
    }
}