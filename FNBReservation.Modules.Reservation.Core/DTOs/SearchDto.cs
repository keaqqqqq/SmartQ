// In FNBReservation.Modules.Reservation.Core/DTOs/SearchDto.cs
namespace FNBReservation.Modules.Reservation.Core.DTOs
{
    public class ReservationSearchResultDto
    {
        public List<ReservationDto> Reservations { get; set; } = new List<ReservationDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public string SearchTerm { get; set; }
        public List<string> AppliedStatuses { get; set; } = new List<string>();
        public List<Guid> AppliedOutletIds { get; set; } = new List<Guid>();
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}