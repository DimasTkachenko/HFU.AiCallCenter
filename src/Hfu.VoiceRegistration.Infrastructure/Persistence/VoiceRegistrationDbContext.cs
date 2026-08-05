using Hfu.VoiceRegistration.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hfu.VoiceRegistration.Infrastructure.Persistence;

public sealed class VoiceRegistrationDbContext : DbContext
{
    public VoiceRegistrationDbContext(DbContextOptions<VoiceRegistrationDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserRegistrationEntity> UserRegistrations => Set<UserRegistrationEntity>();

    public DbSet<SessionRecordEntity> SessionAuditLogs => Set<SessionRecordEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserRegistrationEntity>(b =>
        {
            b.ToTable("UserRegistrations");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.SessionId).IsUnique();
            b.HasIndex(x => x.DemoRegistrationId);
            b.HasIndex(x => x.PhoneNumber);
            b.HasIndex(x => x.CompletedAt);

            b.Property(x => x.DemoRegistrationId).HasMaxLength(64).IsRequired();
            b.Property(x => x.FirstName).HasMaxLength(128).IsRequired();
            b.Property(x => x.LastName).HasMaxLength(128).IsRequired();
            b.Property(x => x.Patronymic).HasMaxLength(128);
            b.Property(x => x.PhoneNumber).HasMaxLength(32).IsRequired();
            b.Property(x => x.Email).HasMaxLength(256);
            b.Property(x => x.CurrentRegion).HasMaxLength(128).IsRequired();
            b.Property(x => x.CurrentRegionReferenceId).HasMaxLength(64);
            b.Property(x => x.CurrentCity).HasMaxLength(128).IsRequired();
            b.Property(x => x.ActualAddress).HasMaxLength(512);
            b.Property(x => x.UserCategory).HasMaxLength(64).IsRequired();
            b.Property(x => x.RegionBeforeWar).HasMaxLength(128);
            b.Property(x => x.RegionBeforeWarReferenceId).HasMaxLength(64);
        });

        modelBuilder.Entity<SessionRecordEntity>(b =>
        {
            b.ToTable("SessionAuditLogs");
            b.HasKey(x => x.SessionId);
            b.HasIndex(x => x.CreatedAt);
            b.HasIndex(x => x.Status);

            b.Property(x => x.Status).HasMaxLength(32).IsRequired();
            b.Property(x => x.DraftJson).HasColumnType("text").IsRequired();
            b.Property(x => x.EventsJson).HasColumnType("text").IsRequired();
            b.Property(x => x.DemoRegistrationId).HasMaxLength(64);
        });
    }
}
