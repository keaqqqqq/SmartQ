// FNBReservation.Modules.Outlet.Core/DTOs/TableDto.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace FNBReservation.Modules.Outlet.Core.DTOs
{
    public class TableDto
    {
        public Guid Id { get; set; }
        public Guid OutletId { get; set; }
        public string TableNumber { get; set; }
        public int Capacity { get; set; }
        public string Section { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateTableDto
    {
        [Required(ErrorMessage = "Table number is required")]
        [StringLength(20, ErrorMessage = "Table number cannot exceed 20 characters")]
        public string TableNumber { get; set; }

        [Required(ErrorMessage = "Capacity is required")]
        [Range(1, 20, ErrorMessage = "Capacity must be between 1 and 20")]
        public int Capacity { get; set; }

        [Required(ErrorMessage = "Section is required")]
        [StringLength(50, ErrorMessage = "Section cannot exceed 50 characters")]
        public string Section { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class UpdateTableDto
    {
        [StringLength(20, ErrorMessage = "Table number cannot exceed 20 characters")]
        public string? TableNumber { get; set; }

        [Range(1, 20, ErrorMessage = "Capacity must be between 1 and 20")]
        public int? Capacity { get; set; }

        [StringLength(50, ErrorMessage = "Section cannot exceed 50 characters")]
        public string? Section { get; set; }

        public bool? IsActive { get; set; }
    }

    public class SectionDto
    {
        public string Name { get; set; }
        public int TableCount { get; set; }
        public int TotalCapacity { get; set; }
    }
}