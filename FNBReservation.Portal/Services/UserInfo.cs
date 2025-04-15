using System.Collections.Generic;

namespace FNBReservation.Portal.Services
{
    public class UserInfo
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new List<string>();
    }
} 