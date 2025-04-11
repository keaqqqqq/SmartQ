// FNBReservation.Modules.Outlet.Core/Interfaces/IPeakHourRepository.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FNBReservation.Modules.Outlet.Core.Entities;

namespace FNBReservation.Modules.Outlet.Core.Interfaces
{
    public interface IPeakHourRepository
    {
        Task<PeakHourSetting> CreateAsync(PeakHourSetting peakHourSetting);
        Task<PeakHourSetting> GetByIdAsync(Guid id);
        Task<IEnumerable<PeakHourSetting>> GetByOutletIdAsync(Guid outletId);
        Task<IEnumerable<PeakHourSetting>> GetActiveSettingsForDateAsync(Guid outletId, DateTime date);
        Task<PeakHourSetting> GetActiveSettingForDateTimeAsync(Guid outletId, DateTime dateTime);
        Task<PeakHourSetting> UpdateAsync(PeakHourSetting peakHourSetting);
        Task<bool> DeleteAsync(Guid id);
    }
}