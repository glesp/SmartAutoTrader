using System.Text.Json;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SmartAutoTrader.API.Data;
using SmartAutoTrader.API.Models;
using SmartAutoTrader.API.Repositories;

namespace SmartAutoTrader.API.Services
{
    public class OpenRouterRecommendationService(
        IVehicleRepository vehicleRepo,
        IConfiguration configuration,
        ILogger<OpenRouterRecommendationService> logger,
        HttpClient httpClient) : IAIRecommendationService
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly IVehicleRepository _vehicleRepo = vehicleRepo;
        private readonly HttpClient _httpClient = httpClient;
        private readonly ILogger<OpenRouterRecommendationService> _logger = logger;

        public async Task<IEnumerable<Vehicle>> GetRecommendationsAsync(int userId, RecommendationParameters parameters)
        {
            try
            {
                _logger.LogInformation(
                    "Fetching recommendations for user ID: {UserId} with parameters: {Parameters}",
                    userId, JsonSerializer.Serialize(parameters));

                // Log the important parameter values we'll be filtering on
                _logger.LogInformation(
                    "Key filter values: Makes={Makes}, FuelTypes={FuelTypes}, VehicleTypes={VehicleTypes}",
                    parameters.PreferredMakes != null ? string.Join(", ", parameters.PreferredMakes) : "null",
                    parameters.PreferredFuelTypes != null ? string.Join(", ", parameters.PreferredFuelTypes) : "null",
                    parameters.PreferredVehicleTypes != null
                        ? string.Join(", ", parameters.PreferredVehicleTypes)
                        : "null");

                List<Vehicle> filteredVehicles = await GetFilteredVehiclesAsync(parameters);

                if (!filteredVehicles.Any())
                {
                    _logger.LogWarning("No available vehicles found for filtering criteria.");
                    return new List<Vehicle>();
                }

                if (filteredVehicles.Count > parameters.MaxResults)
                {
                    filteredVehicles = filteredVehicles
                        .OrderByDescending(v => v.DateListed)
                        .Take(parameters.MaxResults ?? 5)
                        .ToList();
                }

                _logger.LogInformation("Returning {Count} vehicles based on parameter filtering", filteredVehicles.Count);

                // Log the actual results for debugging
                foreach (Vehicle? vehicle in filteredVehicles.Take(5))
                {
                    _logger.LogInformation(
                        "Recommended vehicle: {Year} {Make} {Model}, Type={Type}, Fuel={Fuel}",
                        vehicle.Year, vehicle.Make, vehicle.Model, vehicle.VehicleType, vehicle.FuelType);
                }

                return filteredVehicles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recommendations for user ID {UserId}", userId);
                return new List<Vehicle>();
            }
        }

        private async Task<List<Vehicle>> GetFilteredVehiclesAsync(RecommendationParameters parameters)
        {
            // Build a combined expression for filtering
            Expression<Func<Vehicle, bool>> filterExpression = BuildFilterExpression(parameters);
            
            // Log the filter criteria
            _logger.LogInformation("Applying filter expression for vehicle search");
            
            // Execute the query through the repository
            var vehicles = await _vehicleRepo.SearchAsync(filterExpression);
            
            // Optional in-memory feature ranking
            if (parameters.DesiredFeatures?.Any() == true)
            {
                _logger.LogInformation("Ranking by optional features: {Features}",
                    string.Join(", ", parameters.DesiredFeatures));

                HashSet<string> normalizedFeatureSet = parameters.DesiredFeatures
                    .Select(f => f.Trim().ToLowerInvariant())
                    .ToHashSet();

                return vehicles
                    .Select(v => new
                    {
                        Vehicle = v,
                        MatchCount = v.Features
                            .Count(f => normalizedFeatureSet.Contains(
                                f.Name?.Trim().ToLowerInvariant() ?? string.Empty))
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
                    v => parameters.PreferredFuelTypes.Contains(v.FuelType)
                );
            }
            
            // Add vehicle type filter
            if (parameters.PreferredVehicleTypes?.Any() == true)
            {
                expression = CombineExpressions(
                    expression, 
                    v => parameters.PreferredVehicleTypes.Contains(v.VehicleType)
                );
            }
            
            // Add make filter
            if (parameters.PreferredMakes?.Any() == true)
            {
                var normalizedMakes = parameters.PreferredMakes
                    .Select(m => m.Trim().ToLowerInvariant())
                    .ToList();
                
                _logger.LogInformation("Filtering by manufacturers: {Manufacturers}",
                    string.Join(", ", normalizedMakes));
                
                expression = CombineExpressions(
                    expression, 
                    v => normalizedMakes.Contains(v.Make.ToLower())
                );
            }
            
            return expression;
        }
        
        // Helper method to combine two expressions with AND operator
        private Expression<Func<Vehicle, bool>> CombineExpressions(
            Expression<Func<Vehicle, bool>> expr1, 
            Expression<Func<Vehicle, bool>> expr2)
        {
            var parameter = Expression.Parameter(typeof(Vehicle), "v");
            
            var leftVisitor = new ReplaceParameterVisitor(expr1.Parameters[0], parameter);
            var left = leftVisitor.Visit(expr1.Body);
            
            var rightVisitor = new ReplaceParameterVisitor(expr2.Parameters[0], parameter);
            var right = rightVisitor.Visit(expr2.Body);
            
            return Expression.Lambda<Func<Vehicle, bool>>(
                Expression.AndAlso(left, right), parameter);
        }
        
        private class ReplaceParameterVisitor : System.Linq.Expressions.ExpressionVisitor
        {
            private readonly ParameterExpression _oldParameter;
            private readonly ParameterExpression _newParameter;

            public ReplaceParameterVisitor(ParameterExpression oldParameter, ParameterExpression newParameter)
            {
                _oldParameter = oldParameter;
                _newParameter = newParameter;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return node == _oldParameter ? _newParameter : base.VisitParameter(node);
            }
        }
    }
}
