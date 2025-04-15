using FNBReservation.Portal.Models;

namespace FNBReservation.Portal.Services
{
    public interface IPeakHourService
    {
        Task<List<PeakHour>> GetPeakHoursAsync(string outletId);
        Task<PeakHour> GetPeakHourByIdAsync(string outletId, string peakHourId);
        Task<PeakHour> CreatePeakHourAsync(string outletId, PeakHour peakHour);
        Task<PeakHour> UpdatePeakHourAsync(string outletId, string peakHourId, PeakHour peakHour);
        Task<bool> DeletePeakHourAsync(string outletId, string peakHourId);
        Task<List<PeakHour>> GetActivePeakHoursAsync(string outletId, DateTime? date = null);
    }
} 