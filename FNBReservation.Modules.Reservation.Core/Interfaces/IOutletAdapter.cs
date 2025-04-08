namespace FNBReservation.Modules.Reservation.Core.Interfaces
{
    public interface IOutletAdapter
    {
        Task<OutletInfo> GetOutletInfoAsync(Guid outletId);
        Task<IEnumerable<TableInfo>> GetTablesAsync(Guid outletId);
        Task<ReservationSettings> GetReservationSettingsAsync(Guid outletId, DateTime dateTime);
    }

    public class OutletInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int DefaultDiningDurationMinutes { get; set; }
        public int MaxAdvanceReservationTime { get; set; }
        public int MinAdvanceReservationTime { get; set; }
        public bool IsActive { get; set; }
    }

    public class TableInfo
    {
        public Guid Id { get; set; }
        public string TableNumber { get; set; }
        public int Capacity { get; set; }
        public string Section { get; set; }
        public bool IsActive { get; set; }
    }

    public class ReservationSettings
    {
        public int ReservationAllocationPercent { get; set; }
        public int DefaultDiningDurationMinutes { get; set; }
        public int TotalCapacity { get; set; }
        public int ReservationCapacity { get; set; }
    }
}