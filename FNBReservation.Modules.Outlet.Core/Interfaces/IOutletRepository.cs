// FNBReservation.Modules.Outlet.Core/Interfaces/IOutletRepository.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FNBReservation.Modules.Outlet.Core.Entities;

namespace FNBReservation.Modules.Outlet.Core.Interfaces
{
    public interface IOutletRepository
    {
        Task<OutletEntity> CreateAsync(OutletEntity outlet);
        Task<OutletEntity> GetByIdAsync(Guid id);
        Task<OutletEntity> GetByBusinessIdAsync(string outletId);
        Task<IEnumerable<OutletEntity>> GetAllAsync();
        Task<OutletEntity> UpdateAsync(OutletEntity outlet);
        Task<bool> DeleteAsync(Guid id);
        Task<IEnumerable<OutletChange>> GetOutletChangesAsync(Guid outletId);
        Task<OutletChange> GetOutletChangeByIdAsync(Guid changeId);
        Task<OutletChange> CreateOutletChangeAsync(OutletChange change);
        Task<OutletChange> UpdateOutletChangeAsync(OutletChange change);
    }
}