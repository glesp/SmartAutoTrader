/* <copyright file="RecommendationParameterValidator.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the RecommendationParameterValidator class, which provides methods for validating user-provided recommendation parameters in the Smart Auto Trader application.
</summary>
<remarks>
The RecommendationParameterValidator class ensures that user-provided recommendation parameters, such as preferred fuel types, vehicle types, price ranges, and year ranges, are valid. It checks for invalid enum values, ensures logical consistency in ranges, and provides detailed error messages for invalid inputs. This class is typically used in scenarios where user input needs to be validated before processing recommendations.
</remarks>
<dependencies>
- SmartAutoTrader.API.Enums
- SmartAutoTrader.API.Models
</dependencies>
 */

namespace SmartAutoTrader.API.Validators
{
    using SmartAutoTrader.API.Enums;
    using SmartAutoTrader.API.Models;

    /// <summary>
    /// Provides methods for validating user-provided recommendation parameters.
    /// </summary>
    /// <remarks>
    /// This static class validates various aspects of recommendation parameters, including enum values, price ranges, and year ranges. It ensures that invalid inputs are identified and replaced or rejected with appropriate error messages.
    /// </remarks>
    public static class RecommendationParameterValidator
    {
        /// <summary>
        /// Validates the provided recommendation parameters.
        /// </summary>
        /// <param name="parameters">The <see cref="RecommendationParameters"/> object containing user preferences for recommendations.</param>
        /// <param name="errorMessage">An output parameter that contains the error message if validation fails; otherwise, null.</param>
        /// <returns>
        /// True if the parameters are valid; otherwise, false.
        /// </returns>
        /// <remarks>
        /// This method performs the following validations:
        /// - Ensures that all provided fuel types and vehicle types are valid enums.
        /// - Ensures that the minimum price is not greater than the maximum price.
        /// - Ensures that the minimum year is not greater than the maximum year.
        /// If any validation fails, an appropriate error message is returned.
        /// </remarks>
        /// <example>
        /// <code>
        /// var parameters = new RecommendationParameters
        /// {
        ///     PreferredFuelTypes = new List<FuelType> { FuelType.Gasoline, (FuelType)999 },
        ///     MinPrice = 5000,
        ///     MaxPrice = 3000
        /// };
        ///
        /// if (!RecommendationParameterValidator.Validate(parameters, out string? errorMessage))
        /// {
        ///     Console.WriteLine($"Validation failed: {errorMessage}");
        /// }
        /// </code>
        /// </example>
        public static bool Validate(RecommendationParameters parameters, out string? errorMessage)
        {
            errorMessage = null;

            // Check if FuelType values are valid
            if (parameters.PreferredFuelTypes?.Any() == true)
            {
                List<string> invalidFuelTypes = new();
                List<FuelType> validatedFuelTypes = new();

                foreach (FuelType fuelType in parameters.PreferredFuelTypes)
                {
                    // Check if the value is a valid enum
                    if (!Enum.IsDefined(typeof(FuelType), fuelType))
                    {
                        invalidFuelTypes.Add(fuelType.ToString());
                    }
                    else
                    {
                        validatedFuelTypes.Add(fuelType);
                    }
                }

                // If there are invalid fuel types, report them
                if (invalidFuelTypes.Count != 0)
                {
                    errorMessage =
                        $"Invalid FuelType values: {string.Join(", ", invalidFuelTypes)}. Valid options are: {string.Join(", ", Enum.GetNames(typeof(FuelType)))}";
                    return false;
                }

                // Replace the list with validated values
                parameters.PreferredFuelTypes = validatedFuelTypes;
            }

            // Check if VehicleType values are valid
            if (parameters.PreferredVehicleTypes?.Any() == true)
            {
                List<string> invalidVehicleTypes = new();
                List<VehicleType> validatedVehicleTypes = new();

                foreach (VehicleType vehicleType in parameters.PreferredVehicleTypes)
                {
                    // Check if the value is a valid enum
                    if (!Enum.IsDefined(typeof(VehicleType), vehicleType))
                    {
                        invalidVehicleTypes.Add(vehicleType.ToString());
                    }
                    else
                    {
                        validatedVehicleTypes.Add(vehicleType);
                    }
                }

                // If there are invalid vehicle types, report them
                if (invalidVehicleTypes.Count != 0)
                {
                    errorMessage =
                        $"Invalid VehicleType values: {string.Join(", ", invalidVehicleTypes)}. Valid options are: {string.Join(", ", Enum.GetNames(typeof(VehicleType)))}";
                    return false;
                }

                // Replace the list with validated values
                parameters.PreferredVehicleTypes = validatedVehicleTypes;
            }

            // Validate price range
            if (parameters.MinPrice.HasValue && parameters.MaxPrice.HasValue &&
                parameters.MinPrice > parameters.MaxPrice)
            {
                errorMessage =
                    $"Invalid price range: Min ({parameters.MinPrice}) cannot be greater than Max ({parameters.MaxPrice})";
                return false;
            }

            // Validate year range
            if (parameters.MinYear.HasValue && parameters.MaxYear.HasValue &&
                parameters.MinYear > parameters.MaxYear)
            {
                errorMessage =
                    $"Invalid year range: Min ({parameters.MinYear}) cannot be greater than Max ({parameters.MaxYear})";
                return false;
            }

            return true;
        }
    }
}