// FNBReservation.Modules.Outlet.Infrastructure/Services/OutletService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FNBReservation.Modules.Outlet.Core.DTOs;
using FNBReservation.Modules.Outlet.Core.Entities;
using FNBReservation.Modules.Outlet.Core.Interfaces;
using FNBReservation.Modules.Outlet.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace FNBReservation.Modules.Outlet.Infrastructure.Services
{
    public class OutletService : IOutletService
    {
        private readonly IOutletRepository _outletRepository;
        private readonly ILogger<OutletService> _logger;
        private readonly ITableRepository _tableRepository; // Add this field

        public OutletService(IOutletRepository outletRepository, ITableRepository tableRepository, ILogger<OutletService> logger)
        {
            _outletRepository = outletRepository ?? throw new ArgumentNullException(nameof(outletRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tableRepository = tableRepository ?? throw new ArgumentNullException(nameof(tableRepository)); // Set field

        }

        public async Task<OutletDto> CreateOutletAsync(CreateOutletDto createOutletDto, Guid userId)
        {
            _logger.LogInformation("Creating new outlet: {Name}", createOutletDto.Name);

            // Generate a unique outletId
            var outletId = GenerateOutletId(createOutletDto.Name);

            var outlet = new OutletEntity
            {
                Id = Guid.NewGuid(),
                OutletId = outletId,
                Name = createOutletDto.Name,
                Location = createOutletDto.Location,
                OperatingHours = createOutletDto.OperatingHours,
                MaxAdvanceReservationTime = createOutletDto.MaxAdvanceReservationTime,
                MinAdvanceReservationTime = createOutletDto.MinAdvanceReservationTime,
                Contact = createOutletDto.Contact,
                QueueEnabled = createOutletDto.QueueEnabled,
                SpecialRequirements = createOutletDto.SpecialRequirements,
                Status = createOutletDto.Status,
                Latitude = createOutletDto.Latitude,
                Longitude = createOutletDto.Longitude,
                ReservationAllocationPercent = createOutletDto.ReservationAllocationPercent,
                DefaultDiningDurationMinutes = createOutletDto.DefaultDiningDurationMinutes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };

            var createdOutlet = await _outletRepository.CreateAsync(outlet);
            return await MapToOutletDtoWithCapacityAsync(createdOutlet);
        }

        public async Task<OutletDto> GetOutletByIdAsync(Guid outletId)
        {
            _logger.LogInformation("Getting outlet by ID: {Id}", outletId);

            var outlet = await _outletRepository.GetByIdAsync(outletId);
            return outlet != null ? await MapToOutletDtoWithCapacityAsync(outlet) : null;
        }

        public async Task<OutletDto> GetOutletByBusinessIdAsync(string outletId)
        {
            _logger.LogInformation("Getting outlet by business ID: {OutletId}", outletId);

            var outlet = await _outletRepository.GetByBusinessIdAsync(outletId);
            return outlet != null ? await MapToOutletDtoWithCapacityAsync(outlet) : null;
        }

        public async Task<IEnumerable<OutletDto>> GetAllOutletsAsync()
        {
            _logger.LogInformation("Getting all outlets");

            var outlets = await _outletRepository.GetAllAsync();
            var outletDtos = new List<OutletDto>();

            foreach (var outlet in outlets)
            {
                outletDtos.Add(await MapToOutletDtoWithCapacityAsync(outlet));
            }

            return outletDtos;
        }

        public async Task<OutletDto> UpdateOutletAsync(Guid outletId, UpdateOutletDto updateOutletDto, Guid userId)
        {
            _logger.LogInformation("Updating outlet: {Id}", outletId);

            var existingOutlet = await _outletRepository.GetByIdAsync(outletId);
            if (existingOutlet == null)
            {
                _logger.LogWarning("Outlet not found for update: {Id}", outletId);
                return null;
            }

            // Update only the properties that are provided in the DTO
            if (!string.IsNullOrEmpty(updateOutletDto.Name))
                existingOutlet.Name = updateOutletDto.Name;

            if (!string.IsNullOrEmpty(updateOutletDto.Location))
                existingOutlet.Location = updateOutletDto.Location;

            if (!string.IsNullOrEmpty(updateOutletDto.OperatingHours))
                existingOutlet.OperatingHours = updateOutletDto.OperatingHours;

            if (updateOutletDto.MaxAdvanceReservationTime.HasValue)
                existingOutlet.MaxAdvanceReservationTime = updateOutletDto.MaxAdvanceReservationTime.Value;

            if (updateOutletDto.MinAdvanceReservationTime.HasValue)
                existingOutlet.MinAdvanceReservationTime = updateOutletDto.MinAdvanceReservationTime.Value;

            if (!string.IsNullOrEmpty(updateOutletDto.Contact))
                existingOutlet.Contact = updateOutletDto.Contact;

            if (updateOutletDto.QueueEnabled.HasValue)
                existingOutlet.QueueEnabled = updateOutletDto.QueueEnabled.Value;

            if (updateOutletDto.SpecialRequirements.HasValue) // Allow empty string
                existingOutlet.SpecialRequirements = updateOutletDto.SpecialRequirements.Value;

            if (!string.IsNullOrEmpty(updateOutletDto.Status))
                existingOutlet.Status = updateOutletDto.Status;

            if (updateOutletDto.Latitude.HasValue)
                existingOutlet.Latitude = updateOutletDto.Latitude;

            if (updateOutletDto.Longitude.HasValue)
                existingOutlet.Longitude = updateOutletDto.Longitude;

            if (updateOutletDto.ReservationAllocationPercent.HasValue)
                existingOutlet.ReservationAllocationPercent = updateOutletDto.ReservationAllocationPercent.Value;

            if (updateOutletDto.DefaultDiningDurationMinutes.HasValue)
                existingOutlet.DefaultDiningDurationMinutes = updateOutletDto.DefaultDiningDurationMinutes.Value;

            existingOutlet.UpdatedAt = DateTime.UtcNow;
            existingOutlet.UpdatedBy = userId;

            var updatedOutlet = await _outletRepository.UpdateAsync(existingOutlet);
            return await MapToOutletDtoWithCapacityAsync(updatedOutlet);
        }

        public async Task<bool> DeleteOutletAsync(Guid outletId)
        {
            _logger.LogInformation("Deleting outlet: {Id}", outletId);

            return await _outletRepository.DeleteAsync(outletId);
        }

        public async Task<IEnumerable<OutletChangeDto>> GetOutletChangesAsync(Guid outletId)
        {
            _logger.LogInformation("Getting changes for outlet: {OutletId}", outletId);

            var changes = await _outletRepository.GetOutletChangesAsync(outletId);
            return changes.Select(MapToOutletChangeDto);
        }

        public async Task<OutletChangeDto> RespondToOutletChangeAsync(Guid changeId, OutletChangeResponseDto responseDto, Guid adminId)
        {
            _logger.LogInformation("Responding to outlet change: {ChangeId} with status: {Status}", changeId, responseDto.Status);

            var change = await _outletRepository.GetOutletChangeByIdAsync(changeId);
            if (change == null)
            {
                _logger.LogWarning("Outlet change not found: {ChangeId}", changeId);
                return null;
            }

            if (change.Status != "Pending")
            {
                _logger.LogWarning("Cannot update change that is not in Pending status: {ChangeId}, current status: {Status}", changeId, change.Status);
                return null;
            }

            change.Status = responseDto.Status;
            change.ReviewedAt = DateTime.UtcNow;
            change.ReviewedBy = adminId;
            change.Comments = responseDto.Comments;

            // If approved, apply the change to the outlet
            if (responseDto.Status == "Approved")
            {
                var outlet = await _outletRepository.GetByIdAsync(change.OutletId);
                if (outlet != null)
                {
                    // Use reflection to set the property value
                    var property = typeof(OutletEntity).GetProperty(change.FieldName);
                    if (property != null)
                    {
                        var convertedValue = ConvertValue(change.NewValue, property.PropertyType);
                        property.SetValue(outlet, convertedValue);
                        outlet.UpdatedAt = DateTime.UtcNow;
                        outlet.UpdatedBy = adminId;
                        await _outletRepository.UpdateAsync(outlet);
                    }
                }
            }

            var updatedChange = await _outletRepository.UpdateOutletChangeAsync(change);
            return MapToOutletChangeDto(updatedChange);
        }

        #region Helper Methods
        private string GenerateOutletId(string outletName)
        {
            // Generate a business-friendly ID based on the outlet name
            // e.g., "Downtown Cafe" -> "DOWNTCAFE001"
            var prefix = new string(outletName.Where(char.IsLetter).Take(8).ToArray()).ToUpper();
            var random = new Random();
            var suffix = random.Next(1, 999).ToString("D3");
            return $"{prefix}{suffix}";
        }

        private async Task<OutletDto> MapToOutletDtoWithCapacityAsync(OutletEntity outlet)
        {
            int totalCapacity = await _tableRepository.GetTotalTablesCapacityAsync(outlet.Id);

            return new OutletDto
            {
                Id = outlet.Id,
                OutletId = outlet.OutletId,
                Name = outlet.Name,
                Location = outlet.Location,
                OperatingHours = outlet.OperatingHours,
                Capacity = totalCapacity, // Set calculated capacity
                MaxAdvanceReservationTime = outlet.MaxAdvanceReservationTime,
                MinAdvanceReservationTime = outlet.MinAdvanceReservationTime,
                Contact = outlet.Contact,
                QueueEnabled = outlet.QueueEnabled,
                SpecialRequirements = outlet.SpecialRequirements,
                Status = outlet.Status,
                CreatedAt = outlet.CreatedAt,
                UpdatedAt = outlet.UpdatedAt,
                Latitude = outlet.Latitude,
                Longitude = outlet.Longitude,
                ReservationAllocationPercent = outlet.ReservationAllocationPercent,
                DefaultDiningDurationMinutes = outlet.DefaultDiningDurationMinutes,
                PeakHourSettings = null // You can populate this if needed
            };
        }

        private OutletChangeDto MapToOutletChangeDto(OutletChange change)
        {
            return new OutletChangeDto
            {
                Id = change.Id,
                OutletId = change.OutletId,
                OutletName = change.Outlet?.Name,
                FieldName = change.FieldName,
                OldValue = change.OldValue,
                NewValue = change.NewValue,
                Status = change.Status,
                RequestedAt = change.RequestedAt,
                RequestedBy = change.RequestedBy.ToString(), // In a real scenario, you might want to get the username
                ReviewedAt = change.ReviewedAt,
                ReviewedBy = change.ReviewedBy?.ToString(),
                Comments = change.Comments
            };
        }

        private object ConvertValue(string value, Type targetType)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            if (targetType == typeof(string))
                return value;

            if (targetType == typeof(int) || targetType == typeof(int?))
                return int.Parse(value);

            if (targetType == typeof(bool) || targetType == typeof(bool?))
                return bool.Parse(value);

            if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
                return DateTime.Parse(value);

            if (targetType == typeof(Guid) || targetType == typeof(Guid?))
                return Guid.Parse(value);

            return value;
        }
        #endregion
    }
}