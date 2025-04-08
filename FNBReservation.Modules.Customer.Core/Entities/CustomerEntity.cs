namespace FNBReservation.Modules.Customer.Core.Entities
{
    public class CustomerEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Status { get; set; } // Active or Banned
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public ICollection<CustomerBanEntity> BanHistory { get; set; } = new List<CustomerBanEntity>();
    }

    public class CustomerBanEntity
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public CustomerEntity Customer { get; set; }
        public string Reason { get; set; }
        public DateTime BannedAt { get; set; }
        public int DurationDays { get; set; } // 0 for permanent
        public bool IsActive { get; set; } // To track if this ban is currently active
        public Guid BannedById { get; set; } // Admin user ID who performed the ban
        public DateTime? RemovedAt { get; set; } // When ban was removed (null if still active)
        public Guid? RemovedById { get; set; } // Admin user ID who removed the ban
    }
}