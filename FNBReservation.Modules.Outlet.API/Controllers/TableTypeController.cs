// FNBReservation.Modules.Outlet.API/Controllers/TableTypeController.cs

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Outlet.Core.Interfaces;

namespace FNBReservation.Modules.Outlet.API.Controllers
{
    [ApiController]
    [Route("api/v1/outlets/{outletId}/table-types")]
    [Authorize(Policy = "StaffOnly")]
    public class TableTypeController : ControllerBase
    {
        private readonly ITableTypeService _tableTypeService;
        private readonly ILogger<TableTypeController> _logger;

        public TableTypeController(
            ITableTypeService tableTypeService,
            ILogger<TableTypeController> logger)
        {
            _tableTypeService = tableTypeService ?? throw new ArgumentNullException(nameof(tableTypeService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("reservation")]
        public async Task<IActionResult> GetReservationTables(
            Guid outletId,
            [FromQuery] DateTime? dateTime = null)
        {
            try
            {
                var actualDateTime = dateTime ?? DateTime.UtcNow;
                var tables = await _tableTypeService.GetReservationTablesAsync(outletId, actualDateTime);
                return Ok(tables);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reservation tables for outlet: {OutletId}", outletId);
                return StatusCode(500, new { message = "An error occurred while retrieving reservation tables" });
            }
        }

        [HttpGet("queue")]
        public async Task<IActionResult> GetQueueTables(
            Guid outletId,
            [FromQuery] DateTime? dateTime = null)
        {
            try
            {
                var actualDateTime = dateTime ?? DateTime.UtcNow;
                var tables = await _tableTypeService.GetQueueTablesAsync(outletId, actualDateTime);
                return Ok(tables);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue tables for outlet: {OutletId}", outletId);
                return StatusCode(500, new { message = "An error occurred while retrieving queue tables" });
            }
        }

        [HttpGet("{tableId}/type")]
        public async Task<IActionResult> GetTableType(
            Guid outletId,
            Guid tableId,
            [FromQuery] DateTime? dateTime = null)
        {
            try
            {
                var actualDateTime = dateTime ?? DateTime.UtcNow;

                bool isReservationTable = await _tableTypeService.IsReservationTableAsync(
                    outletId, tableId, actualDateTime);

                bool isQueueTable = await _tableTypeService.IsQueueTableAsync(
                    outletId, tableId, actualDateTime);

                return Ok(new
                {
                    tableId = tableId,
                    isReservationTable = isReservationTable,
                    isQueueTable = isQueueTable,
                    type = isReservationTable ? "Reservation" : (isQueueTable ? "Queue" : "Unknown")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting table type for table: {TableId} in outlet: {OutletId}",
                    tableId, outletId);
                return StatusCode(500, new { message = "An error occurred while getting table type" });
            }
        }
    }
}
