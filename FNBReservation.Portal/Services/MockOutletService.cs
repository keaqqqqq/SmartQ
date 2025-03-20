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
                    Status = "Active",
                    QueueEnabled = true,
                    Tables = GenerateTables(25, "Main Area"),
                    Contact = new ContactInfo
                    {
                        Phone = "+123456789",
                        Email = "downtown@oceanview.com",
                        Website = "www.oceanview.com/downtown"
                    }
                },
                new OutletDto
                {
                    OutletId = "A16",
                    Name = "Ocean View Restaurant - Beachside",
                    Location = "456 Beach Drive, Oceanfront",
                    Status = "Active",
                    QueueEnabled = true,
                    Tables = GenerateTables(20, "Beach View"),
                    Contact = new ContactInfo
                    {
                        Phone = "+123456790",
                        Email = "beachside@oceanview.com",
                        Website = "www.oceanview.com/beachside"
                    }
                },
                new OutletDto
                {
                    OutletId = "A17",
                    Name = "Ocean View Restaurant - Harborfront",
                    Location = "789 Harbor Road, Marina District",
                    Status = "Active",
                    QueueEnabled = false,
                    Tables = GenerateTables(25, "Harbor View"),
                    Contact = new ContactInfo
                    {
                        Phone = "+123456791",
                        Email = "harbor@oceanview.com",
                        Website = "www.oceanview.com/harbor"
                    }
                }
            };
        }

        private List<FNBReservation.Portal.Models.TableInfo> GenerateTables(int count, string location)
        {
            var tables = new List<FNBReservation.Portal.Models.TableInfo>();
            for (int i = 1; i <= count; i++)
            {
                tables.Add(new FNBReservation.Portal.Models.TableInfo
                {
                    TableId = Guid.NewGuid().ToString(),
                    Name = $"Table {i}",
                    Capacity = i % 3 == 0 ? 6 : 4, // Mix of 4 and 6 seat tables
                    Status = "Available",
                    Location = location
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