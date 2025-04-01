using SmartAutoTrader.API.Models;
using SmartAutoTrader.API.Services;

namespace SmartAutoTrader.API.Validators;

public static class RecommendationParameterValidator
{
    public static bool Validate(RecommendationParameters parameters, out string errorMessage)
    {
        errorMessage = null;

        // Check if FuelType values are valid
        if (parameters.PreferredFuelTypes?.Any() == true)
        {
            var invalidFuelTypes = new List<string>();
            var validatedFuelTypes = new List<FuelType>();

            foreach (var fuelType in parameters.PreferredFuelTypes)
            {
                // Skip null values
                if (fuelType == null)
                    continue;

                // Check if the value is a valid enum
                if (!Enum.IsDefined(typeof(FuelType), fuelType))
                    invalidFuelTypes.Add(fuelType.ToString());
                else
                    validatedFuelTypes.Add(fuelType);
            }

            // If there are invalid fuel types, report them
            if (invalidFuelTypes.Any())
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
            var invalidVehicleTypes = new List<string>();
            var validatedVehicleTypes = new List<VehicleType>();

            foreach (var vehicleType in parameters.PreferredVehicleTypes)
            {
                // Skip null values
                if (vehicleType == null)
                    continue;

                // Check if the value is a valid enum
                if (!Enum.IsDefined(typeof(VehicleType), vehicleType))
                    invalidVehicleTypes.Add(vehicleType.ToString());
                else
                    validatedVehicleTypes.Add(vehicleType);
            }

            // If there are invalid vehicle types, report them
            if (invalidVehicleTypes.Any())
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