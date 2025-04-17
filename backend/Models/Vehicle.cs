// <copyright file="Vehicle.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SmartAutoTrader.API.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.Text.Json.Serialization;

    public enum FuelType
    {
        Petrol,
        Diesel,
        Electric,
        Hybrid,
        Plugin,
    }

    public enum TransmissionType
    {
        Manual,
        Automatic,
        SemiAutomatic,
    }

    public enum VehicleType
    {
        Sedan,
        SUV,
        Hatchback,
        Estate,
        Coupe,
        Convertible,
        Pickup,
        Van,
    }

    public enum VehicleStatus
    {
        Available,
        Reserved,
        Sold,
    }

    public class Vehicle
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Make { get; set; } = null!;

        [Required]
        public string Model { get; set; } = null!;

        [Required]
        public int Year { get; set; }

        [Required]
        public decimal Price { get; set; }

        public int Mileage { get; set; }

        public FuelType FuelType { get; set; }

        public TransmissionType Transmission { get; set; }

        public VehicleType VehicleType { get; set; }

        public double EngineSize { get; set; }

        public int HorsePower { get; set; }

        public string? Country { get; set; }

        [Required]
        public string Description { get; set; } = null!;

        public DateTime DateListed { get; set; } = DateTime.Now;

        public VehicleStatus Status { get; set; } = VehicleStatus.Available;

        // Navigation properties
        public ICollection<VehicleImage>? Images { get; set; }

        public ICollection<VehicleFeature>? Features { get; set; }

        [JsonIgnore]
        public IEnumerable<UserFavorite>? FavoritedBy { get; set; }
    }

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
}