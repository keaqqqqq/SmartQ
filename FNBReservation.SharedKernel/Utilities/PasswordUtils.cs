using System;
using System.Security.Cryptography;
using System.Text;

namespace FNBReservation.Shared.Utilities
{
    public static class PasswordUtils
    {
        /// <summary>
        /// Utility to generate password hash and salt for initial admin user seeding
        /// </summary>
        public static (string passwordHash, string passwordSalt) CreatePasswordHash(string password)
        {
            using (var hmac = new HMACSHA512())
            {
                string passwordSalt = Convert.ToBase64String(hmac.Key);
                byte[] passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                return (Convert.ToBase64String(passwordHash), passwordSalt);
            }
        }

        /// <summary>
        /// Command-line utility to generate hash and salt for a password
        /// Can be used in development to create seed data
        /// </summary>
        public static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                var (hash, salt) = CreatePasswordHash(args[0]);
                Console.WriteLine($"Password: {args[0]}");
                Console.WriteLine($"Hash: {hash}");
                Console.WriteLine($"Salt: {salt}");
            }
            else
            {
                Console.WriteLine("Please provide a password as an argument");
            }
        }
    }
}