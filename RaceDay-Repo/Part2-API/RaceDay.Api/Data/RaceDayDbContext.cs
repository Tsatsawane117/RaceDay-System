using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users", t => t.HasCheckConstraint("CK_Users_Role", "[Role] IN ('Organiser', 'Participant')"));
            e.HasKey(x => x.UserID);
            e.Property(x => x.FullName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(150).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            e.Property(x => x.Role).HasColumnType("varchar(20)").IsRequired().HasDefaultValue("Participant");
            e.Property(x => x.ContactNumber).HasColumnType("varchar(20)");
            e.Property(x => x.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
        });

        modelBuilder.Entity<Event>(e =>
        {
            e.ToTable("Events", t =>
            {
                t.HasCheckConstraint("CK_Events_Status", "[Status] IN ('Planned', 'Open', 'Closed', 'Completed', 'Cancelled')");
                t.HasCheckConstraint("CK_Events_EventType", "[EventType] IN ('Run', 'Walk', 'Cycle')");
            });
            e.HasKey(x => x.EventID);
            e.Property(x => x.EventName).HasMaxLength(150).IsRequired();
            e.Property(x => x.EventDate).HasColumnType("date");
            e.Property(x => x.Location).HasMaxLength(150).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.DistanceKM).HasPrecision(5, 2);
            e.Property(x => x.EventType).HasColumnType("varchar(10)").IsRequired().HasDefaultValue("Run");
            e.Property(x => x.Status).HasColumnType("varchar(20)").IsRequired().HasDefaultValue("Planned");
            e.HasOne(x => x.Organiser).WithMany(u => u.OrganisedEvents)
                .HasForeignKey(x => x.OrganiserID).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.HasKey(x => x.CategoryID);
            e.Property(x => x.CategoryName).HasMaxLength(100).IsRequired();
            e.Property(x => x.DistanceKM).HasPrecision(5, 2);
            e.Property(x => x.MaxParticipants).HasDefaultValue(100);
            e.Property(x => x.EntryFee).HasPrecision(8, 2).HasDefaultValue(0m);
            e.HasOne(x => x.Event).WithMany(ev => ev.Categories)
                .HasForeignKey(x => x.EventID).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Enrolment>(e =>
        {
            e.ToTable("Enrolments", t => t.HasCheckConstraint("CK_Enrolments_Status", "[Status] IN ('Pending', 'Confirmed', 'Cancelled')"));
            e.HasKey(x => x.EnrolmentID);
            e.Property(x => x.EnrolmentDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            e.Property(x => x.BibNumber).HasColumnType("varchar(10)");
            e.Property(x => x.Status).HasColumnType("varchar(20)").IsRequired().HasDefaultValue("Pending");
            e.HasIndex(x => new { x.ParticipantID, x.CategoryID }).IsUnique();
            e.HasOne(x => x.Participant).WithMany(u => u.Enrolments)
                .HasForeignKey(x => x.ParticipantID).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Category).WithMany(c => c.Enrolments)
                .HasForeignKey(x => x.CategoryID).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Result>(e =>
        {
            e.ToTable("Results", t => t.HasCheckConstraint("CK_Results_Status", "[Status] IN ('Not Started', 'Finished', 'DNF', 'DSQ')"));
            e.HasKey(x => x.ResultID);
            e.Property(x => x.FinishTime).HasColumnType("time");
            e.Property(x => x.Status).HasColumnType("varchar(20)").IsRequired().HasDefaultValue("Not Started");
            // One Result per Enrolment (1-to-1) -> unique FK.
            e.HasOne(x => x.Enrolment).WithOne(en => en.Result)
                .HasForeignKey<Result>(x => x.EnrolmentID).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Payment>(e =>
        {
            e.ToTable("Payments", t =>
            {
                t.HasCheckConstraint("CK_Payments_Method", "[PaymentMethod] IN ('Card', 'EFT', 'Cash')");
                t.HasCheckConstraint("CK_Payments_Status", "[PaymentStatus] IN ('Pending', 'Success', 'Failed', 'Refunded')");
            });
            e.HasKey(x => x.PaymentID);
            e.Property(x => x.Amount).HasPrecision(8, 2);
            e.Property(x => x.PaymentDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            e.Property(x => x.PaymentMethod).HasColumnType("varchar(30)").IsRequired().HasDefaultValue("Card");
            e.Property(x => x.PaymentStatus).HasColumnType("varchar(20)").IsRequired().HasDefaultValue("Pending");
            e.HasOne(x => x.Enrolment).WithMany(en => en.Payments)
                .HasForeignKey(x => x.EnrolmentID).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
