
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Staybnb.Enums;
using Staybnb.Models;
using Staybnb.Services;

namespace Staybnb.Data;

/// <summary>
/// Creates demo hosts and properties.
/// Also replaces old demo placeholder images
/// with online house photographs.
/// </summary>
public static class DemoDataSeeder
{
    private const string DemoHostPassword = "Host@12345";

    private record DemoHost(
        string FirstName,
        string LastName,
        string Email);

    private record DemoProperty(
        string HostEmail,
        string Title,
        string Description,
        decimal PricePerNight,
        decimal CleaningFee,
        string Address,
        string City,
        PropertyType Type,
        int MaxGuests,
        int Bedrooms,
        int Beds,
        int Bathrooms,
        int[] AmenityIds);

    private static readonly DemoHost[] Hosts =
    {
        new("Elsie", "Masuku", "elsie.masuku@staybnb.demo"),
        new("Neroshen", "Govender", "neroshen.govender@staybnb.demo"),
        new("Muzi", "Mango", "muzi.mango@staybnb.demo")
    };

    // Demonstration photographs from Unsplash.
    // They are not verified photographs of these properties.
    private static readonly string[] HousePhotos =
    {
        "https://images.unsplash.com/photo-1600596542815-ffad4c1539a9",
        "https://images.unsplash.com/photo-1600607687920-4e2a09cf159d",
        "https://images.unsplash.com/photo-1600566753190-17f0baa2a6c3",
        "https://images.unsplash.com/photo-1600585154340-be6161a56a0c",
        "https://images.unsplash.com/photo-1600047509807-ba8f99d2cdde"
    };

    // Amenity IDs:
    // 1 Wifi, 2 Kitchen, 3 Washer, 4 Dryer,
    // 5 Air Conditioning, 6 Heating, 7 Pool,
    // 8 Free Parking, 9 TV, 10 Braai Area.
    private static readonly DemoProperty[] Properties =
    {
        new("elsie.masuku@staybnb.demo", "Lovely City Apartment",
            "A bright, modern apartment a short walk from the promenade, with a balcony overlooking the sea.",
            1780m, 250m, "12 Lagoon Drive", "Umhlanga",
            PropertyType.Apartment, 5, 2, 4, 2,
            new[] { 1, 2, 3, 5, 8, 9 }),

        new("elsie.masuku@staybnb.demo", "Cozy Forest Cabin",
            "Escape to a cabin tucked among towering trees and birdsong, with a deck made for slow mornings.",
            3100m, 350m, "8 Forest Road", "Durban",
            PropertyType.Cabin, 8, 4, 5, 4,
            new[] { 1, 2, 3, 4, 5, 6, 10 }),

        new("elsie.masuku@staybnb.demo", "Cape Town Studio Loft",
            "A stylish open-plan loft in the city bowl, close to cafes, galleries and the waterfront.",
            950m, 150m, "44 Loop Street", "Cape Town",
            PropertyType.Studio, 2, 1, 1, 1,
            new[] { 1, 2, 9 }),

        new("neroshen.govender@staybnb.demo", "Luxury Apartment",
            "Top-floor luxury living with floor-to-ceiling windows and secure underground parking.",
            1320m, 200m, "3 Marine Parade", "Durban",
            PropertyType.Apartment, 4, 2, 2, 2,
            new[] { 1, 2, 3, 5, 8, 9 }),

        new("neroshen.govender@staybnb.demo", "Exceptional Villa",
            "A private villa with a heated pool and braai area, ideal for family holidays.",
            2450m, 400m, "21 Lowveld Close", "Nelspruit",
            PropertyType.Villa, 10, 5, 6, 4,
            new[] { 1, 2, 3, 4, 5, 7, 8, 10 }),

        new("neroshen.govender@staybnb.demo", "Winelands Cottage",
            "A charming cottage between the vineyards, with a fireplace and a garden to match.",
            1650m, 250m, "5 Vineyard Lane", "Stellenbosch",
            PropertyType.Cottage, 4, 2, 3, 1,
            new[] { 1, 2, 6, 8, 10 }),

        new("muzi.mango@staybnb.demo", "Cozy Holiday Cabin",
            "A warm timber cabin near the waterfalls, perfect for a quiet weekend away.",
            1230m, 200m, "17 Panorama Route", "Graskop",
            PropertyType.Cabin, 4, 2, 2, 1,
            new[] { 1, 2, 6, 10 }),

        new("muzi.mango@staybnb.demo", "Beautiful Beachfront Home",
            "Wake up to the sound of waves in this airy home, just steps from the sand.",
            2100m, 300m, "2 Beach Road", "Umhlanga",
            PropertyType.House, 6, 3, 4, 3,
            new[] { 1, 2, 3, 5, 7, 8, 9 }),

        new("muzi.mango@staybnb.demo", "Sandton Skyline Suite",
            "A sleek business-friendly suite with fast wifi, a work desk and views of the skyline.",
            1450m, 180m, "90 Rivonia Road", "Johannesburg",
            PropertyType.Apartment, 3, 1, 2, 1,
            new[] { 1, 2, 5, 8, 9 }),

        new("muzi.mango@staybnb.demo", "Drakensberg Mountain Retreat",
            "A peaceful retreat at the foot of the mountains, with trails starting at the door.",
            1890m, 300m, "6 Berg View", "Drakensberg",
            PropertyType.Cabin, 6, 3, 4, 2,
            new[] { 1, 2, 4, 6, 10 })
    };

    public static async Task SeedAsync(
        IServiceProvider services)
    {
        var config =
            services.GetRequiredService<IConfiguration>();

        if (!config.GetValue<bool>("Staybnb:SeedDemoData"))
            return;

        var context =
            services.GetRequiredService<ApplicationDbContext>();

        var userManager =
            services.GetRequiredService<
                UserManager<ApplicationUser>>();

        var activityLog =
            services.GetRequiredService<IActivityLogService>();

        // Existing properties must not be deleted or reseeded.
        if (await context.HostProperties.AnyAsync())
        {
            await UpdateDemoImagesAsync(context);
            return;
        }

        var superAdminEmail =
            config["Staybnb:SuperAdmin:Email"]
            ?? "superadmin@staybnb.com";

        var superAdmin =
            await userManager.FindByEmailAsync(superAdminEmail);

        // Create demo hosts.
        var hostsByEmail =
            new Dictionary<string, ApplicationUser>();

        foreach (var demo in Hosts)
        {
            var user =
                await userManager.FindByEmailAsync(demo.Email);

            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = demo.Email,
                    Email = demo.Email,
                    EmailConfirmed = true,
                    FirstName = demo.FirstName,
                    LastName = demo.LastName
                };

                var result =
                    await userManager.CreateAsync(
                        user,
                        DemoHostPassword);

                if (!result.Succeeded)
                {
                    var errors = string.Join(
                        "; ",
                        result.Errors.Select(e => e.Description));

                    throw new InvalidOperationException(
                        $"Could not seed demo host {demo.Email}: {errors}");
                }

                await userManager.AddToRoleAsync(
                    user,
                    RoleNames.Host);
            }

            hostsByEmail[demo.Email] = user;
        }

        var amenities =
            await context.Amenities.ToListAsync();

        var imageNumber = 0;
        var daysAgo = Properties.Length * 3;

        foreach (var demo in Properties)
        {
            var host = hostsByEmail[demo.HostEmail];

            var created =
                DateTime.UtcNow.AddDays(-daysAgo);

            daysAgo -= 3;

            var property = new HostProperty
            {
                Title = demo.Title,
                Description = demo.Description,
                PricePerNight = demo.PricePerNight,
                CleaningFee = demo.CleaningFee,
                Address = demo.Address,
                City = demo.City,
                Country = "South Africa",
                PropertyType = demo.Type,
                MaxGuests = demo.MaxGuests,
                Bedrooms = demo.Bedrooms,
                Beds = demo.Beds,
                Bathrooms = demo.Bathrooms,
                IsApproved = true,
                IsActive = true,
                CreatedAt = created,
                HostId = host.Id
            };

            // Use online photographs instead of placeholders.
            for (var i = 0; i < 3; i++)
            {
                property.Images.Add(new PropertyImage
                {
                    ImageUrl = HousePhotos[
                        imageNumber++ % HousePhotos.Length]
                });
            }

            foreach (var amenity in amenities.Where(
                a => demo.AmenityIds.Contains(a.Id)))
            {
                property.Amenities.Add(amenity);
            }

            property.CheckInProcess = new CheckInProcess
            {
                Title = "Standard check-in",
                StepsJson = JsonSerializer.Serialize(new[]
                {
                    "Confirm your arrival details",
                    "Upload your ID or passport",
                    "Read and accept the house rules"
                }),
                RequiredDocuments = "ID,Passport",
                CreatedAt = created
            };

            context.HostProperties.Add(property);

            context.HostApplications.Add(new HostApplication
            {
                ApplicationUserId = host.Id,
                Property = property,
                Status = ApplicationStatus.Approved,
                AppliedAt = created,
                ReviewedAt = created.AddHours(4),
                ReviewedById = superAdmin?.Id,
                AdminNotes =
                    "Demo data approved when the database was seeded."
            });
        }

        await context.SaveChangesAsync();

        await activityLog.LogAsync(
            superAdmin?.Id,
            "DemoDataSeeded",
            ActivityType.System,
            $"Seeded {Hosts.Length} demo hosts and {Properties.Length} approved properties.");
    }

    // Update images for previously seeded demo properties.
    // Only properties belonging to known demo hosts are checked.
    private static async Task UpdateDemoImagesAsync(
        ApplicationDbContext context)
    {
        var demoEmails = Hosts
            .Select(h => h.Email)
            .ToArray();

        var demoProperties = await context.HostProperties
            .Include(p => p.Host)
            .Include(p => p.Images)
            .Where(p => demoEmails.Contains(p.Host.Email!))
            .ToListAsync();

        var hasChanges = false;

        foreach (var property in demoProperties)
        {
            // Match the existing property to its demo definition.
            var demoIndex = Array.FindIndex(
                Properties,
                p => p.Title == property.Title);

            if (demoIndex < 0)
                continue;

            // Never overwrite images already supplied by a host
            // or images that are already external photographs.
            var existingImages = property.Images
                .OrderBy(i => i.Id)
                .ToList();

            // The original seeder created exactly three
            // placeholders per property.
            // Replace only those known original placeholder URLs.
            for (var i = 0; i < existingImages.Count; i++)
            {
                var image = existingImages[i];

                var oldImageNumber =
                    demoIndex * 3 + i;

                var originalPlaceholder =
                    PlaceholderImage.UrlFor(oldImageNumber);

                if (image.ImageUrl != originalPlaceholder)
                    continue;

                image.ImageUrl = HousePhotos[
                    oldImageNumber % HousePhotos.Length];

                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            await context.SaveChangesAsync();
        }
    }
}
