using Microsoft.EntityFrameworkCore;
using FNBReservation.Modules.Queue.Core.Entities;

namespace FNBReservation.Modules.Queue.Infrastructure.Data
{
    public class QueueDbContext : DbContext
    {
        public QueueDbContext(DbContextOptions<QueueDbContext> options) : base(options)
        {
        }

        public DbSet<QueueEntry> QueueEntries { get; set; }
        public DbSet<QueueStatusChange> QueueStatusChanges { get; set; }
        public DbSet<QueueTableAssignment> QueueTableAssignments { get; set; }
        public DbSet<QueueNotification> QueueNotifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure QueueEntry entity
            modelBuilder.Entity<QueueEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.QueueCode).IsUnique();
                entity.HasIndex(e => e.OutletId);
                entity.HasIndex(e => e.QueuePosition);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CustomerPhone);
                entity.HasIndex(e => new { e.OutletId, e.Status });

                entity.Property(e => e.QueueCode).IsRequired().HasMaxLength(20);
                entity.Property(e => e.CustomerName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CustomerPhone).IsRequired().HasMaxLength(20);
                entity.Property(e => e.PartySize).IsRequired();
                entity.Property(e => e.SpecialRequests).HasMaxLength(500);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.QueuePosition).IsRequired();
                entity.Property(e => e.QueuedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.IsHeld).HasDefaultValue(false);
                entity.Property(e => e.EstimatedWaitMinutes).HasDefaultValue(0);
            });

            // Configure QueueStatusChange entity
            modelBuilder.Entity<QueueStatusChange>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.QueueEntryId);
                entity.HasIndex(e => e.ChangedAt);

                entity.Property(e => e.OldStatus).IsRequired().HasMaxLength(20);
                entity.Property(e => e.NewStatus).IsRequired().HasMaxLength(20);
                entity.Property(e => e.ChangedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.Reason).HasMaxLength(500);

                // Configure relationship with QueueEntry
                entity.HasOne(e => e.QueueEntry)
                    .WithMany(q => q.StatusChanges)
                    .HasForeignKey(e => e.QueueEntryId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure QueueTableAssignment entity
            modelBuilder.Entity<QueueTableAssignment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.QueueEntryId);
                entity.HasIndex(e => e.TableId);
                entity.HasIndex(e => e.Status);

                entity.Property(e => e.TableNumber).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.AssignedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Configure relationship with QueueEntry
                entity.HasOne(e => e.QueueEntry)
                    .WithMany(q => q.TableAssignments)
                    .HasForeignKey(e => e.QueueEntryId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure QueueNotification entity
            modelBuilder.Entity<QueueNotification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.QueueEntryId);
                entity.HasIndex(e => e.Status);

                entity.Property(e => e.NotificationType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Channel).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Content).HasMaxLength(1000);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Configure relationship with QueueEntry
                entity.HasOne(e => e.QueueEntry)
                    .WithMany(q => q.Notifications)
                    .HasForeignKey(e => e.QueueEntryId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}