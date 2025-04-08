using FNBReservation.Modules.Customer.Core.DTOs;
using FNBReservation.Modules.Customer.Core.Interfaces;
using Microsoft.Extensions.Logging;

public class CustomerStatsService : ICustomerStatsService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IReservationAdapter _reservationAdapter;
    private readonly ILogger<CustomerStatsService> _logger;

    public CustomerStatsService(
        ICustomerRepository customerRepository,
        IReservationAdapter reservationAdapter,
        ILogger<CustomerStatsService> logger)
    {
        _customerRepository = customerRepository;
        _reservationAdapter = reservationAdapter;
        _logger = logger;
    }

    public async Task<List<CustomerReservationDto>> GetReservationsByCustomerPhoneAsync(string phone, Guid? outletId = null)
    {
        return await _reservationAdapter.GetReservationsByCustomerPhoneAsync(phone, outletId);
    }

    public async Task<(int TotalReservations, int NoShows, DateTime? FirstVisit, DateTime? LastVisit)> GetCustomerStatsAsync(string phone, Guid? outletId = null)
    {
        _logger.LogInformation("Getting stats for customer phone: {Phone}, outlet: {OutletId}", phone, outletId);

        try
        {
            // Get reservations from the Reservation module
            var reservations = await _reservationAdapter.GetReservationsByCustomerPhoneAsync(phone);

            // Filter by outlet if specified
            if (outletId.HasValue)
            {
                reservations = reservations.Where(r => r.OutletId == outletId.Value).ToList();
            }

            // Calculate stats
            int totalReservations = reservations.Count();
            int noShows = reservations.Count(r => r.Status == "NoShow");

            // Only count COMPLETED reservations for firstVisit and lastVisit
            var completedReservations = reservations
                .Where(r => r.Status == "Completed")
                .OrderBy(r => r.Date)
                .ToList();

            // Set firstVisit and lastVisit to null if no completed reservations
            DateTime? firstVisit = completedReservations.Any() ? completedReservations.First().Date : null;
            DateTime? lastVisit = completedReservations.Any() ? completedReservations.Last().Date : null;

            return (totalReservations, noShows, firstVisit, lastVisit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stats for phone: {Phone}", phone);
            return (0, 0, null, null);
        }
    }
}