/* <copyright file="RecommendationParametersDTO.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the RecommendationParametersDto class, which serves as a Data Transfer Object (DTO) for encapsulating parameters used in generating vehicle recommendations in the Smart Auto Trader application.
</summary>
<remarks>
The RecommendationParametersDto class is used to encapsulate user preferences and constraints for vehicle recommendations. It includes properties for filtering by price, year, mileage, make, vehicle type, fuel type, features, and other attributes. The class also supports specifying rejected or negated preferences, such as makes or features to exclude. This DTO is typically used in API requests to provide input for recommendation algorithms.
</remarks>
<dependencies>
- System.Collections.Generic
</dependencies>
 */

namespace SmartAutoTrader.API.DTOs
{
    /// <summary>
    /// Represents a Data Transfer Object (DTO) for encapsulating parameters used in generating vehicle recommendations.
    /// </summary>
    /// <remarks>
    /// This class is used to define user preferences and constraints for vehicle recommendations, including both desired and rejected attributes. It is designed for use in API requests to provide input for recommendation algorithms.
    /// </remarks>
    public class RecommendationParametersDto
    {
        /// <summary>
        /// Gets or sets the minimum price for the recommended vehicles.
        /// </summary>
        /// <value>A decimal representing the minimum price.</value>
        /// <example>20000.00.</example>
        public decimal? MinPrice { get; set; }

        /// <summary>
        /// Gets or sets the maximum price for the recommended vehicles.
        /// </summary>
        /// <value>A decimal representing the maximum price.</value>
        /// <example>50000.00.</example>
        public decimal? MaxPrice { get; set; }

        /// <summary>
        /// Gets or sets the minimum year of manufacture for the recommended vehicles.
        /// </summary>
        /// <value>An integer representing the minimum year.</value>
        /// <example>2015.</example>
        public int? MinYear { get; set; }

        /// <summary>
        /// Gets or sets the maximum year of manufacture for the recommended vehicles.
        /// </summary>
        /// <value>An integer representing the maximum year.</value>
        /// <example>2023.</example>
        public int? MaxYear { get; set; }

        /// <summary>
        /// Gets or sets the maximum mileage for the recommended vehicles.
        /// </summary>
        /// <value>An integer representing the maximum mileage in kilometers.</value>
        /// <example>100000.</example>
        public int? MaxMileage { get; set; }

        /// <summary>
        /// Gets or sets the list of preferred vehicle makes.
        /// </summary>
        /// <value>A list of strings representing the preferred makes.</value>
        /// <example>["Toyota", "Honda"].</example>
        public List<string>? PreferredMakes { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of preferred vehicle types.
        /// </summary>
        /// <value>A list of strings representing the preferred vehicle types.</value>
        /// <example>["SUV", "Sedan"].</example>
        public List<string>? PreferredVehicleTypes { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of preferred fuel types.
        /// </summary>
        /// <value>A list of strings representing the preferred fuel types.</value>
        /// <example>["Petrol", "Hybrid"].</example>
        public List<string>? PreferredFuelTypes { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of desired features for the recommended vehicles.
        /// </summary>
        /// <value>A list of strings representing the desired features.</value>
        /// <example>["Sunroof", "Leather Seats"].</example>
        public List<string>? DesiredFeatures { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of rejected vehicle makes.
        /// </summary>
        /// <value>A list of strings representing the makes to exclude.</value>
        /// <example>["Ford", "Chevrolet"].</example>
        public List<string>? RejectedMakes { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of rejected vehicle types.
        /// </summary>
        /// <value>A list of strings representing the vehicle types to exclude.</value>
        /// <example>["Truck", "Van"].</example>
        public List<string>? RejectedVehicleTypes { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of rejected fuel types.
        /// </summary>
        /// <value>A list of strings representing the fuel types to exclude.</value>
        /// <example>["Diesel"].</example>
        public List<string>? RejectedFuelTypes { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of rejected features for the recommended vehicles.
        /// </summary>
        /// <value>A list of strings representing the features to exclude.</value>
        /// <example>["Manual Transmission", "Cloth Seats"].</example>
        public List<string>? RejectedFeatures { get; set; } = [];

        /// <summary>
        /// Gets or sets the preferred transmission type for the recommended vehicles.
        /// </summary>
        /// <value>A string representing the preferred transmission type.</value>
        /// <example>"Automatic".</example>
        public string? Transmission { get; set; }

        /// <summary>
        /// Gets or sets the minimum engine size for the recommended vehicles.
        /// </summary>
        /// <value>A double representing the minimum engine size in liters.</value>
        /// <example>1.5.</example>
        public double? MinEngineSize { get; set; }

        /// <summary>
        /// Gets or sets the maximum engine size for the recommended vehicles.
        /// </summary>
        /// <value>A double representing the maximum engine size in liters.</value>
        /// <example>3.0.</example>
        public double? MaxEngineSize { get; set; }

        /// <summary>
        /// Gets or sets the minimum horsepower for the recommended vehicles.
        /// </summary>
        /// <value>An integer representing the minimum horsepower.</value>
        /// <example>150.</example>
        public int? MinHorsePower { get; set; }

        /// <summary>
        /// Gets or sets the maximum horsepower for the recommended vehicles.
        /// </summary>
        /// <value>An integer representing the maximum horsepower.</value>
        /// <example>300.</example>
        public int? MaxHorsePower { get; set; }

        /// <summary>
        /// Gets or sets the intent or purpose of the recommendation request.
        /// </summary>
        /// <value>A string representing the intent of the recommendation.</value>
        /// <example>"Find a family car with good fuel efficiency.".</example>
        public string? Intent { get; set; }
    }
}