namespace SmartAutoTrader.API.Models;

using System.ComponentModel.DataAnnotations;

public class UserFavorite
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    public User? User { get; set; }

    public int VehicleId { get; set; }

    public Vehicle? Vehicle { get; set; }

    public DateTime DateAdded { get; set; } = DateTime.Now;
}