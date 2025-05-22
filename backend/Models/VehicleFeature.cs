namespace SmartAutoTrader.API.Models;

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

/// <summary>
/// Represents a feature associated with a vehicle.
/// </summary>
/// <remarks>
/// This class tracks individual features of a vehicle, such as "Sunroof" or "Bluetooth", and establishes a relationship with the Vehicle entity.
/// </remarks>
public class VehicleFeature
{
    /// <summary>
    /// Gets or sets the unique identifier for the vehicle feature.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the feature.
    /// </summary>
    [Required]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the vehicle associated with this feature.
    /// </summary>
    public int VehicleId { get; set; }

    /// <summary>
    /// Gets or sets the vehicle associated with this feature.
    /// </summary>
    /// <remarks>
    /// This navigation property establishes a relationship between the feature and the vehicle entity.
    /// </remarks>
    [JsonIgnore]
    public Vehicle? Vehicle { get; set; }
}