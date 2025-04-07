// FNBReservation.Modules.Outlet.Core/Interfaces/IGeolocationService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FNBReservation.Modules.Outlet.Core.DTOs;

namespace FNBReservation.Modules.Outlet.Core.Interfaces
{
    public interface IGeolocationService
    {
        Task<List<OutletDto>> FindNearestOutletsAsync(double latitude, double longitude, int limit = 5);
        double CalculateDistance(double lat1, double lon1, double lat2, double lon2);
    }
}