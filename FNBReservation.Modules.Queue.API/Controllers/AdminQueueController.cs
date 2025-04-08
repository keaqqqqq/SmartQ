// FNBReservation.Modules.Queue.API/Controllers/AdminQueueController.cs
using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Queue.Core.Interfaces;

namespace FNBReservation.Modules.Queue.API.Controllers
{
    [ApiController]
    [Route("api/v1/admin/queue")]
    [Authorize(Policy = "StaffOnly")]
    public class AdminQueueController : ControllerBase
    {
        private readonly IQueueService _queueService;
        private readonly ILogger<AdminQueueController> _logger;

        public AdminQueueController(
            IQueueService queueService,
            ILogger<AdminQueueController> logger)
        {
            _queueService = queueService ?? throw new ArgumentNullException(nameof(queueService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("summary/{outletId}")]
        public async Task<IActionResult> GetQueueSummary(Guid outletId)
        {
            try
            {
                var summary = await _queueService.GetQueueSummaryAsync(outletId);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue summary for outlet: {OutletId}", outletId);
                return StatusCode(500, new { message = "An error occurred while retrieving queue summary" });
            }
        }

        [HttpGet("all/{outletId}")]
        public async Task<IActionResult> GetAllQueueEntries(
            Guid outletId,
            [FromQuery] List<string> statuses = null,
            [FromQuery] string searchTerm = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var entries = await _queueService.GetQueueEntriesAsync(
                    outletId, statuses, searchTerm, page, pageSize);
                return Ok(entries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue entries for outlet: {OutletId}", outletId);
                return StatusCode(500, new { message = "An error occurred while retrieving queue entries" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteQueueEntry(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _queueService.CancelQueueEntryAsync(id, "Deleted by admin", userId);
                if (!result)
                    return NotFound(new { message = "Queue entry not found" });

                return Ok(new { message = "Queue entry deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting queue entry: {QueueEntryId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the queue entry" });
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