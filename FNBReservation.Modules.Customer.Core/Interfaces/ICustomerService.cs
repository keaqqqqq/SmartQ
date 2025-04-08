using FNBReservation.Modules.Customer.Core.DTOs;
using FNBReservation.Modules.Customer.Core.Entities;

namespace FNBReservation.Modules.Customer.Core.Interfaces
{
    public interface ICustomerService
    {
        Task<CustomerListResponseDto> GetAllCustomersAsync(string searchTerm = null, int page = 1, int pageSize = 20, Guid? outletId = null);
        Task<CustomerListResponseDto> GetActiveCustomersAsync(string searchTerm = null, int page = 1, int pageSize = 20, Guid? outletId = null);
        Task<List<BannedCustomerDto>> GetBannedCustomersAsync(Guid? outletId = null);
        Task<CustomerDetailDto> GetCustomerByIdAsync(Guid id);
        Task<CustomerDto> BanCustomerAsync(BanCustomerDto banRequest, Guid adminId);
        Task<CustomerDto> RemoveBanAsync(Guid customerId, Guid adminId);
        Task<List<CustomerReservationDto>> GetCustomerReservationsAsync(Guid customerId, Guid? outletId = null);
    }

    public interface ICustomerRepository
    {
        Task<(List<Guid> CustomerIds, int TotalCount)> SearchCustomersAsync(
            string searchTerm,
            string status = null,
            Guid? outletId = null,
            int page = 1,
            int pageSize = 20);

        Task<CustomerEntity> GetByIdAsync(Guid id);
        Task<CustomerEntity> GetByPhoneAsync(string phone);
        Task<List<CustomerEntity>> GetByIdsAsync(List<Guid> ids);
        Task<CustomerEntity> UpdateAsync(CustomerEntity customer);
        Task<CustomerBanEntity> AddBanAsync(CustomerBanEntity ban);
        Task<CustomerBanEntity> GetActiveBanAsync(Guid customerId);
        Task<List<CustomerBanEntity>> GetBanHistoryAsync(Guid customerId);
        Task<List<CustomerBanEntity>> GetActiveBansAsync(Guid? outletId = null);
        Task<CustomerEntity> CreateAsync(CustomerEntity customer);

    }

    // Interface for adapter to get data from Reservation module
    public interface IReservationAdapter
    {
        Task<List<CustomerReservationDto>> GetReservationsByCustomerPhoneAsync(string phone, Guid? outletId = null);
        Task<(int TotalReservations, int NoShows, DateTime? FirstVisit, DateTime? LastVisit)> GetCustomerStatsAsync(string phone, Guid? outletId = null);
    }
}