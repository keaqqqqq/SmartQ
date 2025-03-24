using FNBReservation.Portal.Models;
using System.Text.Json;

namespace FNBReservation.Portal.Services
{
    public interface IOutletService
    {
        Task<List<OutletDto>> GetOutletsAsync(string? searchTerm = null);
        Task<OutletDto?> GetOutletByIdAsync(string outletId);
        Task<List<OutletChangeDto>> GetOutletChangesAsync(string outletId);
        Task<bool> CreateOutletAsync(OutletDto outlet);
        Task<bool> UpdateOutletAsync(OutletDto outlet);
        Task<bool> DeleteOutletAsync(string outletId);
    }

    public class MockOutletService : IOutletService
    {
        private List<OutletDto> _outlets;
        private List<OutletChangeDto> _changes;

        public MockOutletService()
        {
            _outlets = GenerateMockOutlets();
            _changes = GenerateMockChanges();
        }

        public Task<List<OutletDto>> GetOutletsAsync(string? searchTerm = null)
        {
            var outlets = _outlets;

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                outlets = outlets
                    .Where(o =>
                        o.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        o.Location.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return Task.FromResult(outlets);
        }

        public Task<OutletDto?> GetOutletByIdAsync(string outletId)
        {
            var outlet = _outlets.FirstOrDefault(o => o.OutletId == outletId);
            return Task.FromResult(outlet);
        }

        public Task<List<OutletChangeDto>> GetOutletChangesAsync(string outletId)
        {
            var changes = _changes.Where(c => c.OutletId == outletId).ToList();
            return Task.FromResult(changes);
        }

        public Task<bool> CreateOutletAsync(OutletDto outlet)
        {
            // Ensure outlet has an ID
            if (string.IsNullOrEmpty(outlet.OutletId))
            {
                outlet.OutletId = Guid.NewGuid().ToString();
            }

            _outlets.Add(outlet);

            // Add a change record
            _changes.Add(new OutletChangeDto
            {
                ChangeId = Guid.NewGuid().ToString(),
                OutletId = outlet.OutletId,
                ChangeType = "Create",
                Description = $"Created outlet: {outlet.Name}",
                ChangedBy = "Test User",
                ChangeDate = DateTime.Now
            });

            return Task.FromResult(true);
        }

        public Task<bool> UpdateOutletAsync(OutletDto outlet)
        {
            var existingOutlet = _outlets.FirstOrDefault(o => o.OutletId == outlet.OutletId);

            if (existingOutlet == null)
            {
                return Task.FromResult(false);
            }

            // Remove old outlet and add updated one
            _outlets.Remove(existingOutlet);
            _outlets.Add(outlet);

            // Add a change record
            _changes.Add(new OutletChangeDto
            {
                ChangeId = Guid.NewGuid().ToString(),
                OutletId = outlet.OutletId,
                ChangeType = "Update",
                Description = $"Updated outlet: {outlet.Name}",
                ChangedBy = "Test User",
                ChangeDate = DateTime.Now
            });

            return Task.FromResult(true);
        }

        public Task<bool> DeleteOutletAsync(string outletId)
        {
            var outlet = _outlets.FirstOrDefault(o => o.OutletId == outletId);

            if (outlet == null)
            {
                return Task.FromResult(false);
            }

            _outlets.Remove(outlet);

            // Add a change record
            _changes.Add(new OutletChangeDto
            {
                ChangeId = Guid.NewGuid().ToString(),
                OutletId = outletId,
                ChangeType = "Delete",
                Description = $"Deleted outlet: {outlet.Name}",
                ChangedBy = "Test User",
                ChangeDate = DateTime.Now
            });

            return Task.FromResult(true);
        }

        private List<OutletDto> GenerateMockOutlets()
        {
            return new List<OutletDto>
            {
                new OutletDto
                {
                    OutletId = "A15",
                    Name = "Ocean View Restaurant - Downtown",
                    Location = "123 Main Street, Downtown",
                    OperatingHours = "10:00 AM - 10:00 PM",
                    Status = "Active",
                    QueueEnabled = true,
                    ReservationAllocationPercent = 50,
                    DefaultDiningDurationMinutes = 90,
                    Contact = "+123456789",
                    Tables = GenerateTables(25, "Main Area"),
                    PeakHours = new List<PeakHour>
                    {
                        new PeakHour
                        {
                            Name = "Lunch Rush",
                            DaysOfWeek = "1,2,3,4,5",
                            StartTime = "12:00:00",
                            EndTime = "14:00:00",
                            ReservationAllocationPercent = 80,
                            IsActive = true
                        },
                        new PeakHour
                        {
                            Name = "Dinner Rush",
                            DaysOfWeek = "1,2,3,4,5,6,7",
                            StartTime = "18:00:00",
                            EndTime = "20:00:00",
                            ReservationAllocationPercent = 90,
                            IsActive = true
                        }
                    }
                },
                new OutletDto
                {
                    OutletId = "A16",
                    Name = "Ocean View Restaurant - Beachside",
                    Location = "456 Beach Drive, Oceanfront",
                    OperatingHours = "08:00 AM - 11:00 PM",
                    Status = "Active",
                    QueueEnabled = true,
                    ReservationAllocationPercent = 60,
                    DefaultDiningDurationMinutes = 120,
                    Contact = "+123456790",
                    Tables = GenerateTables(20, "Beach View"),
                    PeakHours = new List<PeakHour>
                    {
                        new PeakHour
                        {
                            Name = "Weekend Brunch",
                            DaysOfWeek = "6,7",
                            StartTime = "09:00:00",
                            EndTime = "13:00:00",
                            ReservationAllocationPercent = 100,
                            IsActive = true
                        }
                    }
                },
                new OutletDto
                {
                    OutletId = "A17",
                    Name = "Ocean View Restaurant - Harborfront",
                    Location = "789 Harbor Road, Marina District",
                    OperatingHours = "09:00 AM - 09:00 PM",
                    Status = "Active",
                    QueueEnabled = false,
                    ReservationAllocationPercent = 40,
                    DefaultDiningDurationMinutes = 90,
                    Contact = "+123456791",
                    Tables = GenerateTables(15, "Harbor View"),
                    PeakHours = new List<PeakHour>()
                }
            };
        }

        private List<TableInfo> GenerateTables(int count, string section)
        {
            var tables = new List<TableInfo>();
            for (int i = 1; i <= count; i++)
            {
                tables.Add(new TableInfo
                {
                    TableNumber = $"T{i}",
                    Capacity = i % 3 == 0 ? 6 : 4, // Mix of 4 and 6 seat tables
                    Section = section,
                    IsActive = true
                });
            }
            return tables;
        }

        private List<OutletChangeDto> GenerateMockChanges()
        {
            return new List<OutletChangeDto>
            {
                new OutletChangeDto
                {
                    ChangeId = Guid.NewGuid().ToString(),
                    OutletId = "A15",
                    ChangeType = "Create",
                    Description = "Created outlet: Ocean View Restaurant - Downtown",
                    ChangedBy = "Admin User",
                    ChangeDate = DateTime.Now.AddDays(-30)
                },
                new OutletChangeDto
                {
                    ChangeId = Guid.NewGuid().ToString(),
                    OutletId = "A15",
                    ChangeType = "Update",
                    Description = "Updated operating hours",
                    ChangedBy = "Admin User",
                    ChangeDate = DateTime.Now.AddDays(-15)
                },
                new OutletChangeDto
                {
                    ChangeId = Guid.NewGuid().ToString(),
                    OutletId = "A16",
                    ChangeType = "Create",
                    Description = "Created outlet: Ocean View Restaurant - Beachside",
                    ChangedBy = "Admin User",
                    ChangeDate = DateTime.Now.AddDays(-25)
                },
                new OutletChangeDto
                {
                    ChangeId = Guid.NewGuid().ToString(),
                    OutletId = "A17",
                    ChangeType = "Create",
                    Description = "Created outlet: Ocean View Restaurant - Harborfront",
                    ChangedBy = "Admin User",
                    ChangeDate = DateTime.Now.AddDays(-20)
                },
                new OutletChangeDto
                {
                    ChangeId = Guid.NewGuid().ToString(),
                    OutletId = "A17",
                    ChangeType = "Status_Change",
                    Description = "Changed status from Inactive to Active",
                    ChangedBy = "Admin User",
                    ChangeDate = DateTime.Now.AddDays(-5)
                }
            };
        }
    }
}