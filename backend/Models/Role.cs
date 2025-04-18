namespace SmartAutoTrader.API.Models;

using System.ComponentModel.DataAnnotations;

public class Role
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = null!;

    // Navigation property for the join table
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}