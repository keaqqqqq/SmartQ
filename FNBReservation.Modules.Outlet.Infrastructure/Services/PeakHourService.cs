// FNBReservation.Modules.Outlet.Infrastructure/Services/PeakHourService.cs
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Outlet.Core.DTOs;
using FNBReservation.Modules.Outlet.Core.Entities;
using FNBReservation.Modules.Outlet.Core.Interfaces;

namespace FNBReservation.Modules.Outlet.Infrastructure.Services
{
    public class PeakHourService : IPeakHourService
    {
        private readonly IPeakHourRepository _peakHourRepository;
        private readonly IOutletRepository _outletRepository;
        private readonly ILogger<PeakHourService> _logger;

        public PeakHourService(
            IPeakHourRepository peakHourRepository,
            IOutletRepository outletRepository,
            ILogger<PeakHourService> logger)
        {
            _peakHourRepository = peakHourRepository ?? throw new ArgumentNullException(nameof(peakHourRepository));
            _outletRepository = outletRepository ?? throw new ArgumentNullException(nameof(outletRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PeakHourSettingDto> CreatePeakHourSettingAsync(
            Guid outletId,
            CreatePeakHourSettingDto createDto,
            Guid userId)
        {
            _logger.LogInformation("Creating peak hour setting {Name} for outlet {OutletId}",
                createDto.Name, outletId);

            // Validate outlet exists
            var outlet = await _outletRepository.GetByIdAsync(outletId);
            if (outlet == null)
            {
                _logger.LogWarning("Outlet {OutletId} not found", outletId);
                throw new ArgumentException($"Outlet with ID {outletId} not found");
            }

            // Validate times
            if (createDto.EndTime <= createDto.StartTime)
            {
                _logger.LogWarning("End time must be after start time");
                throw new ArgumentException("End time must be after start time");
            }

            // Create entity
            var peakHourSetting = new PeakHourSetting
            {
                Id = Guid.NewGuid(),
                OutletId = outletId,
                Name = createDto.Name,
                DaysOfWeek = createDto.DaysOfWeek,
                StartTime = createDto.StartTime,
                EndTime = createDto.EndTime,
                ReservationAllocationPercent = createDto.ReservationAllocationPercent,
                IsActive = createDto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };

            // Save to database
            var createdSetting = await _peakHourRepository.CreateAsync(peakHourSetting);

            return MapToPeakHourSettingDto(createdSetting);
        }

        public async Task<PeakHourSettingDto> GetPeakHourSettingByIdAsync(Guid id)
        {
            _logger.LogInformation("Getting peak hour setting {Id}", id);

            var setting = await _peakHourRepository.GetByIdAsync(id);
            if (setting == null)
            {
                _logger.LogWarning("Peak hour setting {Id} not found", id);
                return null;
            }

            return MapToPeakHourSettingDto(setting);
        }

        public async Task<IEnumerable<PeakHourSettingDto>> GetPeakHourSettingsByOutletIdAsync(Guid outletId)
        {
            _logger.LogInformation("Getting all peak hour settings for outlet {OutletId}", outletId);

            var settings = await _peakHourRepository.GetByOutletIdAsync(outletId);
            return settings.Select(MapToPeakHourSettingDto);
        }

        public async Task<IEnumerable<PeakHourSettingDto>> GetActivePeakHourSettingsAsync(Guid outletId, DateTime date)
        {
            _logger.LogInformation("Getting active peak hour settings for outlet {OutletId} on date {Date}",
                outletId, date.ToShortDateString());

            var settings = await _peakHourRepository.GetActiveSettingsForDateAsync(outletId, date);
            return settings.Select(MapToPeakHourSettingDto);
        }

        public async Task<PeakHourSettingDto> UpdatePeakHourSettingAsync(
            Guid id,
            UpdatePeakHourSettingDto updateDto,
            Guid userId)
        {
            _logger.LogInformation("Updating peak hour setting {Id}", id);

            var setting = await _peakHourRepository.GetByIdAsync(id);
            if (setting == null)
            {
                _logger.LogWarning("Peak hour setting {Id} not found for update", id);
                return null;
            }

            // Update properties if provided
            if (!string.IsNullOrEmpty(updateDto.Name))
                setting.Name = updateDto.Name;

            if (!string.IsNullOrEmpty(updateDto.DaysOfWeek))
                setting.DaysOfWeek = updateDto.DaysOfWeek;

            if (updateDto.StartTime.HasValue)
                setting.StartTime = updateDto.StartTime.Value;

            if (updateDto.EndTime.HasValue)
                setting.EndTime = updateDto.EndTime.Value;

            if (updateDto.ReservationAllocationPercent.HasValue)
                setting.ReservationAllocationPercent = updateDto.ReservationAllocationPercent.Value;

            if (updateDto.IsActive.HasValue)
                setting.IsActive = updateDto.IsActive.Value;

            // Validate times
            if (setting.EndTime <= setting.StartTime)
            {
                _logger.LogWarning("End time must be after start time");
                throw new ArgumentException("End time must be after start time");
            }

            setting.UpdatedAt = DateTime.UtcNow;
            setting.UpdatedBy = userId;

            // Save changes
            var updatedSetting = await _peakHourRepository.UpdateAsync(setting);

            return MapToPeakHourSettingDto(updatedSetting);
        }

        public async Task<bool> DeletePeakHourSettingAsync(Guid id)
        {
            _logger.LogInformation("Deleting peak hour setting {Id}", id);

            return await _peakHourRepository.DeleteAsync(id);
        }

        public async Task<int> GetCurrentReservationAllocationAsync(Guid outletId, DateTime dateTime)
        {
            _logger.LogInformation("Getting current reservation allocation for outlet {OutletId} at {DateTime}",
                outletId, dateTime.ToString());

            // First check if there's an active peak hour setting for this date and time
            var activeSetting = await _peakHourRepository.GetActiveSettingForDateTimeAsync(outletId, dateTime);

            if (activeSetting != null)
            {
                _logger.LogInformation("Found active peak hour setting: {SettingName} with allocation {Allocation}%",
                    activeSetting.Name, activeSetting.ReservationAllocationPercent);
                return activeSetting.ReservationAllocationPercent;
            }

            // If no active peak hour setting, use the outlet's default allocation
            var outlet = await _outletRepository.GetByIdAsync(outletId);
            if (outlet == null)
            {
                _logger.LogWarning("Outlet {OutletId} not found", outletId);
                throw new ArgumentException($"Outlet with ID {outletId} not found");
            }

            _logger.LogInformation("Using default outlet allocation: {Allocation}%",
                outlet.ReservationAllocationPercent);
            return outlet.ReservationAllocationPercent;
        }

        // Helper methods
        private PeakHourSettingDto MapToPeakHourSettingDto(PeakHourSetting entity)
        {
            return new PeakHourSettingDto
            {
                Id = entity.Id,
                OutletId = entity.OutletId,
                Name = entity.Name,
                DaysOfWeek = entity.DaysOfWeek,
                StartTime = entity.StartTime,
                EndTime = entity.EndTime,
                ReservationAllocationPercent = entity.ReservationAllocationPercent,
                IsActive = entity.IsActive
            };
        }
    }
}