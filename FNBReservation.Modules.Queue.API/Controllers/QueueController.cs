// FNBReservation.Modules.Queue.API/Controllers/QueueController.cs
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Queue.Core.DTOs;
using FNBReservation.Modules.Queue.Core.Interfaces;
using System.Runtime.InteropServices;

namespace FNBReservation.Modules.Queue.API.Controllers
{
    [ApiController]
    [Route("api/v1/queue")]
    public class QueueController : ControllerBase
    {
        private readonly IQueueService _queueService;
        private readonly ILogger<QueueController> _logger;

        public QueueController(
            IQueueService queueService,
            ILogger<QueueController> logger)
        {
            _queueService = queueService ?? throw new ArgumentNullException(nameof(queueService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost]
        public async Task<IActionResult> CreateQueueEntry([FromBody] CreateQueueEntryDto createQueueEntryDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var queueEntry = await _queueService.CreateQueueEntryAsync(createQueueEntryDto);
                return CreatedAtAction(nameof(GetQueueEntryByCode), new { queueCode = queueEntry.QueueCode }, queueEntry);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating queue entry for outlet: {OutletId}", createQueueEntryDto.OutletId);
                return StatusCode(500, new { message = "An error occurred while creating the queue entry" });
            }
        }

        [HttpGet("code/{queueCode}")]
        public async Task<IActionResult> GetQueueEntryByCode(string queueCode)
        {
            try
            {
                var queueEntry = await _queueService.GetQueueEntryByCodeAsync(queueCode);
                if (queueEntry == null)
                    return NotFound(new { message = "Queue entry not found" });

                return Ok(queueEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue entry by code: {QueueCode}", queueCode);
                return StatusCode(500, new { message = "An error occurred while retrieving the queue entry" });
            }
        }

        [HttpGet("wait-time/{outletId}/{partySize}")]
        public async Task<IActionResult> GetEstimatedWaitTime(Guid outletId, int partySize)
        {
            try
            {
                var waitTime = await _queueService.GetEstimatedWaitTimeAsync(outletId, partySize);
                return Ok(new { estimatedWaitMinutes = waitTime });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting estimated wait time for outlet: {OutletId}, party size: {PartySize}", outletId, partySize);
                return StatusCode(500, new { message = "An error occurred while calculating the estimated wait time" });
            }
        }

        [HttpPost("exit/{queueCode}")]
        public async Task<IActionResult> ExitQueue(string queueCode)
        {
            try
            {
                var queueEntry = await _queueService.GetQueueEntryByCodeAsync(queueCode);
                if (queueEntry == null)
                    return NotFound(new { message = "Queue entry not found" });

                var result = await _queueService.CancelQueueEntryAsync(queueEntry.Id, "Customer voluntarily exited queue", null);
                if (!result)
                    return BadRequest(new { message = "Failed to exit queue" });

                return Ok(new { message = "Successfully exited queue" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exiting queue for code: {QueueCode}", queueCode);
                return StatusCode(500, new { message = "An error occurred while exiting the queue" });
            }
        }

        [HttpPut("{queueCode}")]
        public async Task<IActionResult> UpdateQueueEntry(string queueCode, [FromBody] UpdateQueueEntryDto updateQueueEntryDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var queueEntry = await _queueService.GetQueueEntryByCodeAsync(queueCode);
                if (queueEntry == null)
                    return NotFound(new { message = "Queue entry not found" });

                var updatedEntry = await _queueService.UpdateQueueEntryAsync(queueEntry.Id, updateQueueEntryDto);
                return Ok(updatedEntry);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating queue entry with code: {QueueCode}", queueCode);
                return StatusCode(500, new { message = "An error occurred while updating the queue entry" });
            }
        }
    }
}