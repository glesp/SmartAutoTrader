using System.ComponentModel.DataAnnotations;

namespace SmartAutoTrader.API.Models;

public class User
{
    [Key] public int Id { get; set; }

    [Required] public string Username { get; set; }

    [Required] [EmailAddress] public string Email { get; set; }

    [Required] public string PasswordHash { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string PhoneNumber { get; set; }

    public DateTime DateRegistered { get; set; } = DateTime.Now;

    // Navigation properties
    public ICollection<UserFavorite> Favorites { get; set; }
    public ICollection<UserPreference> Preferences { get; set; }
    public ICollection<BrowsingHistory> BrowsingHistory { get; set; }
    public ICollection<Inquiry> SentInquiries { get; set; }
}

public class UserFavorite
{
    [Key] public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; }

    public int VehicleId { get; set; }
    public Vehicle Vehicle { get; set; }

    public DateTime DateAdded { get; set; } = DateTime.Now;
}

public class UserPreference
{
    [Key] public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; }

    [Required] public string PreferenceType { get; set; } // e.g., "PriceRange", "VehicleType", "FuelType"

    [Required] public string Value { get; set; } // Store as JSON if needed for complex values

    public float Weight { get; set; } = 1.0f; // Default weight = 1
}

public class BrowsingHistory
{
    [Key] public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; }

    public int VehicleId { get; set; }
    public Vehicle Vehicle { get; set; }

    public DateTime ViewDate { get; set; } = DateTime.Now;

    public int ViewDurationSeconds { get; set; }
}