namespace SmartAutoTrader.API.Models;

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

/// <summary>
/// Represents an image associated with a vehicle.
/// </summary>
/// <remarks>
/// This class tracks individual images of a vehicle, including whether the image is the primary one, and establishes a relationship with the Vehicle entity.
/// </remarks>
public class VehicleImage
{
    /// <summary>
    /// Gets or sets the unique identifier for the vehicle image.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the URL of the image.
    /// </summary>
    [Required]
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this image is the primary image for the vehicle.
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the vehicle associated with this image.
    /// </summary>
    public int VehicleId { get; set; }

    /// <summary>
    /// Gets or sets the vehicle associated with this image.
    /// </summary>
    /// <remarks>
    /// This navigation property establishes a relationship between the image and the vehicle entity.
    /// </remarks>
    [JsonIgnore]
    public Vehicle? Vehicle { get; set; }
}