namespace SmartAutoTrader.API.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using SmartAutoTrader.API.Enums;
    using SmartAutoTrader.API.Helpers;
    using SmartAutoTrader.API.Models;
    using SmartAutoTrader.API.Repositories;

    // Interface for any AI recommendation service (allows easy swapping)
    public interface IAIRecommendationService
    {
        Task<IEnumerable<Vehicle>> GetRecommendationsAsync(int userId, RecommendationParameters parameters);
    }

    public class OpenRouterRecommendationService(
        IVehicleRepository vehicleRepo,
        IConfiguration configuration,
        ILogger<OpenRouterRecommendationService> logger,
        HttpClient httpClient) : IAIRecommendationService
    {
        private readonly IConfiguration configuration = configuration;
        private readonly HttpClient httpClient = httpClient;
        private readonly ILogger<OpenRouterRecommendationService> logger = logger;
        private readonly IVehicleRepository vehicleRepo = vehicleRepo;
public async Task<IEnumerable<Vehicle>> GetRecommendationsAsync(int userId, RecommendationParameters parameters)
        {
            try
            {
                this.logger.LogInformation(
                    "Fetching recommendations for user ID: {UserId} with parameters: {Parameters}",
                    userId,
                    JsonSerializer.Serialize(parameters));

                // Log the important parameter values we'll be filtering on
                this.logger.LogInformation(
                    "Key filter values: Makes={Makes}, FuelTypes={FuelTypes}, VehicleTypes={VehicleTypes}, Transmission={Transmission}, EngineSize={EngineSize}, HorsePower={HorsePower}",
                    parameters.PreferredMakes != null ? string.Join(", ", parameters.PreferredMakes) : "null",
                    parameters.PreferredFuelTypes != null ? string.Join(", ", parameters.PreferredFuelTypes) : "null",
                    parameters.PreferredVehicleTypes != null
                        ? string.Join(", ", parameters.PreferredVehicleTypes)
                        : "null",
                    parameters.Transmission.HasValue ? parameters.Transmission.Value.ToString() : "null",
                    (parameters.MinEngineSize.HasValue || parameters.MaxEngineSize.HasValue)
                        ? $"{parameters.MinEngineSize?.ToString() ?? "min"}-{parameters.MaxEngineSize?.ToString() ?? "max"}"
                        : "null",
                    (parameters.MinHorsePower.HasValue || parameters.MaxHorsePower.HasValue)
                        ? $"{parameters.MinHorsePower?.ToString() ?? "min"}-{parameters.MaxHorsePower?.ToString() ?? "max"}"
                        : "null");

                List<Vehicle> filteredVehicles = await this.GetFilteredVehiclesAsync(parameters);

                if (filteredVehicles.Count == 0)
                {
                    this.logger.LogWarning("No available vehicles found for filtering criteria.");
                    return new List<Vehicle>();
                }

                if (filteredVehicles.Count > parameters.MaxResults)
                {
                    filteredVehicles = filteredVehicles
                        .OrderByDescending(v => v.DateListed)
                        .Take(parameters.MaxResults ?? 5)
                        .ToList();
                }

                this.logger.LogInformation("Returning {Count} vehicles based on parameter filtering", filteredVehicles.Count);

                // Log the actual results for debugging
                foreach (Vehicle? vehicle in filteredVehicles.Take(5))
                {
                    this.logger.LogInformation(
                        "Recommended vehicle: {Year} {Make} {Model}, Type={Type}, Fuel={Fuel}",
                        vehicle.Year,
                        vehicle.Make,
                        vehicle.Model,
                        vehicle.VehicleType,
                        vehicle.FuelType);
                }

                return filteredVehicles;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving recommendations for user ID {UserId}", userId);
                return new List<Vehicle>();
            }
        }

        private async Task<List<Vehicle>> GetFilteredVehiclesAsync(RecommendationParameters parameters)
        {
            // Build a combined expression for filtering
            Expression<Func<Vehicle, bool>> filterExpression = this.BuildFilterExpression(parameters);

            // Log the filter criteria
            this.logger.LogInformation("Applying filter expression for vehicle search");

            // Execute the query through the repository
            List<Vehicle> vehicles = await this.vehicleRepo.SearchAsync(filterExpression);

            // Optional in-memory feature ranking
            if (parameters.DesiredFeatures?.Any() == true)
            {
                this.logger.LogInformation(
                    "Ranking by optional features: {Features}",
                    string.Join(", ", parameters.DesiredFeatures));

                HashSet<string> normalizedFeatureSet = parameters.DesiredFeatures
                    .Select(f => f.Trim().ToLowerInvariant())
                    .ToHashSet();

                return vehicles
                    .Select(
                        v => new
                        {
                            Vehicle = v,
                            MatchCount = v.Features?
                                .Count(
                                    f => normalizedFeatureSet.Contains(f.Name?.Trim().ToLowerInvariant() ?? string.Empty)),
                        })
                    .OrderByDescending(v => v.MatchCount)
                    .ThenBy(v => v.Vehicle.Price)
                    .Select(v => v.Vehicle)
                    .ToList();
            }

            return vehicles;
        }

        private Expression<Func<Vehicle, bool>> BuildFilterExpression(RecommendationParameters parameters)
        {
            // Start with a base expression that always evaluates to true
            Expression<Func<Vehicle, bool>> expression = v => v.Status == VehicleStatus.Available;

            // Add price range filters
            if (parameters.MinPrice.HasValue)
            {
                expression = CombineExpressions(expression, v => v.Price >= parameters.MinPrice.Value);
            }

            if (parameters.MaxPrice.HasValue)
            {
                expression = CombineExpressions(expression, v => v.Price <= parameters.MaxPrice.Value);
            }

            // Add year range filters
            if (parameters.MinYear.HasValue)
            {
                expression = CombineExpressions(expression, v => v.Year >= parameters.MinYear.Value);
            }

            if (parameters.MaxYear.HasValue)
            {
                expression = CombineExpressions(expression, v => v.Year <= parameters.MaxYear.Value);
            }

            // Add mileage filter
            if (parameters.MaxMileage.HasValue)
            {
                expression = CombineExpressions(expression, v => v.Mileage <= parameters.MaxMileage.Value);
            }

            // Add fuel type filter
            if (parameters.PreferredFuelTypes?.Any() == true)
            {
                expression = CombineExpressions(
                    expression,
                    v => parameters.PreferredFuelTypes.Contains(v.FuelType));
            }

            // Add vehicle type filter
            if (parameters.PreferredVehicleTypes?.Any() == true)
            {
                expression = CombineExpressions(
                    expression,
                    v => parameters.PreferredVehicleTypes.Contains(v.VehicleType));
            }

            // Add transmission filter
            if (parameters.Transmission.HasValue)
            {
                expression = CombineExpressions(
                    expression,
                    v => v.Transmission == parameters.Transmission.Value);
            }

            // Add engine size range filters
            if (parameters.MinEngineSize.HasValue)
            {
                expression = CombineExpressions(
                    expression,
                    v => v.EngineSize >= parameters.MinEngineSize.Value);
            }

            if (parameters.MaxEngineSize.HasValue)
            {
                expression = CombineExpressions(
                    expression,
                    v => v.EngineSize <= parameters.MaxEngineSize.Value);
            }

            // Add horsepower range filters
            if (parameters.MinHorsePower.HasValue)
            {
                expression = CombineExpressions(
                    expression,
                    v => v.HorsePower >= parameters.MinHorsePower.Value);
            }

            if (parameters.MaxHorsePower.HasValue)
            {
                expression = CombineExpressions(
                    expression,
                    v => v.HorsePower <= parameters.MaxHorsePower.Value);
            }

            // Add make filter - FIXED: Use EF Core's EF.Functions.Like for case-insensitive comparison
            if (parameters.PreferredMakes?.Any() == true)
            {
                // Create an OR condition for each make
                Expression<Func<Vehicle, bool>>? makesExpression = null;

                foreach (string make in parameters.PreferredMakes)
                {
                    string currentMake = make.Trim();
                    Expression<Func<Vehicle, bool>> makeCondition = BuildMakeMatchExpression(currentMake);

                    // Either initialize makesExpression or combine with OR
                    makesExpression = makesExpression == null
                        ? makeCondition
                        : CombineExpressionsWithOr(makesExpression, makeCondition);
                }

                // Combine with the main expression using AND if we have any makes
                if (makesExpression != null)
                {
                    expression = CombineExpressions(expression, makesExpression);
                }

                this.logger.LogInformation(
                    "Filtering by manufacturers: {Manufacturers}",
                    string.Join(", ", parameters.PreferredMakes));
            }

            // Add rejected makes filter
            if (parameters.RejectedMakes?.Any() == true)
            {
                foreach (string rejectedMake in parameters.RejectedMakes)
                {
                    // For each rejected make, create a NOT condition
                    expression = CombineExpressions(
                        expression,
                        v => !v.Make.Contains(rejectedMake) && !rejectedMake.Contains(v.Make));
                }

                this.logger.LogInformation(
                    "Excluding manufacturers: {RejectedManufacturers}",
                    string.Join(", ", parameters.RejectedMakes));
            }

            return expression;
        }

        // Helper method for case-insensitive make matching that's compatible with EF Core
        private static Expression<Func<Vehicle, bool>> BuildMakeMatchExpression(string make)
        {
            return v => v.Make == make ||
                        v.Make.Contains(make) ||
                        make.Contains(v.Make);
        }

        // Helper method to combine two expressions with OR operator
        private static Expression<Func<Vehicle, bool>> CombineExpressionsWithOr(
            Expression<Func<Vehicle, bool>> expr1,
            Expression<Func<Vehicle, bool>> expr2)
        {
            ParameterExpression parameter = Expression.Parameter(typeof(Vehicle), "v");

            ReplaceParameterVisitor leftVisitor = new(expr1.Parameters[0], parameter);
            Expression left = leftVisitor.Visit(expr1.Body);

            ReplaceParameterVisitor rightVisitor = new(expr2.Parameters[0], parameter);
            Expression right = rightVisitor.Visit(expr2.Body);

            return Expression.Lambda<Func<Vehicle, bool>>(Expression.OrElse(left, right), parameter);
        }

        // Helper method to combine two expressions with AND operator
        private static Expression<Func<Vehicle, bool>> CombineExpressions(
            Expression<Func<Vehicle, bool>> expr1,
            Expression<Func<Vehicle, bool>> expr2)
        {
            ParameterExpression parameter = Expression.Parameter(typeof(Vehicle), "v");

            ReplaceParameterVisitor leftVisitor = new(expr1.Parameters[0], parameter);
            Expression left = leftVisitor.Visit(expr1.Body);

            ReplaceParameterVisitor rightVisitor = new(expr2.Parameters[0], parameter);
            Expression right = rightVisitor.Visit(expr2.Body);

            return Expression.Lambda<Func<Vehicle, bool>>(Expression.AndAlso(left, right), parameter);
        }

        private sealed class ReplaceParameterVisitor(ParameterExpression oldParameter, ParameterExpression newParameter)
            : ExpressionVisitor
        {
            private readonly ParameterExpression newParameter = newParameter;
            private readonly ParameterExpression oldParameter = oldParameter;

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return node == this.oldParameter ? this.newParameter : base.VisitParameter(node);
            }
        }
    }
}