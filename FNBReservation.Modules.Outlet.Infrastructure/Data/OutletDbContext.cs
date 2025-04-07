using Microsoft.EntityFrameworkCore;
using FNBReservation.Modules.Outlet.Core.Entities;

namespace FNBReservation.Modules.Outlet.Infrastructure.Data
{
    public class OutletDbContext : DbContext
    {
        public OutletDbContext(DbContextOptions<OutletDbContext> options) : base(options)
        {
        }

        public DbSet<OutletEntity> Outlets { get; set; }
        public DbSet<OutletChange> OutletChanges { get; set; }
        public DbSet<PeakHourSetting> PeakHourSettings { get; set; }
        public DbSet<TableEntity> Tables { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Outlet entity
            modelBuilder.Entity<OutletEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.OutletId).IsUnique();
                entity.HasIndex(e => e.Name).IsUnique();

                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.OutletId).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Location).IsRequired().HasMaxLength(255);
                entity.Property(e => e.OperatingHours).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Contact).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.Latitude).IsRequired(false);
                entity.Property(e => e.Longitude).IsRequired(false);

                // New reservation settings
                entity.Property(e => e.ReservationAllocationPercent).HasDefaultValue(30);
                entity.Property(e => e.DefaultDiningDurationMinutes).HasDefaultValue(120);
            });

            // Configure OutletChange entity
            modelBuilder.Entity<OutletChange>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.FieldName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.OldValue).HasMaxLength(500);
                entity.Property(e => e.NewValue).HasMaxLength(500);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.RequestedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.Comments).HasMaxLength(500);

                // Configure relationship with Outlet
                entity.HasOne(e => e.Outlet)
                    .WithMany(s => s.OutletChanges)
                    .HasForeignKey(e => e.OutletId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure PeakHourSetting entity
            modelBuilder.Entity<PeakHourSetting>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.OutletId, e.Name }).IsUnique();
                entity.HasIndex(e => new { e.OutletId, e.IsRamadanSetting });

                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DaysOfWeek).IsRequired().HasMaxLength(20);
                entity.Property(e => e.StartTime).IsRequired();
                entity.Property(e => e.EndTime).IsRequired();
                entity.Property(e => e.ReservationAllocationPercent).IsRequired();
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.IsRamadanSetting).HasDefaultValue(false);
                entity.Property(e => e.RamadanStartDate).IsRequired(false);
                entity.Property(e => e.RamadanEndDate).IsRequired(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Configure relationship with Outlet
                entity.HasOne(e => e.Outlet)
                    .WithMany(s => s.PeakHourSettings)
                    .HasForeignKey(e => e.OutletId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Table entity
            modelBuilder.Entity<TableEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.OutletId, e.TableNumber }).IsUnique();
                entity.HasIndex(e => new { e.OutletId, e.Section });

                entity.Property(e => e.TableNumber).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Capacity).IsRequired();
                entity.Property(e => e.Section).IsRequired().HasMaxLength(50);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Configure relationship with Outlet
                entity.HasOne(e => e.Outlet)
                    .WithMany(s => s.Tables)
                    .HasForeignKey(e => e.OutletId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}