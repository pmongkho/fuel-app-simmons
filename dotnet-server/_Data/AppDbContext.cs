using dotnet_server.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace dotnet_server._Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<FuelReport> FuelReports => Set<FuelReport>();
    public DbSet<FuelEntry> FuelEntries => Set<FuelEntry>();
    public DbSet<FuelEntryPhoto> FuelEntryPhotos => Set<FuelEntryPhoto>();
    public DbSet<NotificationRecipient> NotificationRecipients => Set<NotificationRecipient>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();

        modelBuilder.Entity<FuelReport>()
            .HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FuelEntry>()
            .HasOne(x => x.EnteredByUser)
            .WithMany()
            .HasForeignKey(x => x.EnteredByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FuelEntry>()
            .HasOne(x => x.VerifiedBySupervisor)
            .WithMany()
            .HasForeignKey(x => x.VerifiedBySupervisorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FuelEntry>()
            .HasOne(x => x.FuelReport)
            .WithMany(x => x.Entries)
            .HasForeignKey(x => x.FuelReportId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FuelEntryPhoto>()
            .HasOne(x => x.FuelEntry)
            .WithMany(x => x.Photos)
            .HasForeignKey(x => x.FuelEntryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
