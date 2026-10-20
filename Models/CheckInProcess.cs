using System.ComponentModel.DataAnnotations;

namespace Staybnb.Models;

public class CheckInProcess
{
    public int Id { get; set; }

    public int PropertyId { get; set; }
    public HostProperty Property { get; set; } = null!;

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>JSON array of the steps / rules the guest must complete.</summary>
    public string StepsJson { get; set; } = "[]";

    /// <summary>Comma-separated document types required, e.g. "ID,Passport".</summary>
    [StringLength(200)]
    public string RequiredDocuments { get; set; } = "ID,Passport";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<GuestCheckIn> GuestCheckIns { get; set; } = new List<GuestCheckIn>();
}