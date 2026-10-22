using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Staybnb.Enums;
using Staybnb.Models;
using Staybnb.Services;

namespace Staybnb.Data;

public static class RoleSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var config = services.GetRequiredService<IConfiguration>();
        var activityLog = services.GetRequiredService<IActivityLogService>();

        // Creates the database and applies any pending migrations
        await context.Database.MigrateAsync();

        // 1. Roles
        foreach (var role in RoleNames.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // 2. SuperAdmin (credentials come from appsettings.json)
        var email = config["Staybnb:SuperAdmin:Email"] ?? "superadmin@staybnb.com";
        var password = config["Staybnb:SuperAdmin:Password"] ?? "SuperAdmin@123";
        var firstName = config["Staybnb:SuperAdmin:FirstName"] ?? "Super";
        var lastName = config["Staybnb:SuperAdmin:LastName"] ?? "Admin";

        var superAdmin = await userManager.FindByEmailAsync(email);

        if (superAdmin is null)
        {
            superAdmin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName
            };

            var result = await userManager.CreateAsync(superAdmin, password);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Could not seed the SuperAdmin: {errors}");
            }

            await activityLog.LogAsync(superAdmin.Id, "SuperAdminSeeded", ActivityType.System,
                "SuperAdmin account created by RoleSeeder on startup.");
        }

        // 3. The SuperAdmin holds both roles
        foreach (var role in new[] { RoleNames.SuperAdmin, RoleNames.Admin })
        {
            if (!await userManager.IsInRoleAsync(superAdmin, role))
            {
                await userManager.AddToRoleAsync(superAdmin, role);
            }
        }
    }
}