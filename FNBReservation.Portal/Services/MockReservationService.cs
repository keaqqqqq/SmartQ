using FNBReservation.Portal.Models;

namespace FNBReservation.Portal.Services
{
    public interface IReservationService
    {
        Task<List<ReservationDto>> GetReservationsAsync(ReservationFilterDto filter);
        Task<ReservationDto?> GetReservationByIdAsync(string reservationId);
        Task<AvailabilityResponseDto> CheckAvailabilityAsync(AvailabilityRequestDto request);
        Task<ReservationDto?> CreateReservationAsync(CreateReservationDto request);
        Task<ReservationDto?> UpdateReservationAsync(UpdateReservationDto request);
        Task<bool> CancelReservationAsync(string reservationId, string reason);
        Task<bool> CheckInReservationAsync(string reservationId);
        Task<bool> CheckOutReservationAsync(string reservationId);
        Task<List<ReservationDto>> GetReservationsByOutletAndDateAsync(string outletId, DateTime date);
    }

    public class MockReservationService : IReservationService
    {
        private readonly IOutletService _outletService;
        private List<ReservationDto> _reservations = new List<ReservationDto>();
        private readonly Dictionary<string, string> _statuses = new Dictionary<string, string>
        {
            { "Confirmed", "Confirmed" },
            { "Seated", "Seated" },
            { "Completed", "Completed" },
            { "Cancelled", "Cancelled" },
            { "No-Show", "No-Show" }
        };

        public MockReservationService(IOutletService outletService)
        {
            _outletService = outletService;
            _reservations = GenerateSampleReservations();
        }

        public async Task<List<ReservationDto>> GetReservationsAsync(ReservationFilterDto filter)
        {
            var filteredReservations = _reservations.AsEnumerable();

            if (!string.IsNullOrEmpty(filter.OutletId))
            {
                filteredReservations = filteredReservations.Where(r => r.OutletId == filter.OutletId);
            }

            if (filter.StartDate.HasValue)
            {
                var startDate = filter.StartDate.Value.Date;
                filteredReservations = filteredReservations.Where(r => r.ReservationDate.Date >= startDate);
            }

            if (filter.EndDate.HasValue)
            {
                var endDate = filter.EndDate.Value.Date.AddDays(1).AddSeconds(-1);
                filteredReservations = filteredReservations.Where(r => r.ReservationDate.Date <= endDate.Date);
            }

            if (!string.IsNullOrEmpty(filter.Status))
            {
                filteredReservations = filteredReservations.Where(r => r.Status == filter.Status);
            }

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.ToLower();
                filteredReservations = filteredReservations.Where(r =>
                    r.CustomerName.ToLower().Contains(searchTerm) ||
                    r.CustomerPhone.ToLower().Contains(searchTerm) ||
                    (r.CustomerEmail != null && r.CustomerEmail.ToLower().Contains(searchTerm)) ||
                    r.ReservationId.ToLower().Contains(searchTerm) ||
                    (r.Notes != null && r.Notes.ToLower().Contains(searchTerm))
                );
            }

            return filteredReservations.OrderBy(r => r.ReservationDate).ToList();
        }

        public async Task<ReservationDto?> GetReservationByIdAsync(string reservationId)
        {
            return _reservations.FirstOrDefault(r => r.ReservationId == reservationId);
        }

        public async Task<AvailabilityResponseDto> CheckAvailabilityAsync(AvailabilityRequestDto request)
        {
            // This is a mock implementation - in a real application, you would check 
            // actual availability based on existing reservations, table configurations, etc.
            var outlet = await _outletService.GetOutletByIdAsync(request.OutletId);

            if (outlet == null)
            {
                return new AvailabilityResponseDto
                {
                    Available = false,
                    Message = "Outlet not found"
                };
            }

            // Generate some mock time slots
            var availableTimes = new List<AvailableTimeSlot>();

            // Parse the preferred time
            if (TimeSpan.TryParse(request.PreferredTime, out var preferredTime))
            {
                // Check if the date is in the past
                if (request.Date.Date < DateTime.Now.Date)
                {
                    return new AvailabilityResponseDto
                    {
                        Available = false,
                        Message = "Cannot make reservations for past dates",
                        NextAvailableDate = DateTime.Now.Date
                    };
                }

                // Add the preferred time and some alternatives
                availableTimes.Add(new AvailableTimeSlot
                {
                    Time = request.PreferredTime,
                    AvailableTables = 3
                });

                // Add 30 minutes before
                var beforeTime = preferredTime.Add(TimeSpan.FromMinutes(-30));
                availableTimes.Add(new AvailableTimeSlot
                {
                    Time = $"{beforeTime.Hours:D2}:{beforeTime.Minutes:D2}:00",
                    AvailableTables = 2
                });

                // Add 30 minutes after
                var afterTime = preferredTime.Add(TimeSpan.FromMinutes(30));
                availableTimes.Add(new AvailableTimeSlot
                {
                    Time = $"{afterTime.Hours:D2}:{afterTime.Minutes:D2}:00",
                    AvailableTables = 4
                });
            }

            return new AvailabilityResponseDto
            {
                Available = availableTimes.Count > 0,
                AvailableTimes = availableTimes,
                NextAvailableDate = availableTimes.Count == 0 ? request.Date.AddDays(1) : null
            };
        }

        public async Task<ReservationDto?> CreateReservationAsync(CreateReservationDto request)
        {
            var outlet = await _outletService.GetOutletByIdAsync(request.OutletId);

            if (outlet == null)
            {
                return null;
            }

            var availabilityRequest = new AvailabilityRequestDto
            {
                OutletId = request.OutletId,
                PartySize = request.PartySize,
                Date = request.ReservationDate.Date,
                PreferredTime = request.ReservationDate.ToString("HH:mm:ss"),
                EarliestTime = request.ReservationDate.AddMinutes(-30).ToString("HH:mm:ss"),
                LatestTime = request.ReservationDate.AddMinutes(30).ToString("HH:mm:ss")
            };

            var availability = await CheckAvailabilityAsync(availabilityRequest);

            if (!availability.Available)
            {
                return null;
            }

            // Calculate end time (using outlet's default dining duration)
            var endTime = request.ReservationDate.AddMinutes(outlet.DefaultDiningDurationMinutes);

            // Simulate table assignment
            var tables = new List<string>();
            var requiredCapacity = request.PartySize;
            var availableTables = outlet.Tables.Where(t => t.IsActive).OrderBy(t => t.Capacity).ToList();

            foreach (var table in availableTables)
            {
                if (requiredCapacity <= 0) break;
                tables.Add(table.TableNumber);
                requiredCapacity -= table.Capacity;
            }

            var newReservation = new ReservationDto
            {
                ReservationId = $"res-{_reservations.Count + 1:D3}",
                OutletId = outlet.OutletId,
                OutletName = outlet.Name,
                CustomerName = request.CustomerName,
                CustomerPhone = request.CustomerPhone,
                CustomerEmail = request.CustomerEmail,
                PartySize = request.PartySize,
                ReservationDate = request.ReservationDate,
                EndTime = endTime,
                TableAssignments = tables,
                Status = "Confirmed",
                Source = request.Source,
                SpecialRequests = request.SpecialRequests,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _reservations.Add(newReservation);
            return newReservation;
        }

        public async Task<ReservationDto?> UpdateReservationAsync(UpdateReservationDto request)
        {
            var existingReservation = _reservations.FirstOrDefault(r => r.ReservationId == request.ReservationId);

            if (existingReservation == null)
            {
                return null;
            }

            // Update properties if they are provided
            if (!string.IsNullOrEmpty(request.CustomerName))
            {
                existingReservation.CustomerName = request.CustomerName;
            }

            if (!string.IsNullOrEmpty(request.CustomerPhone))
            {
                existingReservation.CustomerPhone = request.CustomerPhone;
            }

            if (request.CustomerEmail != null)
            {
                existingReservation.CustomerEmail = request.CustomerEmail;
            }

            if (request.PartySize.HasValue)
            {
                existingReservation.PartySize = request.PartySize.Value;
            }

            if (request.ReservationDate.HasValue)
            {
                // Get the outlet to calculate new end time
                var outlet = await _outletService.GetOutletByIdAsync(existingReservation.OutletId);
                if (outlet != null)
                {
                    existingReservation.ReservationDate = request.ReservationDate.Value;
                    existingReservation.EndTime = request.ReservationDate.Value.AddMinutes(outlet.DefaultDiningDurationMinutes);
                }
            }

            if (!string.IsNullOrEmpty(request.Status) && _statuses.ContainsKey(request.Status))
            {
                existingReservation.Status = request.Status;
            }

            if (request.TableAssignments != null)
            {
                existingReservation.TableAssignments = request.TableAssignments;
            }

            if (request.SpecialRequests != null)
            {
                existingReservation.SpecialRequests = request.SpecialRequests;
            }

            if (request.Notes != null)
            {
                existingReservation.Notes = request.Notes;
            }

            existingReservation.UpdatedAt = DateTime.Now;
            return existingReservation;
        }

        public async Task<bool> CancelReservationAsync(string reservationId, string reason)
        {
            var reservation = _reservations.FirstOrDefault(r => r.ReservationId == reservationId);

            if (reservation == null)
            {
                return false;
            }

            reservation.Status = "Cancelled";
            reservation.Notes = string.IsNullOrEmpty(reservation.Notes)
                ? $"Cancelled: {reason}"
                : $"{reservation.Notes}\nCancelled: {reason}";
            reservation.UpdatedAt = DateTime.Now;

            return true;
        }

        public async Task<bool> CheckInReservationAsync(string reservationId)
        {
            var reservation = _reservations.FirstOrDefault(r => r.ReservationId == reservationId);

            if (reservation == null || reservation.Status != "Confirmed")
            {
                return false;
            }

            reservation.Status = "Seated";
            reservation.CheckInTime = DateTime.Now;
            reservation.UpdatedAt = DateTime.Now;

            return true;
        }

        public async Task<bool> CheckOutReservationAsync(string reservationId)
        {
            var reservation = _reservations.FirstOrDefault(r => r.ReservationId == reservationId);

            if (reservation == null || reservation.Status != "Seated")
            {
                return false;
            }

            reservation.Status = "Completed";
            reservation.CheckOutTime = DateTime.Now;
            reservation.UpdatedAt = DateTime.Now;

            return true;
        }

        public async Task<List<ReservationDto>> GetReservationsByOutletAndDateAsync(string outletId, DateTime date)
        {
            return _reservations
                .Where(r => r.OutletId == outletId && r.ReservationDate.Date == date.Date)
                .OrderBy(r => r.ReservationDate)
                .ToList();
        }

        private List<ReservationDto> GenerateSampleReservations()
        {
            var today = DateTime.Now.Date;
            var tomorrow = today.AddDays(1);
            var dayAfterTomorrow = today.AddDays(2);

            return new List<ReservationDto>
            {
                // Today's reservations
                new ReservationDto
                {
                    ReservationId = "res-001",
                    OutletId = "A15",
                    OutletName = "Ocean View Restaurant - Downtown",
                    CustomerName = "John Smith",
                    CustomerPhone = "+1234567890",
                    CustomerEmail = "john.smith@example.com",
                    PartySize = 4,
                    ReservationDate = today.AddHours(18),
                    EndTime = today.AddHours(19).AddMinutes(30),
                    TableAssignments = new List<string> { "T1", "T2" },
                    Status = "Confirmed",
                    Source = "Website",
                    SpecialRequests = "Window seating preferred",
                    CreatedAt = today.AddDays(-3),
                    UpdatedAt = today.AddDays(-3)
                },
                new ReservationDto
                {
                    ReservationId = "res-002",
                    OutletId = "A15",
                    OutletName = "Ocean View Restaurant - Downtown",
                    CustomerName = "Emily Johnson",
                    CustomerPhone = "+1234567891",
                    CustomerEmail = "emily.j@example.com",
                    PartySize = 2,
                    ReservationDate = today.AddHours(19),
                    EndTime = today.AddHours(20).AddMinutes(30),
                    TableAssignments = new List<string> { "T3" },
                    Status = "Seated",
                    Source = "Phone",
                    CheckInTime = today.AddHours(19).AddMinutes(5),
                    CreatedAt = today.AddDays(-2),
                    UpdatedAt = today.AddHours(19).AddMinutes(5)
                },
                new ReservationDto
                {
                    ReservationId = "res-003",
                    OutletId = "A16",
                    OutletName = "Ocean View Restaurant - Beachside",
                    CustomerName = "Robert Brown",
                    CustomerPhone = "+1234567892",
                    CustomerEmail = "robert.b@example.com",
                    PartySize = 6,
                    ReservationDate = today.AddHours(20),
                    EndTime = today.AddHours(22),
                    TableAssignments = new List<string> { "T4", "T5" },
                    Status = "Confirmed",
                    Source = "Website",
                    SpecialRequests = "Birthday celebration - please prepare a cake",
                    CreatedAt = today.AddDays(-1),
                    UpdatedAt = today.AddDays(-1)
                },

                // Tomorrow's reservations
                new ReservationDto
                {
                    ReservationId = "res-004",
                    OutletId = "A15",
                    OutletName = "Ocean View Restaurant - Downtown",
                    CustomerName = "Sarah Williams",
                    CustomerPhone = "+1234567893",
                    CustomerEmail = "sarah.w@example.com",
                    PartySize = 3,
                    ReservationDate = tomorrow.AddHours(12).AddMinutes(30),
                    EndTime = tomorrow.AddHours(14),
                    TableAssignments = new List<string> { "T6" },
                    Status = "Confirmed",
                    Source = "Phone",
                    CreatedAt = today.AddDays(-4),
                    UpdatedAt = today.AddDays(-4)
                },
                new ReservationDto
                {
                    ReservationId = "res-005",
                    OutletId = "A16",
                    OutletName = "Ocean View Restaurant - Beachside",
                    CustomerName = "Michael Davis",
                    CustomerPhone = "+1234567894",
                    CustomerEmail = "m.davis@example.com",
                    PartySize = 4,
                    ReservationDate = tomorrow.AddHours(19),
                    EndTime = tomorrow.AddHours(20).AddMinutes(30),
                    TableAssignments = new List<string> { "T1" },
                    Status = "Confirmed",
                    Source = "Website",
                    SpecialRequests = "Gluten-free options needed",
                    CreatedAt = today.AddDays(-2),
                    UpdatedAt = today.AddDays(-2)
                },

                // Day after tomorrow
                new ReservationDto
                {
                    ReservationId = "res-006",
                    OutletId = "A17",
                    OutletName = "Ocean View Restaurant - Harborfront",
                    CustomerName = "Jessica Martinez",
                    CustomerPhone = "+1234567895",
                    CustomerEmail = "j.martinez@example.com",
                    PartySize = 8,
                    ReservationDate = dayAfterTomorrow.AddHours(18),
                    EndTime = dayAfterTomorrow.AddHours(20),
                    TableAssignments = new List<string> { "T7", "T8", "T9" },
                    Status = "Confirmed",
                    Source = "Website",
                    SpecialRequests = "Business dinner - need quiet area",
                    CreatedAt = today.AddDays(-5),
                    UpdatedAt = today.AddDays(-5)
                },

                // Past reservations
                new ReservationDto
                {
                    ReservationId = "res-007",
                    OutletId = "A15",
                    OutletName = "Ocean View Restaurant - Downtown",
                    CustomerName = "David Wilson",
                    CustomerPhone = "+1234567896",
                    CustomerEmail = "d.wilson@example.com",
                    PartySize = 2,
                    ReservationDate = today.AddDays(-1).AddHours(19),
                    EndTime = today.AddDays(-1).AddHours(20).AddMinutes(30),
                    TableAssignments = new List<string> { "T2" },
                    Status = "Completed",
                    Source = "Phone",
                    CheckInTime = today.AddDays(-1).AddHours(19).AddMinutes(5),
                    CheckOutTime = today.AddDays(-1).AddHours(20).AddMinutes(45),
                    CreatedAt = today.AddDays(-5),
                    UpdatedAt = today.AddDays(-1).AddHours(20).AddMinutes(45)
                },
                new ReservationDto
                {
                    ReservationId = "res-008",
                    OutletId = "A16",
                    OutletName = "Ocean View Restaurant - Beachside",
                    CustomerName = "Linda Taylor",
                    CustomerPhone = "+1234567897",
                    CustomerEmail = "l.taylor@example.com",
                    PartySize = 4,
                    ReservationDate = today.AddDays(-2).AddHours(18),
                    EndTime = today.AddDays(-2).AddHours(19).AddMinutes(30),
                    TableAssignments = new List<string> { "T3" },
                    Status = "No-Show",
                    Source = "Website",
                    Notes = "Customer did not arrive within 15 minutes of reservation time",
                    CreatedAt = today.AddDays(-7),
                    UpdatedAt = today.AddDays(-2).AddHours(18).AddMinutes(15)
                },
                new ReservationDto
                {
                    ReservationId = "res-009",
                    OutletId = "A17",
                    OutletName = "Ocean View Restaurant - Harborfront",
                    CustomerName = "James Anderson",
                    CustomerPhone = "+1234567898",
                    CustomerEmail = "j.anderson@example.com",
                    PartySize = 6,
                    ReservationDate = today.AddDays(-3).AddHours(19).AddMinutes(30),
                    EndTime = today.AddDays(-3).AddHours(21),
                    TableAssignments = new List<string> { "T4", "T5" },
                    Status = "Cancelled",
                    Source = "Phone",
                    Notes = "Customer called to cancel due to illness",
                    CreatedAt = today.AddDays(-10),
                    UpdatedAt = today.AddDays(-4)
                }
            };
        }
    }
}