/* <copyright file="UpdateVehicleDTO.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the UpdateVehicleDto class, which serves as a Data Transfer Object (DTO) for updating vehicle information in the Smart Auto Trader application.
</summary>
<remarks>
The UpdateVehicleDto class is used to encapsulate data required for updating an existing vehicle record. It includes validation attributes to ensure that the provided data meets the application's requirements, such as valid ranges for year, price, mileage, and engine size. This DTO is typically used in API requests to update vehicle details.
</remarks>
<dependencies>
- System.ComponentModel.DataAnnotations
</dependencies>
 */

namespace SmartAutoTrader.API.DTOs
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Represents a Data Transfer Object (DTO) for updating vehicle information.
    /// </summary>
    /// <remarks>
    /// This class contains all editable fields for a vehicle, along with appropriate validation attributes. It is designed for use in API requests to update vehicle records.
    /// </remarks>
    public class UpdateVehicleDto
    {
        /// <summary>
        /// Gets or sets the manufacturer/make of the vehicle (e.g., Toyota, BMW).
        /// </summary>
        /// <value>A string representing the vehicle's make.</value>
        /// <example>"Toyota".</example>
        [Required(ErrorMessage = "Make is required.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Make must be between 2 and 50 characters.")]
        public string Make { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model name of the vehicle (e.g., Corolla, X5).
        /// </summary>
        /// <value>A string representing the vehicle's model.</value>
        /// <example>"Corolla".</example>
        [Required(ErrorMessage = "Model is required.")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Model must be between 1 and 50 characters.")]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the year of manufacture for the vehicle.
        /// </summary>
        /// <value>An integer representing the year of manufacture.</value>
        /// <example>2023.</example>
        [Required(ErrorMessage = "Year is required.")]
        [Range(1900, 2050, ErrorMessage = "Year must be between 1900 and 2050.")]
        public int Year { get; set; }

        /// <summary>
        /// Gets or sets the price of the vehicle in the system's default currency.
        /// </summary>
        /// <value>A decimal representing the price of the vehicle.</value>
        /// <example>25000.00.</example>
        [Required(ErrorMessage = "Price is required.")]
        [Range(typeof(decimal), "0.01", "10000000.00", ErrorMessage = "Price must be a positive value and not exceed 10,000,000.")]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        /// <summary>
        /// Gets or sets the mileage of the vehicle in kilometers, if applicable.
        /// </summary>
        /// <value>An integer representing the mileage in kilometers, or null if not provided.</value>
        /// <example>50000.</example>
        [Range(0, 1500000, ErrorMessage = "If provided, mileage must be between 0 and 1,500,000 km.")]
        public int? Mileage { get; set; }

        /// <summary>
        /// Gets or sets the fuel type of the vehicle (e.g., Petrol, Diesel, Electric).
        /// </summary>
        /// <value>A string representing the fuel type of the vehicle.</value>
        /// <example>"Petrol".</example>
        [Required(ErrorMessage = "Fuel type is required.")]
        public string FuelType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the transmission type of the vehicle (e.g., Manual, Automatic).
        /// </summary>
        /// <value>A string representing the transmission type of the vehicle.</value>
        /// <example>"Automatic".</example>
        [Required(ErrorMessage = "Transmission type is required.")]
        public string Transmission { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of the vehicle (e.g., Sedan, SUV, Truck).
        /// </summary>
        /// <value>A string representing the type of the vehicle.</value>
        /// <example>"SUV".</example>
        [Required(ErrorMessage = "Vehicle type is required.")]
        public string VehicleType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the engine size of the vehicle in liters, if applicable.
        /// </summary>
        /// <value>A double representing the engine size in liters, or null if not provided.</value>
        /// <example>2.5.</example>
        [Range(0.1, 15.0, ErrorMessage = "If provided, engine size must be between 0.1 and 15.0 Liters.")]
        public double? EngineSize { get; set; }

        /// <summary>
        /// Gets or sets the horsepower of the vehicle, if applicable.
        /// </summary>
        /// <value>An integer representing the horsepower, or null if not provided.</value>
        /// <example>200.</example>
        [Range(10, 2500, ErrorMessage = "If provided, horsepower must be between 10 and 2500 HP.")]
        public int? HorsePower { get; set; }

        /// <summary>
        /// Gets or sets the country of origin or registration for the vehicle, if applicable.
        /// </summary>
        /// <value>A string representing the country of origin, or null if not provided.</value>
        /// <example>"Japan".</example>
        [StringLength(60, ErrorMessage = "Country name cannot exceed 60 characters.")]
        public string? Country { get; set; }

        /// <summary>
        /// Gets or sets the detailed description of the vehicle.
        /// </summary>
        /// <value>A string containing the description of the vehicle.</value>
        /// <example>"A reliable and fuel-efficient sedan with low mileage.".</example>
        [Required(ErrorMessage = "Description is required.")]
        [StringLength(5000, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 5000 characters.")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of features associated with the vehicle.
        /// </summary>
        /// <value>A list of <see cref="VehicleFeatureDTO"/> objects representing the vehicle's features.</value>
        /// <example>
        /// [
        ///     { "Name": "Sunroof" },
        ///     { "Name": "Leather Seats" }
        /// ].
        /// </example>
        public List<VehicleFeatureDTO> Features { get; set; } = new List<VehicleFeatureDTO>();
    }
}