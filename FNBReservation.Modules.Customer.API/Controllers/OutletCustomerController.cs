using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Customer.Core.DTOs;
using FNBReservation.Modules.Customer.Core.Interfaces;

namespace FNBReservation.Modules.Customer.API.Controllers
{
    [ApiController]
    [Route("api/v1/outlets/{outletId}/customers")]
    [Authorize(Policy = "StaffOnly")]
    public class OutletCustomerController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly ILogger<OutletCustomerController> _logger;

        public OutletCustomerController(
            ICustomerService customerService,
            ILogger<OutletCustomerController> logger)
        {
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public async Task<IActionResult> GetOutletCustomers(
            Guid outletId,
            [FromQuery] string searchTerm = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                // Check if the user has access to this outlet
                if (!await HasAccessToOutlet(outletId))
                {
                    return Forbid();
                }

                var result = await _customerService.GetAllCustomersAsync(searchTerm, page, pageSize, outletId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customers for outlet: {OutletId}", outletId);
                return StatusCode(500, new { message = "An error occurred while retrieving customers" });
            }
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActiveOutletCustomers(
            Guid outletId,
            [FromQuery] string searchTerm = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                // Check if the user has access to this outlet
                if (!await HasAccessToOutlet(outletId))
                {
                    return Forbid();
                }

                var result = await _customerService.GetActiveCustomersAsync(searchTerm, page, pageSize, outletId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active customers for outlet: {OutletId}", outletId);
                return StatusCode(500, new { message = "An error occurred while retrieving active customers" });
            }
        }

        [HttpGet("banned")]
        public async Task<IActionResult> GetBannedOutletCustomers(Guid outletId)
        {
            try
            {
                // Check if the user has access to this outlet
                if (!await HasAccessToOutlet(outletId))
                {
                    return Forbid();
                }

                var bannedCustomers = await _customerService.GetBannedCustomersAsync(outletId);
                return Ok(bannedCustomers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting banned customers for outlet: {OutletId}", outletId);
                return StatusCode(500, new { message = "An error occurred while retrieving banned customers" });
            }
        }

        [HttpGet("{customerId}")]
        public async Task<IActionResult> GetCustomerById(Guid outletId, Guid customerId)
        {
            try
            {
                // Check if the user has access to this outlet
                if (!await HasAccessToOutlet(outletId))
                {
                    return Forbid();
                }

                var customer = await _customerService.GetCustomerByIdAsync(customerId);
                if (customer == null)
                    return NotFound(new { message = "Customer not found" });

                // Check if this customer has ever made a reservation at this outlet
                bool hasOutletReservation = customer.ReservationHistory.Any(r => r.OutletId == outletId);
                if (!hasOutletReservation)
                {
                    return NotFound(new { message = "Customer not found at this outlet" });
                }

                return Ok(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer: {CustomerId} for outlet: {OutletId}", customerId, outletId);
                return StatusCode(500, new { message = "An error occurred while retrieving the customer" });
            }
        }

        [HttpGet("{customerId}/reservations")]
        public async Task<IActionResult> GetCustomerReservations(Guid outletId, Guid customerId)
        {
            try
            {
                // Check if the user has access to this outlet
                if (!await HasAccessToOutlet(outletId))
                {
                    return Forbid();
                }

                var reservations = await _customerService.GetCustomerReservationsAsync(customerId, outletId);
                return Ok(reservations);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reservations for customer: {CustomerId} at outlet: {OutletId}", customerId, outletId);
                return StatusCode(500, new { message = "An error occurred while retrieving customer reservations" });
            }
        }

        private Task<bool> HasAccessToOutlet(Guid outletId)
        {
            // Get the user's role from the claims
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            // Admins have access to all outlets
            if (role == "Admin")
                return Task.FromResult(true);

            // For OutletStaff, check if they belong to this outlet
            if (role == "OutletStaff")
            {
                // Get the user's OutletId claim
                var userOutletId = User.FindFirst("OutletId")?.Value;

                if (!string.IsNullOrEmpty(userOutletId) && Guid.TryParse(userOutletId, out Guid staffOutletId))
                {
                    // Check if the staff's outlet ID matches the requested outlet ID
                    return Task.FromResult(staffOutletId == outletId);
                }
            }

            // Default to no access
            return Task.FromResult(false);
        }
    }
}
        