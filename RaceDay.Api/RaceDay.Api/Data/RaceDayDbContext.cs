using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Unique email (matches UNIQUE on Users.Email)
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // One profile per user
        modelBuilder.Entity<UserProfile>()
            .HasOne(p => p.User)
            .WithOne(u => u.Profile)
            .HasForeignKey<UserProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Event belongs to an organiser
        modelBuilder.Entity<Event>()
            .HasOne(e => e.Organiser)
            .WithMany()
            .HasForeignKey(e => e.OrganiserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Categories belong to an event
        modelBuilder.Entity<Category>()
            .HasOne(c => c.Event)
            .WithMany(e => e.Categories)
            .HasForeignKey(c => c.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Enrolment links participant, event and category
        modelBuilder.Entity<Enrolment>()
            .HasOne(e => e.Participant)
            .WithMany()
            .HasForeignKey(e => e.ParticipantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Enrolment>()
            .HasOne(e => e.Event)
            .WithMany(ev => ev.Enrolments)
            .HasForeignKey(e => e.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Enrolment>()
            .HasOne(e => e.Category)
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // A participant can enter an event only once
        modelBuilder.Entity<Enrolment>()
            .HasIndex(e => new { e.ParticipantId, e.EventId })
            .IsUnique();

        // One result per enrolment
        modelBuilder.Entity<Result>()
            .HasOne(r => r.Enrolment)
            .WithOne(e => e.Result)
            .HasForeignKey<Result>(r => r.EnrolmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Result>()
            .HasIndex(r => r.EnrolmentId)
            .IsUnique();

        // Decimal precision matches DECIMAL(6,2)
        modelBuilder.Entity<Event>()
            .Property(e => e.DistanceKm)
            .HasPrecision(6, 2);

        modelBuilder.Entity<Category>()
            .Property(c => c.DistanceKm)
            .HasPrecision(6, 2);
    }
}