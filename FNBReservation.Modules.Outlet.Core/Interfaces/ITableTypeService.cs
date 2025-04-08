// FNBReservation.Modules.Outlet.Core/Interfaces/ITableTypeService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FNBReservation.Modules.Outlet.Core.DTOs;

namespace FNBReservation.Modules.Outlet.Core.Interfaces
{
    public interface ITableTypeService
    {
        /// <summary>
        /// Gets tables designated for reservations based on the outlet's reservation allocation percent
        /// </summary>
        Task<List<TableDto>> GetReservationTablesAsync(Guid outletId, DateTime dateTime);

        /// <summary>
        /// Gets tables designated for walk-ins and queue based on the outlet's reservation allocation percent
        /// </summary>
        Task<List<TableDto>> GetQueueTablesAsync(Guid outletId, DateTime dateTime);

        /// <summary>
        /// Determines if a specific table is available for reservations at the given time
        /// </summary>
        Task<bool> IsReservationTableAsync(Guid outletId, Guid tableId, DateTime dateTime);

        /// <summary>
        /// Determines if a specific table is available for queue at the given time
        /// </summary>
        Task<bool> IsQueueTableAsync(Guid outletId, Guid tableId, DateTime dateTime);
    }
}
