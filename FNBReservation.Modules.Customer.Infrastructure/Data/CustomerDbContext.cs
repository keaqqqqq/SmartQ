using System;
using Microsoft.EntityFrameworkCore;
using FNBReservation.Modules.Customer.Core.Entities;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace FNBReservation.Modules.Customer.Infrastructure.Data
{
    public class CustomerDbContext : DbContext
    {
        public CustomerDbContext(DbContextOptions<CustomerDbContext> options) : base(options)
        {
        }

        public DbSet<CustomerEntity> Customers { get; set; }
        public DbSet<CustomerBanEntity> CustomerBans { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Customer entity
            modelBuilder.Entity<CustomerEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Phone).IsUnique();
                entity.HasIndex(e => e.Email);
                entity.HasIndex(e => e.Status);

                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Phone).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Active");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // Configure CustomerBan entity
            modelBuilder.Entity<CustomerBanEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.CustomerId, e.IsActive });
                entity.HasIndex(e => e.BannedAt);

                entity.Property(e => e.Reason).IsRequired().HasMaxLength(500);
                entity.Property(e => e.BannedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.DurationDays).IsRequired();
                entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
                entity.Property(e => e.BannedById).IsRequired();
                entity.Property(e => e.RemovedAt).IsRequired(false);
                entity.Property(e => e.RemovedById).IsRequired(false);

                // Configure relationship with Customer
                entity.HasOne(e => e.Customer)
                    .WithMany(c => c.BanHistory)
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}