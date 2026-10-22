
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Staybnb.Enums;

namespace Staybnb.ViewModels;

public class GuestCheckInViewModel
{
    public int BookingId { get; set; }
    public string PropertyTitle { get; set; } = "";
    public string CheckInTitle { get; set; } = "";
    public List<string> Steps { get; set; } = new();
    public List<string> RequiredDocuments { get; set; } = new();

    public CheckInStatus Status { get; set; }

    public List<GuestCheckInDocumentViewModel> Documents { get; set; } = new();

    [Required(ErrorMessage = "Select a document type.")]
    public string DocumentType { get; set; } = "";

    [Required(ErrorMessage = "Select a document to upload.")]
    public IFormFile? DocumentFile { get; set; }
}

public class GuestCheckInDocumentViewModel
{
    public string DocumentType { get; set; } = "";
    public DocumentStatus Status { get; set; }
    public string? HostRemarks { get; set; }
}
