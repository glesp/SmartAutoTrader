/* <copyright file="EnumsHelper.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the EnumHelpers class, which provides utility methods for parsing and working with enumerations in the Smart Auto Trader application.
</summary>
<remarks>
The EnumHelpers class is a static helper class designed to simplify the process of parsing string values into enumeration types, such as FuelType, VehicleType, and TransmissionType. It includes methods for handling standard parsing as well as fuzzy matching for common terms. Additionally, it provides methods for parsing lists of enumeration values. This class is typically used in scenarios where user input or external data needs to be mapped to strongly-typed enumerations.
</remarks>
<dependencies>
- System.Globalization
- SmartAutoTrader.API.Enums
- SmartAutoTrader.API.Models
</dependencies>
 */

namespace SmartAutoTrader.API.Helpers
{
    using System.Globalization;
    using SmartAutoTrader.API.Enums;
    using SmartAutoTrader.API.Models;

    /// <summary>
    /// Provides utility methods for parsing and working with enumerations.
    /// </summary>
    /// <remarks>
    /// This static class simplifies the process of converting string values into enumeration types, including support for fuzzy matching and list parsing.
    /// </remarks>
    public static class EnumHelpers
    {
        /// <summary>
        /// Attempts to parse a string value into a <see cref="FuelType"/> enumeration.
        /// </summary>
        /// <param name="value">The string value to parse.</param>
        /// <param name="result">The parsed <see cref="FuelType"/> value, if successful.</param>
        /// <returns>True if parsing is successful; otherwise, false.</returns>
        /// <remarks>
        /// This method supports standard parsing as well as fuzzy matching for common terms like "gas" or "ev.".
        /// </remarks>
        /// <example>
        /// <code>
        /// if (EnumHelpers.TryParseFuelType("gasoline", out FuelType fuelType))
        /// {
        ///     Console.WriteLine($"Parsed fuel type: {fuelType}");
        /// }
        /// </code>
        /// </example>
        public static bool TryParseFuelType(string value, out FuelType result)
        {
            result = default;

            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            // Try standard parsing first (case-insensitive)
            if (Enum.TryParse(value, true, out result))
            {
                return true;
            }

            // Additional fuzzy matching for common terms
            switch (value.ToLower(CultureInfo.CurrentCulture).Trim())
            {
                case "gas":
                case "gasoline":
                case "unleaded":
                    result = FuelType.Petrol;
                    return true;

                case "ev":
                case "battery":
                case "battery electric":
                    result = FuelType.Electric;
                    return true;

                case "phev":
                case "plug-in":
                case "plug-in hybrid":
                    result = FuelType.Hybrid;
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Attempts to parse a string value into a <see cref="VehicleType"/> enumeration.
        /// </summary>
        /// <param name="value">The string value to parse.</param>
        /// <param name="result">The parsed <see cref="VehicleType"/> value, if successful.</param>
        /// <returns>True if parsing is successful; otherwise, false.</returns>
        /// <remarks>
        /// This method supports standard parsing as well as fuzzy matching for common terms like "suv" or "crossover.".
        /// </remarks>
        /// <example>
        /// <code>
        /// if (EnumHelpers.TryParseVehicleType("SUV", out VehicleType vehicleType))
        /// {
        ///     Console.WriteLine($"Parsed vehicle type: {vehicleType}");
        /// }
        /// </code>
        /// </example>
        public static bool TryParseVehicleType(string value, out VehicleType result)
        {
            result = default;

            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            // Try standard parsing first (case-insensitive)
            if (Enum.TryParse(value, true, out result))
            {
                return true;
            }

            // Additional fuzzy matching for common terms
            switch (value.ToLower(CultureInfo.CurrentCulture).Trim())
            {
                case "suv":
                case "crossover":
                case "cuv":
                    result = VehicleType.SUV;
                    return true;

                case "sedan":
                case "saloon":
                    result = VehicleType.Sedan;
                    return true;

                case "hatchback":
                case "5-door":
                case "hot hatch":
                    result = VehicleType.Hatchback;
                    return true;

                case "estate":
                case "wagon":
                case "touring":
                    result = VehicleType.Estate;
                    return true;

                case "coupe":
                case "2-door":
                case "sports car":
                    result = VehicleType.Coupe;
                    return true;

                case "convertible":
                case "cabriolet":
                case "roadster":
                    result = VehicleType.Convertible;
                    return true;

                case "truck":
                case "pickup":
                case "flatbed":
                    result = VehicleType.Pickup;
                    return true;

                case "van":
                case "minivan":
                case "mpv":
                    result = VehicleType.Van;
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Attempts to parse a string value into a <see cref="TransmissionType"/> enumeration.
        /// </summary>
        /// <param name="value">The string value to parse.</param>
        /// <param name="result">The parsed <see cref="TransmissionType"/> value, if successful.</param>
        /// <returns>True if parsing is successful; otherwise, false.</returns>
        /// <remarks>
        /// This method supports standard parsing as well as fuzzy matching for common terms like "auto" or "stick shift.".
        /// </remarks>
        /// <example>
        /// <code>
        /// if (EnumHelpers.TryParseTransmissionType("automatic", out TransmissionType transmissionType))
        /// {
        ///     Console.WriteLine($"Parsed transmission type: {transmissionType}");
        /// }
        /// </code>
        /// </example>
        public static bool TryParseTransmissionType(string value, out TransmissionType result)
        {
            result = default;

            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            // Try standard parsing first (case-insensitive)
            if (Enum.TryParse(value, true, out result))
            {
                return true;
            }

            // Additional fuzzy matching for common terms
            switch (value.ToLower(CultureInfo.CurrentCulture).Trim())
            {
                case "auto":
                case "self-shifting":
                case "automat":
                case "automatic transmission":
                case "auto trans":
                    result = TransmissionType.Automatic;
                    return true;

                case "manual transmission":
                case "stick":
                case "stick shift":
                case "standard":
                case "manuel":
                    result = TransmissionType.Manual;
                    return true;

                case "semi":
                case "semi auto":
                case "semi-auto":
                case "paddle":
                case "paddle shift":
                case "dual clutch":
                case "dct":
                case "semi-automatic":
                    result = TransmissionType.SemiAutomatic;
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Parses a list of string values into a list of <see cref="FuelType"/> enumerations.
        /// </summary>
        /// <param name="values">The list of string values to parse.</param>
        /// <returns>A list of parsed <see cref="FuelType"/> values.</returns>
        /// <remarks>
        /// This method iterates through the provided list of strings and attempts to parse each value into a <see cref="FuelType"/>. Invalid values are ignored.
        /// </remarks>
        /// <example>
        /// <code>
        /// var fuelTypes = EnumHelpers.ParseFuelTypeList(new[] { "gas", "electric", "invalid" });
        /// Console.WriteLine($"Parsed fuel types: {string.Join(", ", fuelTypes)}");
        /// </code>
        /// </example>
        public static List<FuelType> ParseFuelTypeList(IEnumerable<string> values)
        {
            List<FuelType> result = [];

            foreach (string value in values)
            {
                if (TryParseFuelType(value, out FuelType fuelType))
                {
                    result.Add(fuelType);
                }
            }

            return result;
        }

        /// <summary>
        /// Parses a list of string values into a list of <see cref="VehicleType"/> enumerations.
        /// </summary>
        /// <param name="values">The list of string values to parse.</param>
        /// <returns>A list of parsed <see cref="VehicleType"/> values.</returns>
        /// <remarks>
        /// This method iterates through the provided list of strings and attempts to parse each value into a <see cref="VehicleType"/>. Invalid values are ignored.
        /// </remarks>
        /// <example>
        /// <code>
        /// var vehicleTypes = EnumHelpers.ParseVehicleTypeList(new[] { "SUV", "sedan", "invalid" });
        /// Console.WriteLine($"Parsed vehicle types: {string.Join(", ", vehicleTypes)}");
        /// </code>
        /// </example>
        public static List<VehicleType> ParseVehicleTypeList(IEnumerable<string> values)
        {
            List<VehicleType> result = [];

            foreach (string value in values)
            {
                if (TryParseVehicleType(value, out VehicleType vehicleType))
                {
                    result.Add(vehicleType);
                }
            }

            return result;
        }
    }
}