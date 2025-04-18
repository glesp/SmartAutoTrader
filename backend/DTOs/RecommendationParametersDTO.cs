namespace SmartAutoTrader.API.DTOs;

public class RecommendationParametersDto
{
    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    public int? MinYear { get; set; }

    public int? MaxYear { get; set; }

    public int? MaxMileage { get; set; }

    public List<string> PreferredMakes { get; set; } = [];

    public List<string> PreferredVehicleTypes { get; set; } = [];

    public List<string> PreferredFuelTypes { get; set; } = [];

    public List<string> DesiredFeatures { get; set; } = [];
}