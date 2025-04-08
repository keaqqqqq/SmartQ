using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Customer.Core.Entities;
using FNBReservation.Modules.Customer.Core.Interfaces;
using FNBReservation.Modules.Customer.Infrastructure.Data;

namespace FNBReservation.Modules.Customer.Infrastructure.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly CustomerDbContext _dbContext;
        private readonly ILogger<CustomerRepository> _logger;

        public CustomerRepository(
            CustomerDbContext dbContext,
            ILogger<CustomerRepository> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<(List<Guid> CustomerIds, int TotalCount)> SearchCustomersAsync(
            string searchTerm,
            string status = null,
            Guid? outletId = null,
            int page = 1,
            int pageSize = 20)
        {
            _logger.LogInformation("Searching customers with term: {SearchTerm}, status: {Status}, outlet: {OutletId}",
                searchTerm, status, outletId);

            IQueryable<CustomerEntity> query = _dbContext.Customers;

            // Filter by status if provided
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(c => c.Status == status);
            }

            // Apply search term to name or phone
            if (!string.IsNullOrEmpty(searchTerm))
            {
                string searchLower = searchTerm.ToLower();
                query = query.Where(c =>
                    c.Name.ToLower().Contains(searchLower) ||
                    c.Phone.Contains(searchLower) ||
                    c.Email.ToLower().Contains(searchLower));
            }

            // If outletId is provided, we'll need to filter based on reservations
            // This will be handled in the service layer since we need to get this data from the reservation adapter

            // Count total before pagination
            int totalCount = await query.CountAsync();

            // Apply pagination
            int skip = (page - 1) * pageSize;
            var customers = await query
                .OrderByDescending(c => c.UpdatedAt)
                .Skip(skip)
                .Take(pageSize)
                .Select(c => c.Id)
                .ToListAsync();

            return (customers, totalCount);
        }

        public async Task<CustomerEntity> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Getting customer by ID: {CustomerId}", id);

            return await _dbContext.Customers
                .Include(c => c.BanHistory)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<CustomerEntity> GetByPhoneAsync(string phone)
        {
            _logger.LogInformation("Getting customer by phone: {Phone}", phone);

            return await _dbContext.Customers
                .Include(c => c.BanHistory)
                .FirstOrDefaultAsync(c => c.Phone == phone);
        }

        public async Task<List<CustomerEntity>> GetByIdsAsync(List<Guid> ids)
        {
            _logger.LogInformation("Getting customers by IDs: {Count} customers", ids.Count);

            return await _dbContext.Customers
                .Include(c => c.BanHistory)
                .Where(c => ids.Contains(c.Id))
                .ToListAsync();
        }

        public async Task<CustomerEntity> UpdateAsync(CustomerEntity customer)
        {
            _logger.LogInformation("Updating customer: {CustomerId}", customer.Id);

            customer.UpdatedAt = DateTime.UtcNow;
            _dbContext.Customers.Update(customer);
            await _dbContext.SaveChangesAsync();

            return customer;
        }

        public async Task<CustomerBanEntity> AddBanAsync(CustomerBanEntity ban)
        {
            _logger.LogInformation("Adding ban for customer: {CustomerId}", ban.CustomerId);

            // First, deactivate any existing active bans
            var existingActiveBan = await GetActiveBanAsync(ban.CustomerId);
            if (existingActiveBan != null)
            {
                existingActiveBan.IsActive = false;
                existingActiveBan.RemovedAt = DateTime.UtcNow;
                existingActiveBan.RemovedById = ban.BannedById; // Use the same admin who's creating the new ban
                _dbContext.CustomerBans.Update(existingActiveBan);
            }

            // Add the new ban
            await _dbContext.CustomerBans.AddAsync(ban);

            // Update customer status
            var customer = await _dbContext.Customers.FindAsync(ban.CustomerId);
            if (customer != null)
            {
                customer.Status = "Banned";
                customer.UpdatedAt = DateTime.UtcNow;
                _dbContext.Customers.Update(customer);
            }

            await _dbContext.SaveChangesAsync();
            return ban;
        }

        public async Task<CustomerBanEntity> GetActiveBanAsync(Guid customerId)
        {
            _logger.LogInformation("Getting active ban for customer: {CustomerId}", customerId);

            return await _dbContext.CustomerBans
                .Where(b => b.CustomerId == customerId && b.IsActive)
                .OrderByDescending(b => b.BannedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<List<CustomerBanEntity>> GetBanHistoryAsync(Guid customerId)
        {
            _logger.LogInformation("Getting ban history for customer: {CustomerId}", customerId);

            return await _dbContext.CustomerBans
                .Where(b => b.CustomerId == customerId)
                .OrderByDescending(b => b.BannedAt)
                .ToListAsync();
        }

        public async Task<List<CustomerBanEntity>> GetActiveBansAsync(Guid? outletId = null)
        {
            _logger.LogInformation("Getting all active bans for outlet: {OutletId}", outletId);

            // This will return all active bans, we'll filter by outlet in the service layer
            // since we need reservation data to determine if a customer has visited that outlet
            return await _dbContext.CustomerBans
                .Include(b => b.Customer)
                .Where(b => b.IsActive)
                .OrderByDescending(b => b.BannedAt)
                .ToListAsync();
        }

        public async Task<CustomerEntity> CreateAsync(CustomerEntity customer)
        {
            _logger.LogInformation("Creating new customer: {Name}, phone: {Phone}", customer.Name, customer.Phone);

            // Check if customer already exists with this phone number
            var existingCustomer = await _dbContext.Customers
                .FirstOrDefaultAsync(c => c.Phone == customer.Phone);

            if (existingCustomer != null)
            {
                _logger.LogWarning("Customer with phone {Phone} already exists", customer.Phone);
                return existingCustomer;
            }

            // Add new customer to database
            await _dbContext.Customers.AddAsync(customer);
            await _dbContext.SaveChangesAsync();

            return customer;
        }
    }
}