// FNBReservation.Modules.Outlet.Core/Interfaces/ITableService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FNBReservation.Modules.Outlet.Core.DTOs;

namespace FNBReservation.Modules.Outlet.Core.Interfaces
{
    public interface ITableService
    {
        Task<TableDto> CreateTableAsync(Guid outletId, CreateTableDto createTableDto, Guid userId);
        Task<TableDto> GetTableByIdAsync(Guid tableId);
        Task<IEnumerable<TableDto>> GetTablesByOutletIdAsync(Guid outletId);
        Task<IEnumerable<TableDto>> GetReservationOnlyTablesAsync(Guid outletId);
        Task<IEnumerable<SectionDto>> GetSectionsByOutletIdAsync(Guid outletId);
        Task<TableDto> UpdateTableAsync(Guid tableId, UpdateTableDto updateTableDto, Guid userId);
        Task<bool> DeleteTableAsync(Guid tableId);
        Task<int> GetTotalTablesCapacityAsync(Guid outletId);
        Task<int> GetReservationCapacityAsync(Guid outletId);
    }
}