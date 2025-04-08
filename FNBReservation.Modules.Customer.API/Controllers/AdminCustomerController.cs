using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Customer.Core.DTOs;
using FNBReservation.Modules.Customer.Core.Interfaces;

namespace FNBReservation.Modules.Customer.API.Controllers
{
    [ApiController]
    [Route("api/v1/admin/customers")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminCustomerController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly ILogger<AdminCustomerController> _logger;

        public AdminCustomerController(
            ICustomerService customerService,
            ILogger<AdminCustomerController> logger)
        {
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCustomers(
            [FromQuery] string searchTerm = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var result = await _customerService.GetAllCustomersAsync(searchTerm, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all customers");
                return StatusCode(500, new { message = "An error occurred while retrieving customers" });
            }
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActiveCustomers(
            [FromQuery] string searchTerm = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var result = await _customerService.GetActiveCustomersAsync(searchTerm, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active customers");
                return StatusCode(500, new { message = "An error occurred while retrieving active customers" });
            }
        }

        [HttpGet("banned")]
        public async Task<IActionResult> GetBannedCustomers()
        {
            try
            {
                var bannedCustomers = await _customerService.GetBannedCustomersAsync();
                return Ok(bannedCustomers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting banned customers");
                return StatusCode(500, new { message = "An error occurred while retrieving banned customers" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCustomerById(Guid id)
        {
            try
            {
                var customer = await _customerService.GetCustomerByIdAsync(id);
                if (customer == null)
                    return NotFound(new { message = "Customer not found" });

                return Ok(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer: {CustomerId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the customer" });
            }
        }

        [HttpGet("{id}/reservations")]
        public async Task<IActionResult> GetCustomerReservations(Guid id)
        {
            try
            {
                var reservations = await _customerService.GetCustomerReservationsAsync(id);
                return Ok(reservations);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reservations for customer: {CustomerId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving customer reservations" });
            }
        }

        [HttpPost("{id}/ban")]
        public async Task<IActionResult> BanCustomer(Guid id, [FromBody] BanCustomerDto banRequest)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Ensure the ID in the URL matches the one in the request
            if (id != banRequest.CustomerId)
            {
                return BadRequest(new { message = "Customer ID in URL must match the one in the request body" });
            }

            try
            {
                var userId = GetCurrentUserId();
                var customer = await _customerService.BanCustomerAsync(banRequest, userId);
                return Ok(customer);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error banning customer: {CustomerId}", id);
                return StatusCode(500, new { message = "An error occurred while banning the customer" });
            }
        }

        [HttpPost("{id}/remove-ban")]
        public async Task<IActionResult> RemoveBan(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var customer = await _customerService.RemoveBanAsync(id, userId);
                return Ok(customer);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing ban for customer: {CustomerId}", id);
                return StatusCode(500, new { message = "An error occurred while removing the ban" });
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