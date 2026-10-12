using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayContext : DbContext
{
    public RaceDayContext(DbContextOptions<RaceDayContext> options) : base(options) { }

    public DbSet<Organiser> Organisers => Set<Organiser>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---------------- Organisers ----------------
        modelBuilder.Entity<Organiser>(b =>
        {
            b.ToTable("Organisers");
            b.HasKey(o => o.OrganiserId);
            b.Property(o => o.FullName).HasMaxLength(100).IsRequired();
            b.Property(o => o.Email).HasMaxLength(150).IsRequired();
            b.HasIndex(o => o.Email).IsUnique();
            b.Property(o => o.PasswordHash).HasMaxLength(255).IsRequired();
        });

        // ---------------- Users ----------------
        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("Users");
            b.HasKey(u => u.UserId);
            b.Property(u => u.FullName).HasMaxLength(100).IsRequired();
            b.Property(u => u.Email).HasMaxLength(150).IsRequired();
            b.HasIndex(u => u.Email).IsUnique();
            b.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
            b.Property(u => u.Role).HasMaxLength(20).IsRequired().HasDefaultValue("Participant");
            b.Property(u => u.DateCreated).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        // ---------------- Events ----------------
        modelBuilder.Entity<Event>(b =>
        {
            b.ToTable("Events");
            b.HasKey(e => e.EventId);
            b.Property(e => e.EventName).HasMaxLength(150).IsRequired();
            b.Property(e => e.Location).HasMaxLength(200).IsRequired();
            b.Property(e => e.EventType).HasMaxLength(50).IsRequired();
            b.Property(e => e.Distance).HasColumnType("decimal(5,2)");
            b.ToTable(t => t.HasCheckConstraint("CHK_Events_Distance", "[Distance] > 0"));

            b.HasOne(e => e.Organiser)
             .WithMany(o => o.Events)
             .HasForeignKey(e => e.OrganiserId)
             .OnDelete(DeleteBehavior.Restrict); // matches ON DELETE NO ACTION in script
        });

        // ---------------- Categories ----------------
        modelBuilder.Entity<Category>(b =>
        {
            b.ToTable("Categories");
            b.HasKey(c => c.CategoryId);
            b.Property(c => c.CategoryName).HasMaxLength(100).IsRequired();

            b.HasOne(c => c.Event)
             .WithMany(e => e.Categories)
             .HasForeignKey(c => c.EventId)
             .OnDelete(DeleteBehavior.Cascade); // matches ON DELETE CASCADE
        });

        // ---------------- Enrolments ----------------
        modelBuilder.Entity<Enrolment>(b =>
        {
            b.ToTable("Enrolments");
            b.HasKey(en => en.EnrolmentId);
            b.Property(en => en.EnrolmentDate).HasDefaultValueSql("SYSUTCDATETIME()");

            b.HasOne(en => en.User)
             .WithMany(u => u.Enrolments)
             .HasForeignKey(en => en.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(en => en.Event)
             .WithMany(e => e.Enrolments)
             .HasForeignKey(en => en.EventId)
             .OnDelete(DeleteBehavior.Cascade);

            // NOTE (documented deviation): the Part 1 script defines
            // ON DELETE CASCADE on all three Enrolments foreign keys. SQL
            // Server rejects that combination at creation time because it
            // produces two cascade paths into Enrolments from Events
            // (Events -> Enrolments directly, and Events -> Categories ->
            // Enrolments). To keep the script buildable, the
            // Enrolments -> Categories relationship below is NO ACTION
            // instead of CASCADE. End-user behaviour is unchanged: deleting
            // an Event still cascades to its Enrolments (directly) and its
            // Categories (via Categories' own cascade from Events), because
            // the Enrolments rows are removed by the direct Events cascade
            // before the Categories cascade would otherwise conflict.
            b.HasOne(en => en.Category)
             .WithMany(c => c.Enrolments)
             .HasForeignKey(en => en.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------------- Results ----------------
        modelBuilder.Entity<Result>(b =>
        {
            b.ToTable("Results");
            b.HasKey(r => r.ResultId);
            b.ToTable(t => t.HasCheckConstraint("CHK_Results_Position", "[Position] IS NULL OR [Position] > 0"));

            b.HasOne(r => r.Enrolment)
             .WithOne(en => en.Result)
             .HasForeignKey<Result>(r => r.EnrolmentId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(r => r.EnrolmentId).IsUnique(); // enforces the 1;1 Enrolment -> Result relationship
        });
    }
}
