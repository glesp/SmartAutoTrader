using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartAutoTrader.API.Data;
using SmartAutoTrader.API.Models;

namespace SmartAutoTrader.API.Services
{
    public class OpenRouterRecommendationService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<OpenRouterRecommendationService> logger,
        HttpClient httpClient) : IAIRecommendationService
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly ApplicationDbContext _context = context;
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
    IQueryable<Vehicle> query = _context.Vehicles.AsQueryable();

    // Numeric filters
    if (parameters.MinPrice.HasValue)
        query = query.Where(v => v.Price >= parameters.MinPrice.Value);

    if (parameters.MaxPrice.HasValue)
        query = query.Where(v => v.Price <= parameters.MaxPrice.Value);

    if (parameters.MinYear.HasValue)
        query = query.Where(v => v.Year >= parameters.MinYear.Value);

    if (parameters.MaxYear.HasValue)
        query = query.Where(v => v.Year <= parameters.MaxYear.Value);

    if (parameters.MaxMileage.HasValue)
        query = query.Where(v => v.Mileage <= parameters.MaxMileage.Value);

    // Enum filters
    if (parameters.PreferredFuelTypes?.Any() == true)
        query = query.Where(v => parameters.PreferredFuelTypes.Contains(v.FuelType));

    if (parameters.PreferredVehicleTypes?.Any() == true)
        query = query.Where(v => parameters.PreferredVehicleTypes.Contains(v.VehicleType));

    // Make filter (case-insensitive SQLite-compatible)
    if (parameters.PreferredMakes?.Any() == true)
    {
        _logger.LogInformation("Filtering by manufacturers: {Manufacturers}",
            string.Join(", ", parameters.PreferredMakes));

        var normalizedMakes = parameters.PreferredMakes
            .Select(m => m.Trim().ToLowerInvariant())
            .ToList();

        query = query.Where(v => normalizedMakes.Contains(v.Make.ToLower()));
    }

    // Only include available vehicles
    query = query.Where(v => v.Status == VehicleStatus.Available);

    // Include related data (after filtering)
    query = query
        .Include(v => v.Features)
        .Include(v => v.Images);

    _logger.LogInformation("SQL Query: {Query}", query.ToQueryString());

    // Execute query
    var vehicles = await query.ToListAsync();

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

    }
}