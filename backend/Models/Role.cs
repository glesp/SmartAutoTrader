using System.ComponentModel.DataAnnotations;

namespace SmartAutoTrader.API.Models;

public class Role
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = null!; // e.g., "Admin", "User"

    // Navigation property for the join table
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}