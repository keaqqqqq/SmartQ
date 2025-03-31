// FNBReservation.Modules.Authentication.Core/DTOs/StaffDto.cs (new file)
using System;
using System.ComponentModel.DataAnnotations;

namespace FNBReservation.Modules.Authentication.Core.DTOs
{
    public class CreateStaffDto
    {
        [Required(ErrorMessage = "Outlet ID is required")]
        public Guid OutletId { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, ErrorMessage = "Username cannot exceed 50 characters")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
            ErrorMessage = "Password must include at least one uppercase letter, one lowercase letter, one number, and one special character")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        [RegularExpression(@"^\+?[0-9\s\-\(\)]+$", ErrorMessage = "Invalid phone number format")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Role is required")]
        [RegularExpression("^(Admin|OutletStaff)$", ErrorMessage = "Role must be either 'Admin' or 'OutletStaff'")]
        public string Role { get; set; } = "OutletStaff";
    }

    public class UpdateStaffDto
    {
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string Email { get; set; }

        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
        public string FullName { get; set; }

        [StringLength(50, ErrorMessage = "Username cannot exceed 50 characters")]
        public string Username { get; set; }

        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
            ErrorMessage = "Password must include at least one uppercase letter, one lowercase letter, one number, and one special character")]
        public string Password { get; set; }

        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        [RegularExpression(@"^\+?[0-9\s\-\(\)]+$", ErrorMessage = "Invalid phone number format")]
        public string Phone { get; set; }

        [RegularExpression("^(Admin|OutletStaff)$", ErrorMessage = "Role must be either 'Admin' or 'OutletStaff'")]
        public string Role { get; set; }

        public bool? IsActive { get; set; }
    }

    public class StaffDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public Guid OutletId { get; set; }
        public string OutletName { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Username { get; set; }
        public string Phone { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}