namespace SmartAutoTrader.API.DTOs
{
    using System.ComponentModel.DataAnnotations;

    public class CreateVehicleDto
    {
        [Required(ErrorMessage = "Make is required.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Make must be between 2 and 50 characters.")]
        public string Make { get; set; } = string.Empty;

        [Required(ErrorMessage = "Model is required.")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Model must be between 1 and 50 characters.")]
        public string Model { get; set; } = string.Empty;

        [Required(ErrorMessage = "Year is required.")]
        [Range(1900, 2050, ErrorMessage = "Year must be between 1900 and 2050.")]
        public int Year { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(typeof(decimal), "0.01", "10000000.00", ErrorMessage = "Price must be a positive value and not exceed 10,000,000.")]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [Range(0, 1500000, ErrorMessage = "If provided, mileage must be between 0 and 1,500,000 km.")]
        public int? Mileage { get; set; } // Optional

        [Required(ErrorMessage = "Fuel type is required.")]
        public string FuelType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Transmission type is required.")]
        public string Transmission { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vehicle type is required.")]
        public string VehicleType { get; set; } = string.Empty;

        [Range(0.1, 15.0, ErrorMessage = "If provided, engine size must be between 0.1 and 15.0 Liters.")]
        public double? EngineSize { get; set; } // Optional

        [Range(10, 2500, ErrorMessage = "If provided, horsepower must be between 10 and 2500 HP.")]
        public int? HorsePower { get; set; } // Optional

        [StringLength(60, ErrorMessage = "Country name cannot exceed 60 characters.")]
        public string? Country { get; set; } // Optional

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(5000, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 5000 characters.")]
        public string Description { get; set; } = string.Empty;

        public List<VehicleFeatureDTO> VehicleFeatures { get; set; } = new List<VehicleFeatureDTO>();
    }
}