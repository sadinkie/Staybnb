using Staybnb.Models;

namespace Staybnb.ViewModels;

public class TopBarViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string RoleLabel { get; set; } = string.Empty;
    public int UnreadNotifications { get; set; }
    public int UnreadMessages { get; set; }
    public List<Notification> Recent { get; set; } = new();
}