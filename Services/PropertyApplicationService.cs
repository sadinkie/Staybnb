using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Enums;
using Staybnb.Models;
using Staybnb.ViewModels;

namespace Staybnb.Services;

public class ApplicationResult
{
    public bool Succeeded { get; private init; }
    public int ApplicationId { get; private init; }
    public List<string> Errors { get; private init; } = new();

    public static ApplicationResult Success(int applicationId) =>
        new() { Succeeded = true, ApplicationId = applicationId };

    public static ApplicationResult Fail(List<string> errors) =>
        new() { Errors = errors };
}

public interface IPropertyApplicationService
{
    Task<List<string>> ValidateImagesAsync(IEnumerable<IFormFile> images);

    /// <summary>
    /// Creates the property, its images and a Pending HostApplication in ONE database save.
    /// Use isAdditional = true when an existing Host adds another property.
    /// </summary>
    Task<ApplicationResult> SubmitAsync(ApplicationUser applicant, BecomeHostViewModel model, bool isAdditional = false);
}

public class PropertyApplicationService : IPropertyApplicationService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _files;
    private readonly INotificationService _notifications;
    private readonly IActivityLogService _activityLog;

    public PropertyApplicationService(
        ApplicationDbContext context,
        IFileStorageService files,
        INotificationService notifications,
        IActivityLogService activityLog)
    {
        _context = context;
        _files = files;
        _notifications = notifications;
        _activityLog = activityLog;
    }

    public async Task<List<string>> ValidateImagesAsync(IEnumerable<IFormFile> images)
    {
        var errors = new List<string>();
        var list = images.ToList();

        if (list.Count == 0)
        {
            errors.Add("Please add at least one photo of your property. An application cannot be submitted without an image.");
            return errors;
        }

        if (list.Count > UploadRules.MaxImages)
            errors.Add($"You can upload at most {UploadRules.MaxImages} photos.");

        foreach (var file in list)
        {
            var error = await _files.ValidateImageAsync(file);
            if (error is not null) errors.Add(error);
        }

        return errors;
    }

    public async Task<ApplicationResult> SubmitAsync(
        ApplicationUser applicant, BecomeHostViewModel model, bool isAdditional = false)
    {
        // Defence in depth: the rule is enforced here even if a controller forgets to check
        var errors = await ValidateImagesAsync(model.Images);
        if (errors.Count > 0)
            return ApplicationResult.Fail(errors);

        var savedPaths = new List<string>();
        HostApplication application;

        try
        {
            var property = new HostProperty
            {
                Title = model.Title.Trim(),
                Description = model.Description.Trim(),
                PricePerNight = model.PricePerNight,
                CleaningFee = model.CleaningFee,
                Address = model.Address.Trim(),
                City = model.City.Trim(),
                Country = model.Country.Trim(),
                PropertyType = model.PropertyType,
                MaxGuests = model.MaxGuests,
                Bedrooms = model.Bedrooms,
                Beds = model.Beds,
                Bathrooms = model.Bathrooms,
                IsApproved = false,   // an Admin must approve it
                IsActive = false,     // not public until approved
                CreatedAt = DateTime.UtcNow,
                HostId = applicant.Id
            };

            foreach (var file in model.Images)
            {
                var path = await _files.SaveImageAsync(file, "properties");
                savedPaths.Add(path);
                property.Images.Add(new PropertyImage { ImageUrl = path });
            }

            if (model.SelectedAmenityIds.Count > 0)
            {
                var amenities = await _context.Amenities
                    .Where(a => model.SelectedAmenityIds.Contains(a.Id))
                    .ToListAsync();

                foreach (var amenity in amenities)
                    property.Amenities.Add(amenity);
            }

            application = new HostApplication
            {
                ApplicationUserId = applicant.Id,
                Property = property,
                Status = ApplicationStatus.Pending,
                AppliedAt = DateTime.UtcNow
            };

            // Property, images, amenities and application are saved together in ONE transaction
            _context.HostApplications.Add(application);
            await _context.SaveChangesAsync();
        }
        catch
        {
            // Nothing reached the database, so do not leave orphaned files on disk
            foreach (var path in savedPaths)
                _files.Delete(path);

            throw;
        }

        await AnnounceAsync(applicant, model, application.Id, isAdditional);

        return ApplicationResult.Success(application.Id);
    }

    private async Task AnnounceAsync(ApplicationUser applicant, BecomeHostViewModel model, int applicationId, bool isAdditional)
    {
        var title = model.Title.Trim();

        if (isAdditional)
        {
            await _notifications.NotifyRoleAsync(
                RoleNames.Admin,
                "Additional Property Application",
                $"{applicant.FullName} submitted '{title}' for review.",
                NotificationType.AdditionalPropertyApplication,
                "/Admin/HostRequests");

            await _notifications.CreateAsync(
                applicant.Id,
                "Additional Property Application Submitted",
                $"Your additional property application has been received and is under review. \u2022 {title}",
                NotificationType.AdditionalPropertyApplication,
                "/Host/Properties");
        }
        else
        {
            await _notifications.NotifyRoleAsync(
                RoleNames.Admin,
                "New Host Application",
                $"{applicant.FullName} applied to become a Host with '{title}'.",
                NotificationType.HostApplication,
                "/Admin/HostRequests");

            await _notifications.CreateAsync(
                applicant.Id,
                "Host Application Submitted",
                $"Your application for '{title}' has been received and is under review.",
                NotificationType.HostApplication,
                "/BecomeHost/Status");
        }

        await _activityLog.LogAsync(
            applicant.Id,
            isAdditional ? "AdditionalPropertyApplicationSubmitted" : "HostApplicationSubmitted",
            ActivityType.HostApplication,
            $"'{title}' in {model.City.Trim()} (application #{applicationId}).");
    }
}