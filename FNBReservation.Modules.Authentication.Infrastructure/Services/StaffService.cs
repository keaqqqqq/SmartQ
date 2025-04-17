using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Authentication.Core.DTOs;
using FNBReservation.Modules.Authentication.Core.Entities;
using FNBReservation.Modules.Authentication.Core.Interfaces;

namespace FNBReservation.Modules.Authentication.Infrastructure.Services
{
    public class StaffService : IStaffService
    {
        private readonly IStaffRepository _staffRepository;
        private readonly IOutletAdapter _outletAdapter;
        private readonly ILogger<StaffService> _logger;

        public StaffService(
            IStaffRepository staffRepository,
            IOutletAdapter outletAdapter,
            ILogger<StaffService> logger)
        {
            _staffRepository = staffRepository ?? throw new ArgumentNullException(nameof(staffRepository));
            _outletAdapter = outletAdapter ?? throw new ArgumentNullException(nameof(outletAdapter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<StaffDto> CreateStaffAsync(CreateStaffDto createStaffDto, Guid adminId)
        {
            _logger.LogInformation("Creating new staff: {Username} for outlet: {OutletId}",
                createStaffDto.Username, createStaffDto.OutletId);

            // Validate outlet exists
            var outletExists = await _outletAdapter.OutletExistsAsync(createStaffDto.OutletId);
            if (!outletExists)
            {
                _logger.LogWarning("Outlet not found: {OutletId}", createStaffDto.OutletId);
                throw new ArgumentException($"Outlet with ID {createStaffDto.OutletId} not found");
            }

            // Check if username already exists
            var existingUsername = await _staffRepository.GetUserByUsernameAsync(createStaffDto.Username);
            if (existingUsername != null)
            {
                _logger.LogWarning("Username already exists: {Username}", createStaffDto.Username);
                throw new ArgumentException($"Username '{createStaffDto.Username}' is already taken");
            }

            // Check if email already exists
            var existingEmail = await _staffRepository.GetUserByEmailAsync(createStaffDto.Email);
            if (existingEmail != null)
            {
                _logger.LogWarning("Email already exists: {Email}", createStaffDto.Email);
                throw new ArgumentException($"Email '{createStaffDto.Email}' is already registered");
            }

            // Create password hash
            var passwordHasher = new PasswordHasher<User>();
            var passwordHash = passwordHasher.HashPassword(null, createStaffDto.Password);

            // Generate a unique UserId for staff
            var staffUserId = $"STAFF{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

            var staff = new User
            {
                Id = Guid.NewGuid(),
                UserId = staffUserId,
                OutletId = createStaffDto.OutletId,
                Email = createStaffDto.Email,
                FullName = createStaffDto.FullName,
                Username = createStaffDto.Username,
                PasswordHash = passwordHash,
                Phone = createStaffDto.Phone,
                Role = createStaffDto.Role,
                UserType = "Staff",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            var createdStaff = await _staffRepository.CreateStaffAsync(staff);
            return await MapToStaffDtoWithOutletName(createdStaff);
        }

        public async Task<StaffDto> GetStaffByIdAsync(Guid id)
        {
            _logger.LogInformation("Getting staff by ID: {Id}", id);

            var staff = await _staffRepository.GetStaffByIdAsync(id);
            if (staff == null)
            {
                return null;
            }

            return await MapToStaffDtoWithOutletName(staff);
        }

        public async Task<IEnumerable<StaffDto>> GetStaffByOutletIdAsync(Guid outletId)
        {
            _logger.LogInformation("Getting staff by outlet ID: {OutletId}", outletId);

            // Validate outlet exists
            var outletExists = await _outletAdapter.OutletExistsAsync(outletId);
            if (!outletExists)
            {
                _logger.LogWarning("Outlet not found: {OutletId}", outletId);
                throw new ArgumentException($"Outlet with ID {outletId} not found");
            }

            var staffList = await _staffRepository.GetStaffByOutletIdAsync(outletId);
            var staffDtoList = new List<StaffDto>();

            foreach (var staff in staffList)
            {
                staffDtoList.Add(await MapToStaffDtoWithOutletName(staff));
            }

            return staffDtoList;
        }

        public async Task<IEnumerable<StaffDto>> GetAllStaffAsync()
        {
            _logger.LogInformation("Getting all staff");

            var staffList = await _staffRepository.GetAllStaffAsync();
            var staffDtoList = new List<StaffDto>();

            foreach (var staff in staffList)
            {
                staffDtoList.Add(await MapToStaffDtoWithOutletName(staff));
            }

            return staffDtoList;
        }

        public async Task<StaffDto> UpdateStaffAsync(Guid id, UpdateStaffDto updateStaffDto, Guid adminId)
        {
            _logger.LogInformation("Updating staff: {Id}", id);

            var existingStaff = await _staffRepository.GetStaffByIdAsync(id);
            if (existingStaff == null)
            {
                _logger.LogWarning("Staff not found for update: {Id}", id);
                return null;
            }

            if (!string.IsNullOrEmpty(updateStaffDto.FullName))
            {
                existingStaff.FullName = updateStaffDto.FullName;
            }

            // Check for username uniqueness if updating username
            if (!string.IsNullOrEmpty(updateStaffDto.Username) && updateStaffDto.Username != existingStaff.Username)
            {
                var existingUsername = await _staffRepository.GetUserByUsernameAsync(updateStaffDto.Username);
                if (existingUsername != null)
                {
                    _logger.LogWarning("Username already exists: {Username}", updateStaffDto.Username);
                    throw new ArgumentException($"Username '{updateStaffDto.Username}' is already taken");
                }
                existingStaff.Username = updateStaffDto.Username;
            }

            // Check for email uniqueness if updating email
            if (!string.IsNullOrEmpty(updateStaffDto.Email) && updateStaffDto.Email != existingStaff.Email)
            {
                var existingEmail = await _staffRepository.GetUserByEmailAsync(updateStaffDto.Email);
                if (existingEmail != null)
                {
                    _logger.LogWarning("Email already exists: {Email}", updateStaffDto.Email);
                    throw new ArgumentException($"Email '{updateStaffDto.Email}' is already registered");
                }
                existingStaff.Email = updateStaffDto.Email;
            }

            // Update password if provided
            if (!string.IsNullOrEmpty(updateStaffDto.Password))
            {
                var passwordHasher = new PasswordHasher<User>();
                existingStaff.PasswordHash = passwordHasher.HashPassword(null, updateStaffDto.Password);
            }

            // Update other fields if provided
            if (!string.IsNullOrEmpty(updateStaffDto.Phone))
            {
                existingStaff.Phone = updateStaffDto.Phone;
            }

            if (!string.IsNullOrEmpty(updateStaffDto.Role))
            {
                existingStaff.Role = updateStaffDto.Role;
            }

            if (updateStaffDto.IsActive.HasValue)
            {
                existingStaff.IsActive = updateStaffDto.IsActive.Value;
            }

            existingStaff.UpdatedAt = DateTime.UtcNow;

            var updatedStaff = await _staffRepository.UpdateStaffAsync(existingStaff);
            return await MapToStaffDtoWithOutletName(updatedStaff);
        }

        public async Task<bool> DeleteStaffAsync(Guid id)
        {
            _logger.LogInformation("Deleting staff: {Id}", id);
            return await _staffRepository.DeleteStaffAsync(id);
        }

        private async Task<StaffDto> MapToStaffDtoWithOutletName(User staff)
        {
            var outletName = "Unknown";
            if (staff.OutletId.HasValue)
            {
                try
                {
                    var outletInfo = await _outletAdapter.GetOutletBasicInfoAsync(staff.OutletId.Value);
                    if (outletInfo != null)
                    {
                        outletName = outletInfo.Name;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get outlet name for staff: {StaffId}, outlet: {OutletId}",
                        staff.Id, staff.OutletId);
                }
            }

            return new StaffDto
            {
                Id = staff.Id,
                UserId = staff.UserId,
                OutletId = staff.OutletId ?? Guid.Empty,
                OutletName = outletName,
                Email = staff.Email,
                FullName = staff.FullName ?? "",
                Username = staff.Username,
                Phone = staff.Phone ?? "",
                Role = staff.Role,
                IsActive = staff.IsActive,
                CreatedAt = staff.CreatedAt,
                UpdatedAt = staff.UpdatedAt
            };
        }
    }
}