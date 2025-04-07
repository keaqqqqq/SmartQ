// FNBReservation.Modules.Outlet.API/Controllers/AdminTableController.cs (Updated)
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Outlet.Core.DTOs;
using FNBReservation.Modules.Outlet.Core.Interfaces;

namespace FNBReservation.Modules.Outlet.API.Controllers
{
    [ApiController]
    [Route("api/v1/admin/outlets/{outletId}/tables")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminTableController : ControllerBase
    {
        private readonly ITableService _tableService;
        private readonly ILogger<AdminTableController> _logger;

        public AdminTableController(
            ITableService tableService,
            ILogger<AdminTableController> logger)
        {
            _tableService = tableService ?? throw new ArgumentNullException(nameof(tableService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost]
        public async Task<IActionResult> CreateTable(Guid outletId, [FromBody] CreateTableDto createTableDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = GetCurrentUserId();
                var table = await _tableService.CreateTableAsync(outletId, createTableDto, userId);
                return CreatedAtAction(nameof(GetTable), new { outletId, tableId = table.Id }, table);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating table for outlet: {OutletId}", outletId);
                return StatusCode(500, new { message = "An error occurred while creating the table" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTables(Guid outletId)
        {
            try
            {
                var tables = await _tableService.GetTablesByOutletIdAsync(outletId);
                return Ok(tables);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tables for outlet: {OutletId}", outletId);
                return StatusCode(500, new { message = "An error occurred while retrieving tables" });
            }
        }

        [HttpGet("sections")]
        public async Task<IActionResult> GetSections(Guid outletId)
        {
            try
            {
                var sections = await _tableService.GetSectionsByOutletIdAsync(outletId);
                return Ok(sections);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sections for outlet: {OutletId}", outletId);
                return StatusCode(500, new { message = "An error occurred while retrieving sections" });
            }
        }

        [HttpGet("capacity")]
        public async Task<IActionResult> GetCapacity(Guid outletId)
        {
            try
            {
                var totalCapacity = await _tableService.GetTotalTablesCapacityAsync(outletId);
                var reservationCapacity = await _tableService.GetReservationCapacityAsync(outletId);

                return Ok(new
                {
                    totalCapacity = totalCapacity,
                    reservationCapacity = reservationCapacity,
                    walkInCapacity = totalCapacity - reservationCapacity
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting capacity for outlet: {OutletId}", outletId);
                return StatusCode(500, new { message = "An error occurred while retrieving capacity information" });
            }
        }

        [HttpGet("{tableId}")]
        public async Task<IActionResult> GetTable(Guid outletId, Guid tableId)
        {
            try
            {
                var table = await _tableService.GetTableByIdAsync(tableId);

                if (table == null)
                    return NotFound(new { message = "Table not found" });

                if (table.OutletId != outletId)
                    return BadRequest(new { message = "Table does not belong to the specified outlet" });

                return Ok(table);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting table: {TableId} for outlet: {OutletId}", tableId, outletId);
                return StatusCode(500, new { message = "An error occurred while retrieving the table" });
            }
        }

        [HttpPut("{tableId}")]
        public async Task<IActionResult> UpdateTable(Guid outletId, Guid tableId, [FromBody] UpdateTableDto updateTableDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = GetCurrentUserId();
                var table = await _tableService.GetTableByIdAsync(tableId);

                if (table == null)
                    return NotFound(new { message = "Table not found" });

                if (table.OutletId != outletId)
                    return BadRequest(new { message = "Table does not belong to the specified outlet" });

                var updatedTable = await _tableService.UpdateTableAsync(tableId, updateTableDto, userId);
                return Ok(updatedTable);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating table: {TableId} for outlet: {OutletId}", tableId, outletId);
                return StatusCode(500, new { message = "An error occurred while updating the table" });
            }
        }

        [HttpDelete("{tableId}")]
        public async Task<IActionResult> DeleteTable(Guid outletId, Guid tableId)
        {
            try
            {
                var table = await _tableService.GetTableByIdAsync(tableId);

                if (table == null)
                    return NotFound(new { message = "Table not found" });

                if (table.OutletId != outletId)
                    return BadRequest(new { message = "Table does not belong to the specified outlet" });

                var result = await _tableService.DeleteTableAsync(tableId);
                return Ok(new { message = "Table deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting table: {TableId} for outlet: {OutletId}", tableId, outletId);
                return StatusCode(500, new { message = "An error occurred while deleting the table" });
            }
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                _logger.LogWarning("User ID claim not found in token");
                throw new InvalidOperationException("User ID claim not found in token");
            }

            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                _logger.LogWarning("Failed to parse user ID from claim: {UserIdClaim}", userIdClaim);
                throw new InvalidOperationException($"Invalid user ID format in token: {userIdClaim}");
            }

            return userId;
        }
    }
}