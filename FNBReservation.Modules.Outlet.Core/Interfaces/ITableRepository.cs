// FNBReservation.Modules.Outlet.Core/Interfaces/ITableRepository.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FNBReservation.Modules.Outlet.Core.Entities;

namespace FNBReservation.Modules.Outlet.Core.Interfaces
{
    public interface ITableRepository
    {
        Task<TableEntity> CreateAsync(TableEntity table);
        Task<TableEntity> GetByIdAsync(Guid id);
        Task<IEnumerable<TableEntity>> GetByOutletIdAsync(Guid outletId);
        Task<IEnumerable<string>> GetSectionsByOutletIdAsync(Guid outletId);
        Task<int> GetTableCountBySectionAsync(Guid outletId, string section);
        Task<int> GetTotalCapacityBySectionAsync(Guid outletId, string section);
        Task<TableEntity> UpdateAsync(TableEntity table);
        Task<bool> DeleteAsync(Guid id);
        Task<int> GetTotalTablesCapacityAsync(Guid outletId);
        Task<int> GetReservationCapacityAsync(Guid outletId);
    }
}