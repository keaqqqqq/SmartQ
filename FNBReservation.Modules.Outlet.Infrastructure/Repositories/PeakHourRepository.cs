// FNBReservation.Modules.Outlet.Infrastructure/Repositories/PeakHourRepository.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Outlet.Core.Entities;
using FNBReservation.Modules.Outlet.Core.Interfaces;
using FNBReservation.Modules.Outlet.Infrastructure.Data;
using FNBReservation.SharedKernel.Data;

namespace FNBReservation.Modules.Outlet.Infrastructure.Repositories
{
    public class PeakHourRepository : BaseRepository<PeakHourSetting, OutletDbContext>, IPeakHourRepository
    {
        private readonly ILogger<PeakHourRepository> _logger;

        public PeakHourRepository(
            DbContextFactory<OutletDbContext> contextFactory,
            ILogger<PeakHourRepository> logger)
            : base(contextFactory, logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PeakHourSetting> CreateAsync(PeakHourSetting peakHourSetting)
        {
            _logger.LogInformation("Creating new peak hour setting {Name} for outlet {OutletId}",
                peakHourSetting.Name, peakHourSetting.OutletId);

            return await ExecuteWriteQueryAsync(async dbSet =>
            {
                var result = await dbSet.AddAsync(peakHourSetting);
                return result.Entity;
            });
        }

        public async Task<PeakHourSetting> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Getting peak hour setting by ID {Id}", id);

            return await ExecuteReadQueryAsync(async dbSet =>
            {
                return await dbSet.FirstOrDefaultAsync(p => p.Id == id);
            });
        }

        public async Task<IEnumerable<PeakHourSetting>> GetByOutletIdAsync(Guid outletId)
        {
            _logger.LogInformation("Getting all peak hour settings for outlet {OutletId}", outletId);

            return await ExecuteReadQueryAsync(async dbSet =>
            {
                return await dbSet
                    .Where(p => p.OutletId == outletId)
                    .OrderBy(p => p.StartTime)
                    .ToListAsync();
            });
        }

        public async Task<IEnumerable<PeakHourSetting>> GetActiveSettingsForDateAsync(Guid outletId, DateTime date)
        {
            _logger.LogInformation("Getting active peak hour settings for outlet {OutletId} on date {Date}",
                outletId, date.Date.ToShortDateString());

            // Get the day of week (1 = Monday, 7 = Sunday)
            int dayOfWeek = (int)date.DayOfWeek;
            if (dayOfWeek == 0) dayOfWeek = 7; // Convert Sunday from 0 to 7 to match our format

            string dayOfWeekStr = dayOfWeek.ToString();

            return await ExecuteReadQueryAsync(async dbSet =>
            {
                // Get active settings for this day of week
                return await dbSet
                    .Where(p => p.OutletId == outletId
                           && p.IsActive
                           && p.DaysOfWeek.Contains(dayOfWeekStr))
                    .OrderBy(p => p.StartTime)
                    .ToListAsync();
            });
        }

        public async Task<PeakHourSetting> GetActiveSettingForDateTimeAsync(Guid outletId, DateTime dateTime)
        {
            _logger.LogInformation("Getting active peak hour setting for outlet {OutletId} at {DateTime}",
                outletId, dateTime.ToString());

            // Get all active settings for this date
            var activeSettings = await GetActiveSettingsForDateAsync(outletId, dateTime.Date);

            // Find the setting that covers the current time
            var currentTime = dateTime.TimeOfDay;
            var matchingSetting = activeSettings.FirstOrDefault(s =>
                currentTime >= s.StartTime && currentTime <= s.EndTime);

            return matchingSetting;
        }

        public async Task<PeakHourSetting> UpdateAsync(PeakHourSetting peakHourSetting)
        {
            _logger.LogInformation("Updating peak hour setting {Id}", peakHourSetting.Id);

            return await ExecuteWriteQueryAsync(async dbSet =>
            {
                dbSet.Update(peakHourSetting);
                return peakHourSetting;
            });
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation("Deleting peak hour setting {Id}", id);

            return await ExecuteWriteQueryAsync(async dbSet =>
            {
                var setting = await dbSet.FindAsync(id);
                if (setting == null)
                {
                    _logger.LogWarning("Peak hour setting {Id} not found for deletion", id);
                    return false;
                }

                dbSet.Remove(setting);
                return true;
            });
        }
    }
}