using Microsoft.EntityFrameworkCore;
using my_project.Domain;

namespace my_project.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<InviteLink> InviteLinks => Set<InviteLink>();
    public DbSet<Visitor> Visitors => Set<Visitor>();
    public DbSet<ParticipantBinding> ParticipantBindings => Set<ParticipantBinding>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Trip>(t =>
        {
            t.Property(x => x.Name).HasMaxLength(Trip.MaxNameLength).IsRequired();
            t.Property(x => x.Location).HasMaxLength(Trip.MaxLocationLength).IsRequired();
            t.HasMany(x => x.Participants).WithOne(p => p.Trip).HasForeignKey(p => p.TripId);
            t.HasMany(x => x.InviteLinks).WithOne().HasForeignKey(l => l.TripId);
        });

        b.Entity<Participant>(p =>
        {
            p.Property(x => x.DisplayName).HasMaxLength(DisplayNameRules.MaxLength * 4).IsRequired();
            p.Property(x => x.ComparisonKey).HasMaxLength(DisplayNameRules.MaxLength * 4).IsRequired();
            // Stored as text so the column reads as "In"/"Out"/"Unsure", not an opaque int.
            p.Property(x => x.AttendanceStatus).HasConversion<string>().HasMaxLength(16).IsRequired();
            // INV-3 enforced at the store, so two simultaneous joins cannot both win (EC-1).
            p.HasIndex(x => new { x.TripId, x.ComparisonKey }).IsUnique();
        });

        b.Entity<InviteLink>(l =>
        {
            l.Property(x => x.Token).HasMaxLength(64).IsRequired();
            l.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
            l.HasIndex(x => x.Token).IsUnique();
        });

        b.Entity<Visitor>(v =>
        {
            v.Property(x => x.SessionToken).HasMaxLength(64).IsRequired();
            v.HasIndex(x => x.SessionToken).IsUnique();
            v.HasMany(x => x.Bindings).WithOne().HasForeignKey(x => x.VisitorId);
        });

        b.Entity<ParticipantBinding>(pb =>
        {
            pb.HasOne(x => x.Participant).WithMany().HasForeignKey(x => x.ParticipantId);
        });
    }
}
