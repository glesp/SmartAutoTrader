namespace SmartAutoTrader.API.DTOs;

public class RecommendationParametersDto
{
    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    public int? MinYear { get; set; }

    public int? MaxYear { get; set; }

    public int? MaxMileage { get; set; }

    public List<string>? PreferredMakes { get; set; } = [];

    public List<string>? PreferredVehicleTypes { get; set; } = [];

    public List<string>? PreferredFuelTypes { get; set; } = [];

    public List<string>? DesiredFeatures { get; set; } = [];

    // Add missing properties for rejected/negated items
    public List<string>? RejectedMakes { get; set; } = [];

    public List<string>? RejectedVehicleTypes { get; set; } = [];

    public List<string>? RejectedFuelTypes { get; set; } = [];

    public List<string>? RejectedFeatures { get; set; } = [];

    // Add transmission
    public string? Transmission { get; set; }

    // Add engine size
    public double? MinEngineSize { get; set; }

    public double? MaxEngineSize { get; set; }

    // Add horsepower
    public int? MinHorsePower { get; set; }

    public int? MaxHorsePower { get; set; }

    // Add intent for completeness
    public string? Intent { get; set; }
}