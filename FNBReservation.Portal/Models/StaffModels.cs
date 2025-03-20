using System;
using System.ComponentModel.DataAnnotations;

namespace FNBReservation.Portal.Models
{
    public class StaffMember
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Name must be between 2 and 100 characters.", MinimumLength = 2)]
        public string Name { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "Username must be between 5 and 50 characters.", MinimumLength = 5)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Phone]
        [StringLength(15, ErrorMessage = "Phone number must be between 8 and 15 characters.", MinimumLength = 8)]
        public string PhoneNumber { get; set; }

        [Required]
        public string Role { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public int OutletId { get; set; }

        public Outlet Outlet { get; set; }
    }

    public class Outlet
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Name must be between 2 and 100 characters.", MinimumLength = 2)]
        public string Name { get; set; }

        [Required]
        [StringLength(255, ErrorMessage = "Address must be between 5 and 255 characters.", MinimumLength = 5)]
        public string Address { get; set; }

        public string Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }

    public class StaffCreateRequest
    {
        [Required]
        [StringLength(100, ErrorMessage = "Name must be between 2 and 100 characters.", MinimumLength = 2)]
        public string Name { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "Username must be between 5 and 50 characters.", MinimumLength = 5)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Phone]
        [StringLength(15, ErrorMessage = "Phone number must be between 8 and 15 characters.", MinimumLength = 8)]
        public string PhoneNumber { get; set; }

        [Required]
        public string Role { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "Password must be between 8 and 50 characters.", MinimumLength = 8)]
        public string Password { get; set; }

        [Required]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }

        [Required]
        public int OutletId { get; set; }
    }

    public class StaffUpdateRequest
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Name must be between 2 and 100 characters.", MinimumLength = 2)]
        public string Name { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "Username must be between 5 and 50 characters.", MinimumLength = 5)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Phone]
        [StringLength(15, ErrorMessage = "Phone number must be between 8 and 15 characters.", MinimumLength = 8)]
        public string PhoneNumber { get; set; }

        [Required]
        public string Role { get; set; }

        [StringLength(50, ErrorMessage = "Password must be between 8 and 50 characters.", MinimumLength = 8)]
        public string Password { get; set; }

        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }
}