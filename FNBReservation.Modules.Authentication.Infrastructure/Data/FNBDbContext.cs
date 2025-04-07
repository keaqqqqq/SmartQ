using Microsoft.EntityFrameworkCore;
using FNBReservation.Modules.Authentication.Core.Entities;

namespace FNBReservation.Modules.Authentication.Infrastructure.Data
{
    public class FNBDbContext : DbContext
    {
        public FNBDbContext(DbContextOptions<FNBDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure the User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.UserId).IsUnique();
                entity.HasIndex(e => e.OutletId); // Add index for OutletId

                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.Role).IsRequired().HasMaxLength(20);
                entity.Property(e => e.UserType).IsRequired().HasMaxLength(20).HasDefaultValue("Admin");
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            // Configure the RefreshToken entity
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Token).IsUnique();

                entity.Property(e => e.Token).IsRequired();
                entity.Property(e => e.ExpiryTime).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.IsRevoked).HasDefaultValue(false);

                // Configure relationship with User
                entity.HasOne(e => e.User)
                    .WithMany(s => s.RefreshTokens)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Seed admin user
            var adminId = Guid.NewGuid();
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = adminId,
                    UserId = "ADMIN001",
                    Username = "admin",
                    Email = "admin@fnbreservation.com",
                    // Note: In a real application, generate these with proper hashing
                    PasswordHash = "AQAAAAEAACcQAAAAEKND4k6EtZZbkwsOVZl8s5WQy59k8/MEP5aqO4vWu2Y5OnUW9DSx9STiUsolFq/llg==", // Password: Admin@123
                    Role = "Admin",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true
                }
            );
        }
    }
}