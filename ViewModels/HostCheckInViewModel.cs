
using System.ComponentModel.DataAnnotations;

namespace Staybnb.ViewModels;

public class HostCheckInViewModel
{
    public int PropertyId { get; set; }

    public string PropertyTitle { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter a title.")]
    [StringLength(200)]
    public string Title { get; set; } = "Guest Check-in";

    [Required(ErrorMessage = "Please enter check-in instructions.")]
    [StringLength(3000)]
    public string Instructions { get; set; } = string.Empty;

    public bool RequireId { get; set; } = true;

    public bool RequirePassport { get; set; }
}
