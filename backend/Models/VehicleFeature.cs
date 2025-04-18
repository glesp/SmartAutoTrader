namespace SmartAutoTrader.API.Models;

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public class VehicleFeature
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string? Name { get; set; }

    public int VehicleId { get; set; }

    [JsonIgnore]
    public Vehicle? Vehicle { get; set; }
}