using Staybnb.Enums;
using Staybnb.Models;
using Staybnb.ViewModels;

namespace Staybnb.Services;

public static class HostApplicationQueries
{
    public static IQueryable<HostRequestListItem> ToListItems(this IQueryable<HostApplication> query) =>
        query.Select(a => new HostRequestListItem
        {
            ApplicationId = a.Id,
            ApplicantName = a.ApplicationUser.FirstName + " " + a.ApplicationUser.LastName,
            ApplicantEmail = a.ApplicationUser.Email,
            PropertyTitle = a.Property.Title,
            City = a.Property.City,
            PricePerNight = a.Property.PricePerNight,
            ThumbnailUrl = a.Property.Images.OrderBy(i => i.Id).Select(i => i.ImageUrl).FirstOrDefault(),
            Status = a.Status,
            AppliedAt = a.AppliedAt,
            IsAdditional = a.ApplicationUser.HostApplications
                .Any(x => x.AppliedAt < a.AppliedAt && x.Status == ApplicationStatus.Approved)
        });
}