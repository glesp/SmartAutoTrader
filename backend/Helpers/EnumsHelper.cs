// <copyright file="EnumsHelper.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SmartAutoTrader.API.Helpers
{
    using System.Globalization;
    using SmartAutoTrader.API.Enums;
    using SmartAutoTrader.API.Models;

    public static class EnumHelpers
    {
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