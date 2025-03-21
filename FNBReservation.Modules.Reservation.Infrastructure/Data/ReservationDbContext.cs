using Microsoft.EntityFrameworkCore;
using FNBReservation.Modules.Reservation.Core.Entities;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace FNBReservation.Modules.Reservation.Infrastructure.Data
{
    public class ReservationDbContext : DbContext
    {
        public ReservationDbContext(DbContextOptions<ReservationDbContext> options) : base(options)
        {
        }

        public DbSet<ReservationEntity> Reservations { get; set; }
        public DbSet<ReservationTableAssignment> TableAssignments { get; set; }
        public DbSet<ReservationReminder> Reminders { get; set; }
        public DbSet<ReservationStatusChange> StatusChanges { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Reservation entity
            modelBuilder.Entity<ReservationEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ReservationCode).IsUnique();
                entity.HasIndex(e => e.OutletId);
                entity.HasIndex(e => e.ReservationDate);
                entity.HasIndex(e => e.CustomerPhone);
                entity.HasIndex(e => e.Status);

                entity.Property(e => e.ReservationCode).IsRequired().HasMaxLength(20);
                entity.Property(e => e.CustomerName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CustomerPhone).IsRequired().HasMaxLength(20);
                entity.Property(e => e.CustomerEmail).HasMaxLength(100);
                entity.Property(e => e.PartySize).IsRequired();
                entity.Property(e => e.ReservationDate).IsRequired();
                entity.Property(e => e.Duration).IsRequired();
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.SpecialRequests).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // Configure TableAssignment entity
            modelBuilder.Entity<ReservationTableAssignment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.ReservationId, e.TableId }).IsUnique();

                entity.Property(e => e.TableNumber).IsRequired().HasMaxLength(20);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Configure relationship with Reservation
                entity.HasOne(e => e.Reservation)
                    .WithMany(r => r.TableAssignments)
                    .HasForeignKey(e => e.ReservationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Reminder entity
            modelBuilder.Entity<ReservationReminder>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.ReservationId, e.ReminderType }).IsUnique();
                entity.HasIndex(e => e.ScheduledFor);
                entity.HasIndex(e => e.Status);

                entity.Property(e => e.ReminderType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ScheduledFor).IsRequired();
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Channel).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Content).HasMaxLength(1000);

                // Configure relationship with Reservation
                entity.HasOne(e => e.Reservation)
                    .WithMany(r => r.Reminders)
                    .HasForeignKey(e => e.ReservationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure StatusChange entity
            modelBuilder.Entity<ReservationStatusChange>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ReservationId);
                entity.HasIndex(e => e.ChangedAt);

                entity.Property(e => e.OldStatus).IsRequired().HasMaxLength(20);
                entity.Property(e => e.NewStatus).IsRequired().HasMaxLength(20);
                entity.Property(e => e.ChangedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.Reason).HasMaxLength(500);

                // Configure relationship with Reservation
                entity.HasOne(e => e.Reservation)
                    .WithMany(r => r.StatusChanges)
                    .HasForeignKey(e => e.ReservationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}