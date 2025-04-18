namespace SmartAutoTrader.API.Models;

using System.ComponentModel.DataAnnotations;

public class UserPreference
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    public User? User { get; set; }

    [Required]
    public string PreferenceType { get; set; } = null!; // e.g., "PriceRange", "VehicleType", "FuelType"

    [Required]
    public string Value { get; set; } = null!; // Store as JSON if needed for complex values

    public float Weight { get; set; } = 1.0f; // Default weight = 1
}