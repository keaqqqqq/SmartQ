// In FNBReservation.Modules.Customer.Core/Interfaces/ICustomerStatsService.cs
using FNBReservation.Modules.Customer.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FNBReservation.Modules.Customer.Core.Interfaces
{
    public interface ICustomerStatsService
    {
        Task<List<CustomerReservationDto>> GetReservationsByCustomerPhoneAsync(string phone, Guid? outletId = null);

        Task<(int TotalReservations, int NoShows, DateTime? FirstVisit, DateTime? LastVisit)> GetCustomerStatsAsync(string phone, Guid? outletId = null);
    }
}