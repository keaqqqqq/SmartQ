using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Queue.Core.Interfaces;

namespace FNBReservation.Modules.Queue.API.Controllers
{
    [ApiController]
    [Route("api/v1/admin/maintenance")]
    [Authorize(Policy = "AdminOnly")]
    public class MaintenanceController : ControllerBase
    {
        private readonly IQueueMaintenanceService _maintenanceService;
        private readonly ILogger<MaintenanceController> _logger;

        public MaintenanceController(
            IQueueMaintenanceService maintenanceService,
            ILogger<MaintenanceController> logger)
        {
            _maintenanceService = maintenanceService ?? throw new ArgumentNullException(nameof(maintenanceService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost("cleanup-queue")]
        public async Task<IActionResult> CleanupQueue()
        {
            _logger.LogInformation("Manual queue cleanup triggered");

            try
            {
                await _maintenanceService.CleanupActiveQueueEntriesAsync();
                return Ok(new { message = "Queue cleanup executed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during manual queue cleanup");
                return StatusCode(500, new { message = "An error occurred during queue cleanup" });
            }
        }
    }
}