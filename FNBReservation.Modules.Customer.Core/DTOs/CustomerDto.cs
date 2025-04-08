using System.ComponentModel.DataAnnotations;

namespace FNBReservation.Modules.Customer.Core.DTOs
{
    public class CustomerDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Status { get; set; } // Active or Banned
        public int TotalReservations { get; set; }
        public int NoShows { get; set; }
        public decimal NoShowRate { get; set; } // Calculated field
        public DateTime? LastVisit { get; set; }
        public DateTime? FirstVisit { get; set; }
        public CustomerBanInfoDto BanInfo { get; set; } // Null if not banned
    }

    public class CustomerDetailDto : CustomerDto
    {
        public List<CustomerReservationDto> ReservationHistory { get; set; } = new List<CustomerReservationDto>();
    }

    public class CustomerReservationDto
    {
        public Guid ReservationId { get; set; }
        public string ReservationCode { get; set; }
        public DateTime Date { get; set; }
        public Guid OutletId { get; set; }
        public string OutletName { get; set; }
        public int PartySize { get; set; }
        public string Status { get; set; }
        public string SpecialRequests { get; set; }
    }

    public class CustomerBanInfoDto
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public string Reason { get; set; }
        public DateTime BannedAt { get; set; }
        public int DurationDays { get; set; } // 0 for permanent ban
        public DateTime? EndsAt { get; set; } // Calculated field, null for permanent ban
        public Guid BannedById { get; set; }
        public string BannedByName { get; set; }
    }

    public class BannedCustomerDto
    {
        public Guid CustomerId { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Reason { get; set; }
        public DateTime BannedAt { get; set; }
        public int DurationDays { get; set; }
        public DateTime? EndsAt { get; set; }
        public string BannedByName { get; set; }
    }

    // Request DTOs
    public class BanCustomerDto
    {
        [Required]
        public Guid CustomerId { get; set; }

        [Required]
        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        public string Reason { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Duration must be a non-negative number")]
        public int DurationDays { get; set; } // 0 means permanent
    }

    // Response for pagination
    public class CustomerListResponseDto
    {
        public List<CustomerDto> Customers { get; set; } = new List<CustomerDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public string SearchTerm { get; set; }
    }
}