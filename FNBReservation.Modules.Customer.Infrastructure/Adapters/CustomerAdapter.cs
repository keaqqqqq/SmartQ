using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Customer.Core.Interfaces;

namespace FNBReservation.Modules.Reservation.Infrastructure.Adapters
{
    public class CustomerAdapter : ICustomerAdapter
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ILogger<CustomerAdapter> _logger;

        public CustomerAdapter(
            ICustomerRepository customerRepository,
            ILogger<CustomerAdapter> logger)
        {
            _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Guid> GetOrCreateCustomerAsync(string name, string phone, string email)
        {
            try
            {
                // Check if customer exists by phone number
                var existingCustomer = await _customerRepository.GetByPhoneAsync(phone);
                if (existingCustomer != null)
                {
                    _logger.LogInformation("Found existing customer with phone {Phone}: {CustomerId}", phone, existingCustomer.Id);

                    // Update name/email if different
                    bool needsUpdate = false;

                    if (!string.IsNullOrEmpty(name) && name != existingCustomer.Name)
                    {
                        existingCustomer.Name = name;
                        needsUpdate = true;
                    }

                    if (!string.IsNullOrEmpty(email) && email != existingCustomer.Email)
                    {
                        existingCustomer.Email = email;
                        needsUpdate = true;
                    }

                    if (needsUpdate)
                    {
                        existingCustomer.UpdatedAt = DateTime.UtcNow;
                        await _customerRepository.UpdateAsync(existingCustomer);
                        _logger.LogInformation("Updated customer details for {CustomerId}", existingCustomer.Id);
                    }

                    return existingCustomer.Id;
                }

                // Create new customer if not exists
                var newCustomer = new FNBReservation.Modules.Customer.Core.Entities.CustomerEntity
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Phone = phone,
                    Email = email,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _customerRepository.CreateAsync(newCustomer);
                _logger.LogInformation("Created new customer with ID {CustomerId}", newCustomer.Id);

                return newCustomer.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting or creating customer with phone {Phone}", phone);
                throw;
            }
        }
    }
}