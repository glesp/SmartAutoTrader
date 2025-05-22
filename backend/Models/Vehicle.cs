namespace SmartAutoTrader.API.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Text.Json.Serialization;
    using SmartAutoTrader.API.Enums;

    /// <summary>
    /// Represents a vehicle in the Smart Auto Trader application.
    /// </summary>
    /// <remarks>
    /// This class encapsulates details about a vehicle, including its make, model, year, price, and other specifications. It also includes navigation properties for related entities such as images and features.
    /// </remarks>
    public class Vehicle
    {
        /// <summary>
        /// Gets or sets the unique identifier for the vehicle.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the make of the vehicle.
        /// </summary>
        [Required]
        public string Make { get; set; } = null!;

        /// <summary>
        /// Gets or sets the model of the vehicle.
        /// </summary>
        [Required]
        public string Model { get; set; } = null!;

        /// <summary>
        /// Gets or sets the year of manufacture of the vehicle.
        /// </summary>
        [Required]
        public int Year { get; set; }

        /// <summary>
        /// Gets or sets the price of the vehicle.
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; }

        /// <summary>
        /// Gets or sets the mileage of the vehicle.
        /// </summary>
        public int Mileage { get; set; }

        /// <summary>
        /// Gets or sets the fuel type of the vehicle.
        /// </summary>
        public FuelType FuelType { get; set; }

        /// <summary>
        /// Gets or sets the transmission type of the vehicle.
        /// </summary>
        public TransmissionType Transmission { get; set; }

        /// <summary>
        /// Gets or sets the type of the vehicle.
        /// </summary>
        public VehicleType VehicleType { get; set; }

        /// <summary>
        /// Gets or sets the engine size of the vehicle in liters.
        /// </summary>
        public double EngineSize { get; set; }

        /// <summary>
        /// Gets or sets the horsepower of the vehicle.
        /// </summary>
        public int HorsePower { get; set; }

        /// <summary>
        /// Gets or sets the country of origin of the vehicle.
        /// </summary>
        public string? Country { get; set; }

        /// <summary>
        /// Gets or sets the description of the vehicle.
        /// </summary>
        [Required]
        public string Description { get; set; } = null!;

        /// <summary>
        /// Gets or sets the date when the vehicle was listed.
        /// </summary>
        public DateTime DateListed { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the status of the vehicle.
        /// </summary>
        public VehicleStatus Status { get; set; } = VehicleStatus.Available;

        /// <summary>
        /// Gets or sets the collection of images associated with the vehicle.
        /// </summary>
        public ICollection<VehicleImage>? Images { get; set; }

        /// <summary>
        /// Gets or sets the collection of features associated with the vehicle.
        /// </summary>
        public ICollection<VehicleFeature>? Features { get; set; }

        /// <summary>
        /// Gets or sets the collection of users who have marked this vehicle as a favorite.
        /// </summary>
        [JsonIgnore]
        public IEnumerable<UserFavorite>? FavoritedBy { get; set; }
    }
}