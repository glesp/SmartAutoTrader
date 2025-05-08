namespace SmartAutoTrader.API.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Text.Json.Serialization;
    using SmartAutoTrader.API.Enums;

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
        [Column(TypeName = "decimal(18, 2)")]
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
}