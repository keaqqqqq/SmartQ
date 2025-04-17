using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Customer.Core.Entities;
using FNBReservation.Modules.Customer.Core.Interfaces;
using FNBReservation.Modules.Customer.Infrastructure.Data;
using FNBReservation.SharedKernel.Data;

namespace FNBReservation.Modules.Customer.Infrastructure.Repositories
{
    public class CustomerRepository : BaseRepository<CustomerEntity, CustomerDbContext>, ICustomerRepository
    {
        private readonly ILogger<CustomerRepository> _logger;

        public CustomerRepository(
            DbContextFactory<CustomerDbContext> contextFactory,
            ILogger<CustomerRepository> logger)
            : base(contextFactory, logger)
        {
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

            using var context = _contextFactory.CreateReadContext();
            IQueryable<CustomerEntity> query = context.Customers;

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

            return await ExecuteReadQueryAsync(async dbSet =>
            {
                return await dbSet
                    .Include(c => c.BanHistory)
                    .FirstOrDefaultAsync(c => c.Id == id);
            });
        }

        public async Task<CustomerEntity> GetByPhoneAsync(string phone)
        {
            _logger.LogInformation("Getting customer by phone: {Phone}", phone);

            return await ExecuteReadQueryAsync(async dbSet =>
            {
                return await dbSet
                    .Include(c => c.BanHistory)
                    .FirstOrDefaultAsync(c => c.Phone == phone);
            });
        }

        public async Task<List<CustomerEntity>> GetByIdsAsync(List<Guid> ids)
        {
            _logger.LogInformation("Getting customers by IDs: {Count} customers", ids.Count);

            return await ExecuteReadQueryAsync(async dbSet =>
            {
                return await dbSet
                    .Include(c => c.BanHistory)
                    .Where(c => ids.Contains(c.Id))
                    .ToListAsync();
            });
        }

        public async Task<CustomerEntity> UpdateAsync(CustomerEntity customer)
        {
            _logger.LogInformation("Updating customer: {CustomerId}", customer.Id);

            return await ExecuteWriteQueryAsync(async dbSet =>
            {
                customer.UpdatedAt = DateTime.UtcNow;
                dbSet.Update(customer);
                return customer;
            });
        }

        public async Task<CustomerBanEntity> AddBanAsync(CustomerBanEntity ban)
        {
            _logger.LogInformation("Adding ban for customer: {CustomerId}", ban.CustomerId);

            // Use a regular write context instead of a transaction
            using var context = _contextFactory.CreateWriteContext();

            try
            {
                // First, deactivate any existing active bans
                var existingActiveBan = await context.CustomerBans
                    .Where(b => b.CustomerId == ban.CustomerId && b.IsActive)
                    .OrderByDescending(b => b.BannedAt)
                    .FirstOrDefaultAsync();

                if (existingActiveBan != null)
                {
                    existingActiveBan.IsActive = false;
                    existingActiveBan.RemovedAt = DateTime.UtcNow;
                    existingActiveBan.RemovedById = ban.BannedById; // Use the same admin who's creating the new ban
                    context.CustomerBans.Update(existingActiveBan);
                }

                // Add the new ban
                await context.CustomerBans.AddAsync(ban);

                // Update customer status
                var customer = await context.Customers.FindAsync(ban.CustomerId);
                if (customer != null)
                {
                    customer.Status = "Banned";
                    customer.UpdatedAt = DateTime.UtcNow;
                    context.Customers.Update(customer);
                }

                // Save all changes in a single operation
                await context.SaveChangesAsync();

                return ban;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while adding ban for customer {CustomerId}", ban.CustomerId);
                throw;
            }
        }

        public async Task<CustomerBanEntity> GetActiveBanAsync(Guid customerId)
        {
            _logger.LogInformation("Getting active ban for customer: {CustomerId}", customerId);

            using var context = _contextFactory.CreateReadContext();
            return await context.CustomerBans
                .Where(b => b.CustomerId == customerId && b.IsActive)
                .OrderByDescending(b => b.BannedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<List<CustomerBanEntity>> GetBanHistoryAsync(Guid customerId)
        {
            _logger.LogInformation("Getting ban history for customer: {CustomerId}", customerId);

            using var context = _contextFactory.CreateReadContext();
            return await context.CustomerBans
                .Where(b => b.CustomerId == customerId)
                .OrderByDescending(b => b.BannedAt)
                .ToListAsync();
        }

        public async Task<List<CustomerBanEntity>> GetActiveBansAsync(Guid? outletId = null)
        {
            _logger.LogInformation("Getting all active bans for outlet: {OutletId}", outletId);

            // This will return all active bans, we'll filter by outlet in the service layer
            // since we need reservation data to determine if a customer has visited that outlet
            using var context = _contextFactory.CreateReadContext();
            return await context.CustomerBans
                .Include(b => b.Customer)
                .Where(b => b.IsActive)
                .OrderByDescending(b => b.BannedAt)
                .ToListAsync();
        }

        public async Task<CustomerEntity> CreateAsync(CustomerEntity customer)
        {
            _logger.LogInformation("Creating new customer: {Name}, phone: {Phone}", customer.Name, customer.Phone);

            // Use a regular write context instead of a transaction
            using var context = _contextFactory.CreateWriteContext();

            try
            {
                // Check if customer already exists with this phone number
                var existingCustomer = await context.Customers
                    .FirstOrDefaultAsync(c => c.Phone == customer.Phone);

                if (existingCustomer != null)
                {
                    _logger.LogWarning("Customer with phone {Phone} already exists", customer.Phone);
                    return existingCustomer;
                }

                // Add new customer to database
                await context.Customers.AddAsync(customer);
                await context.SaveChangesAsync();

                return customer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating customer with phone {Phone}", customer.Phone);
                throw;
            }
        }
    }
}