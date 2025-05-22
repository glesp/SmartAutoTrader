/* <copyright file="VehicleFeatureDTO.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the VehicleFeatureDTO class, which serves as a Data Transfer Object (DTO) for representing features associated with vehicles in the Smart Auto Trader application.
</summary>
<remarks>
The VehicleFeatureDTO class is used to encapsulate data related to individual features of a vehicle, such as "Sunroof" or "Leather Seats." It includes validation attributes to ensure that the feature name meets the application's requirements. This DTO is typically used in API requests and responses to manage vehicle features.
</remarks>
<dependencies>
- System.ComponentModel.DataAnnotations
</dependencies>
 */

namespace SmartAutoTrader.API.DTOs
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Represents a Data Transfer Object (DTO) for vehicle features.
    /// </summary>
    /// <remarks>
    /// This class is used to encapsulate data for individual vehicle features, such as "Sunroof" or "Bluetooth." It includes validation rules to ensure the feature name is valid.
    /// </remarks>
    public class VehicleFeatureDTO
    {
        /// <summary>
        /// Gets or sets the name of the vehicle feature.
        /// </summary>
        /// <value>A string representing the name of the feature.</value>
        /// <example>"Sunroof".</example>
        /// <exception cref="ValidationException">Thrown if the feature name is not provided or does not meet length requirements.</exception>
        [Required(ErrorMessage = "Feature name is required.")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Feature name must be between 1 and 100 characters.")]
        public string Name { get; set; } = string.Empty;
    }
}