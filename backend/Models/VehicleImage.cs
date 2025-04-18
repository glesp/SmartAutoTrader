namespace SmartAutoTrader.API.Models;

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public class VehicleImage
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string? ImageUrl { get; set; }

    public bool IsPrimary { get; set; }

    public int VehicleId { get; set; }

    [JsonIgnore]
    public Vehicle? Vehicle { get; set; }
}