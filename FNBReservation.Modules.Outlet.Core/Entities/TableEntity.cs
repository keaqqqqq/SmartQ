// FNBReservation.Modules.Outlet.Core/Entities/TableEntity.cs
using System;

namespace FNBReservation.Modules.Outlet.Core.Entities
{
    public class TableEntity
    {
        public Guid Id { get; set; }
        public Guid OutletId { get; set; }
        public OutletEntity Outlet { get; set; }
        public string TableNumber { get; set; }
        public int Capacity { get; set; }
        public string Section { get; set; } // Section name like "Indoor", "Outdoor", "Balcony", etc.
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
    }
}