// FNBReservation.Modules.Outlet.Core/Interfaces/IOutletService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FNBReservation.Modules.Outlet.Core.DTOs;
using FNBReservation.Modules.Outlet.Core.Entities;

namespace FNBReservation.Modules.Outlet.Core.Interfaces
{
    public interface IOutletService
    {
        Task<OutletDto> CreateOutletAsync(CreateOutletDto createOutletDto, Guid userId);
        Task<OutletDto> GetOutletByIdAsync(Guid outletId);
        Task<OutletDto> GetOutletByBusinessIdAsync(string outletId);
        Task<IEnumerable<OutletDto>> GetAllOutletsAsync();
        Task<OutletDto> UpdateOutletAsync(Guid outletId, UpdateOutletDto updateOutletDto, Guid userId);
        Task<bool> DeleteOutletAsync(Guid outletId);
        Task<IEnumerable<OutletChangeDto>> GetOutletChangesAsync(Guid outletId);
        Task<OutletChangeDto> RespondToOutletChangeAsync(Guid changeId, OutletChangeResponseDto responseDto, Guid adminId);
    }
}

