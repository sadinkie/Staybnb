using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Enums;
using Staybnb.Models;
using Staybnb.ViewModels;

namespace Staybnb.Services;

public interface IPropertyManagementService
{
    /// <summary>Returns null when the property is not this Host's, or has been rejected.</summary>
    Task<EditPropertyViewModel?> GetForEditAsync(int propertyId, string hostId);

    /// <summary>Returns a list of error messages. An empty list means the update succeeded.</summary>
    Task<List<string>> UpdateAsync(ApplicationUser host, EditPropertyViewModel model);

    Task<OperationResult> ToggleActiveAsync(int propertyId, ApplicationUser host);
}

public class PropertyManagementService : IPropertyManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _files;
    private readonly IActivityLogService _activityLog;

    public PropertyManagementService(
        ApplicationDbContext context,
        IFileStorageService files,
        IActivityLogService activityLog)
    {
        _context = context;
        _files = files;
        _activityLog = activityLog;
    }

    // ------------------------------------------------------------------
    // LOAD FOR EDITING
    // ------------------------------------------------------------------
    public async Task<EditPropertyViewModel?> GetForEditAsync(int propertyId, string hostId)
    {
        // The HostId filter is what stops one Host opening another Host's property
        var property = await _context.HostProperties
            .AsNoTracking()
            .Include(p => p.Images)
            .Include(p => p.Amenities)
            .FirstOrDefaultAsync(p => p.Id == propertyId && p.HostId == hostId);

        if (property is null || await IsRejectedAsync(propertyId))
            return null;

        return new EditPropertyViewModel
        {
            Id = property.Id,
            Title = property.Title,
            Description = property.Description,
            PropertyType = property.PropertyType,
            Address = property.Address,
            City = property.City,
            Country = property.Country,
            PricePerNight = property.PricePerNight,
            CleaningFee = property.CleaningFee,
            MaxGuests = property.MaxGuests,
            Bedrooms = property.Bedrooms,
            Beds = property.Beds,
            Bathrooms = property.Bathrooms,
            SelectedAmenityIds = property.Amenities.Select(a => a.Id).ToList(),
            ExistingImages = property.Images
                .OrderBy(i => i.Id)
                .Select(i => new ExistingImageItem { Id = i.Id, Url = i.ImageUrl })
                .ToList()
        };
    }

    // ------------------------------------------------------------------
    // UPDATE
    // ------------------------------------------------------------------
    public async Task<List<string>> UpdateAsync(ApplicationUser host, EditPropertyViewModel model)
    {
        var errors = new List<string>();

        var property = await _context.HostProperties
            .Include(p => p.Images)
            .Include(p => p.Amenities)
            .FirstOrDefaultAsync(p => p.Id == model.Id && p.HostId == host.Id);

        if (property is null)
        {
            errors.Add("That property could not be found.");
            return errors;
        }

        if (await IsRejectedAsync(property.Id))
        {
            errors.Add("A rejected listing cannot be edited. Please submit a new property instead.");
            return errors;
        }

        // Photo rules: a listing always keeps between 1 and 8 photos
        var removeIds = model.RemoveImageIds.ToHashSet();
        var toRemove = property.Images.Where(i => removeIds.Contains(i.Id)).ToList();
        var totalAfter = property.Images.Count - toRemove.Count + model.Images.Count;

        if (totalAfter < 1)
            errors.Add("A listing must keep at least one photo. Add a new photo before removing the last one.");

        if (totalAfter > UploadRules.MaxImages)
            errors.Add($"A listing can have at most {UploadRules.MaxImages} photos. Remove some photos or add fewer.");

        foreach (var file in model.Images)
        {
            var error = await _files.ValidateImageAsync(file);
            if (error is not null) errors.Add(error);
        }

        if (errors.Count > 0)
            return errors;

        var newPaths = new List<string>();

        try
        {
            property.Title = model.Title.Trim();
            property.Description = model.Description.Trim();
            property.PropertyType = model.PropertyType;
            property.Address = model.Address.Trim();
            property.City = model.City.Trim();
            property.Country = model.Country.Trim();
            property.PricePerNight = model.PricePerNight;
            property.CleaningFee = model.CleaningFee;
            property.MaxGuests = model.MaxGuests;
            property.Bedrooms = model.Bedrooms;
            property.Beds = model.Beds;
            property.Bathrooms = model.Bathrooms;

            // Amenities: replace the whole set with what the Host ticked
            var amenities = await _context.Amenities
                .Where(a => model.SelectedAmenityIds.Contains(a.Id))
                .ToListAsync();

            property.Amenities.Clear();
            foreach (var amenity in amenities)
                property.Amenities.Add(amenity);

            // Photos: remove ticked ones, add new ones
            _context.PropertyImages.RemoveRange(toRemove);

            foreach (var file in model.Images)
            {
                var path = await _files.SaveImageAsync(file, "properties");
                newPaths.Add(path);
                property.Images.Add(new PropertyImage { ImageUrl = path });
            }

            await _context.SaveChangesAsync();
        }
        catch
        {
            // The database save failed, so do not leave the new files orphaned on disk
            foreach (var path in newPaths)
                _files.Delete(path);

            throw;
        }

        // Only now (after a successful save) is it safe to delete the removed files
        foreach (var image in toRemove)
            _files.Delete(image.ImageUrl);

        await _activityLog.LogAsync(host.Id, "PropertyUpdated", ActivityType.PropertyChange,
            $"'{property.Title}' (property #{property.Id}) was updated by {host.Email}.");

        return errors;
    }

    // ------------------------------------------------------------------
    // TOGGLE IsActive
    // ------------------------------------------------------------------
    public async Task<OperationResult> ToggleActiveAsync(int propertyId, ApplicationUser host)
    {
        var property = await _context.HostProperties
            .FirstOrDefaultAsync(p => p.Id == propertyId && p.HostId == host.Id);

        if (property is null)
            return OperationResult.Fail("That property could not be found.");

        // Hosts may switch approved listings on and off, but never bypass Admin approval
        if (!property.IsApproved)
            return OperationResult.Fail("This listing is still waiting for Admin approval, so it cannot be activated yet.");

        property.IsActive = !property.IsActive;
        await _context.SaveChangesAsync();

        await _activityLog.LogAsync(
            host.Id,
            property.IsActive ? "PropertyActivated" : "PropertyDeactivated",
            ActivityType.PropertyChange,
            $"'{property.Title}' (property #{property.Id}) was {(property.IsActive ? "activated" : "deactivated")} by {host.Email}.");

        return OperationResult.Success(property.IsActive
            ? $"'{property.Title}' is now active and visible to guests."
            : $"'{property.Title}' is now inactive and hidden from guests.");
    }

    private Task<bool> IsRejectedAsync(int propertyId) =>
        _context.HostApplications.AnyAsync(a =>
            a.PropertyId == propertyId && a.Status == ApplicationStatus.Rejected);
}