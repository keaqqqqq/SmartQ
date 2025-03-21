// FNBReservation.Modules.Outlet.Core/Interfaces/IPeakHourService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FNBReservation.Modules.Outlet.Core.DTOs;

namespace FNBReservation.Modules.Outlet.Core.Interfaces
{
    public interface IPeakHourService
    {
        Task<PeakHourSettingDto> CreatePeakHourSettingAsync(Guid outletId, CreatePeakHourSettingDto createDto, Guid userId);
        Task<PeakHourSettingDto> GetPeakHourSettingByIdAsync(Guid id);
        Task<IEnumerable<PeakHourSettingDto>> GetPeakHourSettingsByOutletIdAsync(Guid outletId);
        Task<IEnumerable<PeakHourSettingDto>> GetRamadanSettingsByOutletIdAsync(Guid outletId);
        Task<IEnumerable<PeakHourSettingDto>> GetActivePeakHourSettingsAsync(Guid outletId, DateTime date);
        Task<PeakHourSettingDto> UpdatePeakHourSettingAsync(Guid id, UpdatePeakHourSettingDto updateDto, Guid userId);
        Task<bool> DeletePeakHourSettingAsync(Guid id);
        Task<int> GetCurrentReservationAllocationAsync(Guid outletId, DateTime dateTime);
    }
}