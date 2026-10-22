using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Staybnb.Models;

namespace Staybnb.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<HostProperty> HostProperties => Set<HostProperty>();
    public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();
    public DbSet<Amenity> Amenities => Set<Amenity>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<HostApplication> HostApplications => Set<HostApplication>();
    public DbSet<CheckInProcess> CheckInProcesses => Set<CheckInProcess>();
    public DbSet<GuestCheckIn> GuestCheckIns => Set<GuestCheckIn>();
    public DbSet<GuestDocument> GuestDocuments => Set<GuestDocument>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // required for Identity tables

        // ---------- HostProperty ----------
        builder.Entity<HostProperty>(e =>
        {
            e.HasOne(p => p.Host)
             .WithMany(u => u.HostedProperties)
             .HasForeignKey(p => p.HostId);

            // Many-to-Many: HostProperty <-> Amenity (join table auto-created)
            e.HasMany(p => p.Amenities)
             .WithMany(a => a.Properties);

            // One-to-One: HostProperty -> CheckInProcess
            e.HasOne(p => p.CheckInProcess)
             .WithOne(c => c.Property)
             .HasForeignKey<CheckInProcess>(c => c.PropertyId);

            e.Property(p => p.PropertyType).HasConversion<string>().HasMaxLength(30);
        });

        // ---------- Booking ----------
        builder.Entity<Booking>(e =>
        {
            e.HasOne(b => b.Property)
             .WithMany(p => p.Bookings)
             .HasForeignKey(b => b.PropertyId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(b => b.Guest)
             .WithMany(u => u.Bookings)
             .HasForeignKey(b => b.GuestId);

            // One-to-One: Booking -> Payment
            e.HasOne(b => b.Payment)
             .WithOne(p => p.Booking)
             .HasForeignKey<Payment>(p => p.BookingId);

            // One-to-One: Booking -> GuestCheckIn
            e.HasOne(b => b.GuestCheckIn)
             .WithOne(g => g.Booking)
             .HasForeignKey<GuestCheckIn>(g => g.BookingId);

            e.Property(b => b.Status).HasConversion<string>().HasMaxLength(30);
        });

        // ---------- Check-in ----------
        builder.Entity<GuestCheckIn>(e =>
        {
            e.HasOne(g => g.CheckInProcess)
             .WithMany(c => c.GuestCheckIns)
             .HasForeignKey(g => g.CheckInProcessId)
             .OnDelete(DeleteBehavior.Restrict);

            e.Property(g => g.Status).HasConversion<string>().HasMaxLength(30);
        });

        builder.Entity<GuestDocument>(e =>
        {
            e.HasOne(d => d.GuestCheckIn)
             .WithMany(g => g.Documents)
             .HasForeignKey(d => d.GuestCheckInId);

            e.Property(d => d.Status).HasConversion<string>().HasMaxLength(30);
        });

        // ---------- Messaging (two FKs to the same table) ----------
        builder.Entity<Message>(e =>
        {
            e.HasOne(m => m.Sender).WithMany().HasForeignKey(m => m.SenderId);
            e.HasOne(m => m.Receiver).WithMany().HasForeignKey(m => m.ReceiverId);
        });

        // ---------- Other enum conversions ----------
        builder.Entity<Payment>().Property(p => p.Status).HasConversion<string>().HasMaxLength(30);
        builder.Entity<HostApplication>().Property(a => a.Status).HasConversion<string>().HasMaxLength(30);
        builder.Entity<Notification>().Property(n => n.Type).HasConversion<string>().HasMaxLength(50);
        builder.Entity<ActivityLog>().Property(a => a.ActivityType).HasConversion<string>().HasMaxLength(50);

        // ---------- A user can only wishlist a property once ----------
        builder.Entity<WishlistItem>()
               .HasIndex(w => new { w.UserId, w.PropertyId })
               .IsUnique();

        // ---------- Avoid SQL Server "multiple cascade paths" errors ----------
        // Every FK from one of OUR tables to ApplicationUser becomes Restrict.
        // (Identity's own tables are left alone.)
        var ourNamespace = typeof(HostProperty).Namespace;
        var userForeignKeys = builder.Model.GetEntityTypes()
            .SelectMany(t => t.GetForeignKeys())
            .Where(fk => fk.PrincipalEntityType.ClrType == typeof(ApplicationUser)
                      && fk.DeclaringEntityType.ClrType.Namespace == ourNamespace);

        foreach (var fk in userForeignKeys)
        {
            fk.DeleteBehavior = DeleteBehavior.Restrict;
        }

        // ---------- Seed amenities (created by the migration itself) ----------
        builder.Entity<Amenity>().HasData(
            new Amenity { Id = 1, Name = "Wifi", IconClass = "fa-solid fa-wifi" },
            new Amenity { Id = 2, Name = "Kitchen", IconClass = "fa-solid fa-utensils" },
            new Amenity { Id = 3, Name = "Washer", IconClass = "fa-solid fa-shirt" },
            new Amenity { Id = 4, Name = "Dryer", IconClass = "fa-solid fa-wind" },
            new Amenity { Id = 5, Name = "Air Conditioning", IconClass = "fa-solid fa-snowflake" },
            new Amenity { Id = 6, Name = "Heating", IconClass = "fa-solid fa-temperature-high" },
            new Amenity { Id = 7, Name = "Pool", IconClass = "fa-solid fa-water-ladder" },
            new Amenity { Id = 8, Name = "Free Parking", IconClass = "fa-solid fa-square-parking" },
            new Amenity { Id = 9, Name = "TV", IconClass = "fa-solid fa-tv" },
            new Amenity { Id = 10, Name = "Braai Area", IconClass = "fa-solid fa-fire-burner" }
        );
    }
}