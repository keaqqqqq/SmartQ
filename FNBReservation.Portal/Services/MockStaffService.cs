using FNBReservation.Portal.Models;
using System.Text.Json;

namespace FNBReservation.Portal.Services
{
    public interface IStaffService
    {
        Task<List<StaffDto>> GetStaffAsync(string outletId, string? searchTerm = null);
        Task<StaffDto?> GetStaffByIdAsync(string outletId, string staffId);
        Task<bool> CreateStaffAsync(string outletId, StaffDto staff);
        Task<bool> UpdateStaffAsync(string outletId, StaffDto staff);
        Task<bool> DeleteStaffAsync(string outletId, string staffId);
    }

    public class MockStaffService : IStaffService
    {
        private Dictionary<string, List<StaffDto>> _staffByOutlet = new();

        public MockStaffService()
        {
            // Initialize with sample data
            var outlet1Staff = new List<StaffDto>
            {
                new StaffDto
                {
                    StaffId = "175",
                    OutletId = "A15",
                    FullName = "John Manager",
                    Username = "john.manager",
                    Email = "john@oceanview.com",
                    Phone = "+1234567890",
                    Role = "Manager",
                    CreatedAt = DateTime.Parse("2025-01-15")
                },
                new StaffDto
                {
                    StaffId = "291",
                    OutletId = "A15",
                    FullName = "Sarah Host",
                    Username = "sarah.host",
                    Email = "sarah@oceanview.com",
                    Phone = "+1234567891",
                    Role = "Host",
                    CreatedAt = DateTime.Parse("2025-01-20")
                },
                new StaffDto
                {
                    StaffId = "305",
                    OutletId = "A15",
                    FullName = "Mike Server",
                    Username = "mike.server",
                    Email = "mike@oceanview.com",
                    Phone = "+1234567892",
                    Role = "Server",
                    CreatedAt = DateTime.Parse("2025-02-05")
                }
            };

            var outlet2Staff = new List<StaffDto>
            {
                new StaffDto
                {
                    StaffId = "176",
                    OutletId = "A16",
                    FullName = "Emily Manager",
                    Username = "emily.manager",
                    Email = "emily@oceanview.com",
                    Phone = "+1234567893",
                    Role = "Manager",
                    CreatedAt = DateTime.Parse("2025-01-10")
                },
                new StaffDto
                {
                    StaffId = "292",
                    OutletId = "A16",
                    FullName = "David Host",
                    Username = "david.host",
                    Email = "david@oceanview.com",
                    Phone = "+1234567894",
                    Role = "Host",
                    CreatedAt = DateTime.Parse("2025-01-15")
                }
            };

            _staffByOutlet.Add("A15", outlet1Staff);
            _staffByOutlet.Add("A16", outlet2Staff);
        }

        public Task<List<StaffDto>> GetStaffAsync(string outletId, string? searchTerm = null)
        {
            if (!_staffByOutlet.ContainsKey(outletId))
            {
                return Task.FromResult(new List<StaffDto>());
            }

            var staff = _staffByOutlet[outletId];

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                staff = staff.Where(s =>
                    s.FullName.ToLower().Contains(searchTerm) ||
                    s.Username.ToLower().Contains(searchTerm) ||
                    s.Email.ToLower().Contains(searchTerm) ||
                    s.Phone.Contains(searchTerm) ||
                    s.Role.ToLower().Contains(searchTerm)
                ).ToList();
            }

            return Task.FromResult(staff);
        }

        public Task<StaffDto?> GetStaffByIdAsync(string outletId, string staffId)
        {
            if (!_staffByOutlet.ContainsKey(outletId))
            {
                return Task.FromResult<StaffDto?>(null);
            }

            var staff = _staffByOutlet[outletId].FirstOrDefault(s => s.StaffId == staffId);
            return Task.FromResult(staff);
        }

        public Task<bool> CreateStaffAsync(string outletId, StaffDto staff)
        {
            if (!_staffByOutlet.ContainsKey(outletId))
            {
                _staffByOutlet[outletId] = new List<StaffDto>();
            }

            // Generate a new ID
            staff.StaffId = (300 + _staffByOutlet[outletId].Count).ToString();
            staff.OutletId = outletId;
            staff.CreatedAt = DateTime.Now;

            _staffByOutlet[outletId].Add(staff);
            return Task.FromResult(true);
        }

        public Task<bool> UpdateStaffAsync(string outletId, StaffDto staff)
        {
            if (!_staffByOutlet.ContainsKey(outletId))
            {
                return Task.FromResult(false);
            }

            var existingStaff = _staffByOutlet[outletId].FirstOrDefault(s => s.StaffId == staff.StaffId);
            if (existingStaff == null)
            {
                return Task.FromResult(false);
            }

            // Update properties
            existingStaff.FullName = staff.FullName;
            existingStaff.Email = staff.Email;
            existingStaff.Phone = staff.Phone;
            existingStaff.Role = staff.Role;

            return Task.FromResult(true);
        }

        public Task<bool> DeleteStaffAsync(string outletId, string staffId)
        {
            if (!_staffByOutlet.ContainsKey(outletId))
            {
                return Task.FromResult(false);
            }

            var existingStaff = _staffByOutlet[outletId].FirstOrDefault(s => s.StaffId == staffId);
            if (existingStaff == null)
            {
                return Task.FromResult(false);
            }

            _staffByOutlet[outletId].Remove(existingStaff);
            return Task.FromResult(true);
        }
    }
}