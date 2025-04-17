using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Customer.Core.DTOs;
using FNBReservation.Modules.Customer.Core.Entities;
using FNBReservation.Modules.Customer.Core.Interfaces;

namespace FNBReservation.Modules.Customer.Infrastructure.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ICustomerStatsService _customerStatsService;
        private readonly ILogger<CustomerService> _logger;

        public CustomerService(
            ICustomerRepository customerRepository,
            ICustomerStatsService customerStatsService,
            ILogger<CustomerService> logger)
        {
            _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
            _customerStatsService = customerStatsService;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CustomerListResponseDto> GetAllCustomersAsync(string searchTerm = null, int page = 1, int pageSize = 20, Guid? outletId = null)
        {
            _logger.LogInformation("Getting all customers with search: {SearchTerm}, outlet: {OutletId}, page: {Page}, size: {PageSize}",
                searchTerm, outletId, page, pageSize);

            try
            {
                // Get matching customer IDs and total count from repository
                var (customerIds, totalCount) = await _customerRepository.SearchCustomersAsync(searchTerm, null, null, page, pageSize);

                // Load full customer details
                var customers = await _customerRepository.GetByIdsAsync(customerIds);

                // Filter by outlet if specified (using reservation data)
                List<CustomerDto> customerDtos = new List<CustomerDto>();
                foreach (var customer in customers)
                {
                    var stats = await _customerStatsService.GetCustomerStatsAsync(customer.Phone, outletId);

                    // Skip if no reservations for this outlet
                    if (outletId.HasValue && stats.TotalReservations == 0)
                        continue;

                    // Get ban info if banned
                    CustomerBanInfoDto banInfo = null;
                    if (customer.Status == "Banned")
                    {
                        var activeBan = customer.BanHistory.FirstOrDefault(b => b.IsActive);
                        if (activeBan != null)
                        {
                            banInfo = MapToBanInfoDto(activeBan);
                        }
                    }

                    // Add customer to results
                    customerDtos.Add(new CustomerDto
                    {
                        Id = customer.Id,
                        Name = customer.Name,
                        Phone = customer.Phone,
                        Email = customer.Email,
                        Status = customer.Status,
                        TotalReservations = stats.TotalReservations,
                        NoShows = stats.NoShows,
                        NoShowRate = stats.TotalReservations > 0 ? (decimal)stats.NoShows / stats.TotalReservations * 100 : 0,
                        FirstVisit = stats.FirstVisit,
                        LastVisit = stats.LastVisit,
                        BanInfo = banInfo
                    });
                }

                // Return paginated response
                return new CustomerListResponseDto
                {
                    Customers = customerDtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    SearchTerm = searchTerm
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customers");
                throw;
            }
        }

        public async Task<CustomerListResponseDto> GetActiveCustomersAsync(string searchTerm = null, int page = 1, int pageSize = 20, Guid? outletId = null)
        {
            _logger.LogInformation("Getting active customers with search: {SearchTerm}, outlet: {OutletId}, page: {Page}, size: {PageSize}",
                searchTerm, outletId, page, pageSize);

            try
            {
                // Get matching customer IDs and total count from repository (with Active status filter)
                var (customerIds, totalCount) = await _customerRepository.SearchCustomersAsync(searchTerm, "Active", null, page, pageSize);

                // Load full customer details
                var customers = await _customerRepository.GetByIdsAsync(customerIds);

                // Filter by outlet if specified (using reservation data)
                List<CustomerDto> customerDtos = new List<CustomerDto>();
                foreach (var customer in customers)
                {
                    var stats = await _customerStatsService.GetCustomerStatsAsync(customer.Phone, outletId);

                    // Skip if no reservations for this outlet
                    if (outletId.HasValue && stats.TotalReservations == 0)
                        continue;

                    // Add customer to results
                    customerDtos.Add(new CustomerDto
                    {
                        Id = customer.Id,
                        Name = customer.Name,
                        Phone = customer.Phone,
                        Email = customer.Email,
                        Status = customer.Status,
                        TotalReservations = stats.TotalReservations,
                        NoShows = stats.NoShows,
                        NoShowRate = stats.TotalReservations > 0 ? (decimal)stats.NoShows / stats.TotalReservations * 100 : 0,
                        FirstVisit = stats.FirstVisit, // This could be null now
                        LastVisit = stats.LastVisit,
                        BanInfo = null // Active customers shouldn't have ban info
                    });
                }

                // Return paginated response
                return new CustomerListResponseDto
                {
                    Customers = customerDtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    SearchTerm = searchTerm
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active customers");
                throw;
            }
        }

        public async Task<List<BannedCustomerDto>> GetBannedCustomersAsync(Guid? outletId = null)
        {
            _logger.LogInformation("Getting banned customers for outlet: {OutletId}", outletId);

            try
            {
                // Get all active bans
                var activeBans = await _customerRepository.GetActiveBansAsync();

                List<BannedCustomerDto> bannedCustomers = new List<BannedCustomerDto>();

                foreach (var ban in activeBans)
                {
                    // Skip if customer has no reservations at this outlet
                    if (outletId.HasValue)
                    {
                        var reservations = await _customerStatsService.GetReservationsByCustomerPhoneAsync(ban.Customer.Phone, outletId);
                        if (!reservations.Any())
                            continue;
                    }

                    // Calculate end date if not permanent
                    DateTime? endsAt = null;
                    if (ban.DurationDays > 0)
                    {
                        endsAt = ban.BannedAt.AddDays(ban.DurationDays);
                    }

                    // Add to results
                    bannedCustomers.Add(new BannedCustomerDto
                    {
                        CustomerId = ban.CustomerId,
                        Name = ban.Customer.Name,
                        Phone = ban.Customer.Phone,
                        Reason = ban.Reason,
                        BannedAt = ban.BannedAt,
                        DurationDays = ban.DurationDays,
                        EndsAt = endsAt,
                        BannedByName = "Admin" // Will be overridden by the frontend
                    });
                }

                return bannedCustomers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting banned customers");
                throw;
            }
        }

        public async Task<CustomerDetailDto> GetCustomerByIdAsync(Guid id)
        {
            _logger.LogInformation("Getting customer details by ID: {CustomerId}", id);

            try
            {
                // Get customer entity
                var customer = await _customerRepository.GetByIdAsync(id);
                if (customer == null)
                {
                    _logger.LogWarning("Customer not found: {CustomerId}", id);
                    return null;
                }

                // Get reservation history and stats
                var reservations = await _customerStatsService.GetReservationsByCustomerPhoneAsync(customer.Phone);
                var stats = await _customerStatsService.GetCustomerStatsAsync(customer.Phone);

                // Get active ban info if banned
                CustomerBanInfoDto banInfo = null;
                if (customer.Status == "Banned")
                {
                    var activeBan = customer.BanHistory.FirstOrDefault(b => b.IsActive);
                    if (activeBan != null)
                    {
                        banInfo = MapToBanInfoDto(activeBan);
                    }
                }

                // Create the customer detail DTO
                var customerDetail = new CustomerDetailDto
                {
                    Id = customer.Id,
                    Name = customer.Name,
                    Phone = customer.Phone,
                    Email = customer.Email,
                    Status = customer.Status,
                    TotalReservations = stats.TotalReservations,
                    NoShows = stats.NoShows,
                    NoShowRate = stats.TotalReservations > 0 ? (decimal)stats.NoShows / stats.TotalReservations * 100 : 0,
                    FirstVisit = stats.FirstVisit,
                    LastVisit = stats.LastVisit,
                    BanInfo = banInfo,
                    ReservationHistory = reservations.OrderByDescending(r => r.Date).ToList()
                };

                return customerDetail;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer details: {CustomerId}", id);
                throw;
            }
        }

        public async Task<CustomerDto> BanCustomerAsync(BanCustomerDto banRequest, Guid adminId)
        {
            _logger.LogInformation("Banning customer: {CustomerId} by admin: {AdminId}", banRequest.CustomerId, adminId);

            try
            {
                // Get customer entity
                var customer = await _customerRepository.GetByIdAsync(banRequest.CustomerId);
                if (customer == null)
                {
                    _logger.LogWarning("Customer not found for ban: {CustomerId}", banRequest.CustomerId);
                    throw new ArgumentException($"Customer with ID {banRequest.CustomerId} not found");
                }

                // Create ban entity
                var ban = new CustomerBanEntity
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    Reason = banRequest.Reason,
                    BannedAt = DateTime.UtcNow,
                    DurationDays = banRequest.DurationDays,
                    IsActive = true,
                    BannedById = adminId
                };

                // Add ban and update customer status
                await _customerRepository.AddBanAsync(ban);

                // Get updated customer
                customer = await _customerRepository.GetByIdAsync(banRequest.CustomerId);

                // Get reservation stats
                var stats = await _customerStatsService.GetCustomerStatsAsync(customer.Phone);

                // Return updated customer DTO
                return new CustomerDto
                {
                    Id = customer.Id,
                    Name = customer.Name,
                    Phone = customer.Phone,
                    Email = customer.Email,
                    Status = customer.Status,
                    TotalReservations = stats.TotalReservations,
                    NoShows = stats.NoShows,
                    NoShowRate = stats.TotalReservations > 0 ? (decimal)stats.NoShows / stats.TotalReservations * 100 : 0,
                    FirstVisit = stats.FirstVisit,
                    LastVisit = stats.LastVisit,
                    BanInfo = MapToBanInfoDto(ban)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error banning customer: {CustomerId}", banRequest.CustomerId);
                throw;
            }
        }

        public async Task<CustomerDto> RemoveBanAsync(Guid customerId, Guid adminId)
        {
            _logger.LogInformation("Removing ban for customer: {CustomerId} by admin: {AdminId}", customerId, adminId);

            try
            {
                // Get customer entity
                var customer = await _customerRepository.GetByIdAsync(customerId);
                if (customer == null)
                {
                    _logger.LogWarning("Customer not found for ban removal: {CustomerId}", customerId);
                    throw new ArgumentException($"Customer with ID {customerId} not found");
                }

                // Check if customer is actually banned
                if (customer.Status != "Banned")
                {
                    _logger.LogWarning("Customer is not banned: {CustomerId}", customerId);
                    throw new InvalidOperationException("Customer is not currently banned");
                }

                // Get active ban
                var activeBan = await _customerRepository.GetActiveBanAsync(customerId);
                if (activeBan == null)
                {
                    _logger.LogWarning("No active ban found for customer: {CustomerId}", customerId);
                    throw new InvalidOperationException("No active ban found for this customer");
                }

                // Deactivate the ban
                activeBan.IsActive = false;
                activeBan.RemovedAt = DateTime.UtcNow;
                activeBan.RemovedById = adminId;

                // Update customer status
                customer.Status = "Active";
                customer.UpdatedAt = DateTime.UtcNow;

                // Save changes
                await _customerRepository.UpdateAsync(customer);

                // Get reservation stats
                var stats = await _customerStatsService.GetCustomerStatsAsync(customer.Phone);

                // Return updated customer DTO
                return new CustomerDto
                {
                    Id = customer.Id,
                    Name = customer.Name,
                    Phone = customer.Phone,
                    Email = customer.Email,
                    Status = customer.Status,
                    TotalReservations = stats.TotalReservations,
                    NoShows = stats.NoShows,
                    NoShowRate = stats.TotalReservations > 0 ? (decimal)stats.NoShows / stats.TotalReservations * 100 : 0,
                    FirstVisit = stats.FirstVisit,
                    LastVisit = stats.LastVisit,
                    BanInfo = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing ban for customer: {CustomerId}", customerId);
                throw;
            }
        }

        public async Task<List<CustomerReservationDto>> GetCustomerReservationsAsync(Guid customerId, Guid? outletId = null)
        {
            _logger.LogInformation("Getting reservations for customer: {CustomerId}, outlet: {OutletId}", customerId, outletId);

            try
            {
                // Get customer entity
                var customer = await _customerRepository.GetByIdAsync(customerId);
                if (customer == null)
                {
                    _logger.LogWarning("Customer not found: {CustomerId}", customerId);
                    throw new ArgumentException($"Customer with ID {customerId} not found");
                }

                // Get reservations via adapter
                var reservations = await _customerStatsService.GetReservationsByCustomerPhoneAsync(customer.Phone, outletId);

                return reservations.OrderByDescending(r => r.Date).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reservations for customer: {CustomerId}", customerId);
                throw;
            }
        }

        // Helper method to map ban entity to DTO
        private CustomerBanInfoDto MapToBanInfoDto(CustomerBanEntity ban)
        {
            DateTime? endsAt = null;
            if (ban.DurationDays > 0)
            {
                endsAt = ban.BannedAt.AddDays(ban.DurationDays);
            }

            return new CustomerBanInfoDto
            {
                Id = ban.Id,
                CustomerId = ban.CustomerId,
                Reason = ban.Reason,
                BannedAt = ban.BannedAt,
                DurationDays = ban.DurationDays,
                EndsAt = endsAt,
                BannedById = ban.BannedById,
                BannedByName = "Admin"
            };
        }
    }
}
        