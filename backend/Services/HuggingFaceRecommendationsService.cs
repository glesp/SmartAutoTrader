using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartAutoTrader.API.Data;
using SmartAutoTrader.API.Models;

namespace SmartAutoTrader.API.Services;

public class HuggingFaceRecommendationService : IAIRecommendationService
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly ILogger<HuggingFaceRecommendationService> _logger;

    public HuggingFaceRecommendationService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<HuggingFaceRecommendationService> logger,
        HttpClient httpClient)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<Vehicle>> GetRecommendationsAsync(int userId, RecommendationParameters parameters)
    {
        try
        {
            _logger.LogInformation("Fetching recommendations for user ID: {UserId} with parameters: {Parameters}",
                userId, JsonSerializer.Serialize(parameters));

            // Log the important parameter values we'll be filtering on
            _logger.LogInformation(
                "Key filter values: Makes={Makes}, FuelTypes={FuelTypes}, VehicleTypes={VehicleTypes}",
                parameters.PreferredMakes != null ? string.Join(", ", parameters.PreferredMakes) : "null",
                parameters.PreferredFuelTypes != null ? string.Join(", ", parameters.PreferredFuelTypes) : "null",
                parameters.PreferredVehicleTypes != null
                    ? string.Join(", ", parameters.PreferredVehicleTypes)
                    : "null");

            var filteredVehicles = await GetFilteredVehiclesAsync(parameters);

            if (!filteredVehicles.Any())
            {
                _logger.LogWarning("No available vehicles found for filtering criteria.");
                return new List<Vehicle>();
            }

            if (filteredVehicles.Count > parameters.MaxResults)
                filteredVehicles = filteredVehicles
                    .OrderByDescending(v => v.DateListed)
                    .Take(parameters.MaxResults ?? 5)
                    .ToList();

            _logger.LogInformation("Returning {Count} vehicles based on parameter filtering", filteredVehicles.Count);

            // Log the actual results for debugging
            foreach (var vehicle in filteredVehicles.Take(5))
                _logger.LogInformation("Recommended vehicle: {Year} {Make} {Model}, Type={Type}, Fuel={Fuel}",
                    vehicle.Year, vehicle.Make, vehicle.Model, vehicle.VehicleType, vehicle.FuelType);

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
        var query = _context.Vehicles.AsQueryable();

        if (parameters.MinPrice.HasValue) query = query.Where(v => v.Price >= parameters.MinPrice.Value);

        if (parameters.MaxPrice.HasValue) query = query.Where(v => v.Price <= parameters.MaxPrice.Value);

        if (parameters.MinYear.HasValue) query = query.Where(v => v.Year >= parameters.MinYear.Value);

        if (parameters.MaxYear.HasValue) query = query.Where(v => v.Year <= parameters.MaxYear.Value);

        if (parameters.MaxMileage.HasValue) query = query.Where(v => v.Mileage <= parameters.MaxMileage.Value);


        if (parameters.PreferredVehicleTypes?.Any() == true)
        {
            _logger.LogInformation("Filtering by vehicle types: {VehicleTypes}",
                string.Join(", ", parameters.PreferredVehicleTypes));

            // We can use the enum values directly since they're already parsed
            query = query.Where(v => parameters.PreferredVehicleTypes.Contains(v.VehicleType));
        }

        if (parameters.PreferredFuelTypes?.Any() == true)
        {
            _logger.LogInformation("Filtering by fuel types: {FuelTypes}",
                string.Join(", ", parameters.PreferredFuelTypes));

            // We can use the enum values directly since they're already parsed
            query = query.Where(v => parameters.PreferredFuelTypes.Contains(v.FuelType));
        }

        if (parameters.PreferredMakes?.Any() == true)
        {
            _logger.LogInformation("Filtering by manufacturers: {Manufacturers}",
                string.Join(", ", parameters.PreferredMakes));

            // Make case-insensitive comparison
            var lowerMakes = parameters.PreferredMakes.Select(m => m.ToLower()).ToList();
            query = query.Where(v => lowerMakes.Contains(v.Make.ToLower()));
        }

        if (parameters.DesiredFeatures?.Any() == true)
        {
            _logger.LogInformation("Ranking by optional features: {Features}",
                string.Join(", ", parameters.DesiredFeatures));

            var featureSet = parameters.DesiredFeatures
                .Select(f => f.ToLowerInvariant())
                .ToHashSet();

            query = query
                .Select(v => new
                {
                    Vehicle = v,
                    MatchCount = v.Features.Count(f => featureSet.Contains(f.Name.ToLower()))
                })
                .OrderByDescending(v => v.MatchCount)
                .ThenBy(v => v.Vehicle.Price) // Optional: add tie-breakers like price
                .Select(v => v.Vehicle);
        }


        query = query.Where(v => v.Status == VehicleStatus.Available)
            .Include(v => v.Features)
            .Include(v => v.Images);

        _logger.LogInformation("SQL Query: {Query}", query.ToQueryString());

        return await query.ToListAsync();
    }
}