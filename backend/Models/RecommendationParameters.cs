namespace SmartAutoTrader.API.Models
{
    // Parameter class to pass user preferences and context to the recommendation service
    public class RecommendationParameters
    {
        public decimal? MinPrice { get; set; }

        public decimal? MaxPrice { get; set; }

        public int? MinYear { get; set; }

        public int? MaxYear { get; set; }

        public int? MaxMileage { get; set; }

        public List<FuelType> PreferredFuelTypes { get; set; } = [];

        public List<VehicleType> PreferredVehicleTypes { get; set; } = [];

        public List<string> PreferredMakes { get; set; } = [];

        public List<string> DesiredFeatures { get; set; } = [];

        public string? TextPrompt { get; set; }

        public int? MaxResults { get; set; } = 5;

        public bool IsOffTopic { get; set; }

        public string? OffTopicResponse { get; set; }

        public string? RetrieverSuggestion { get; set; }

        public string? ModelUsed { get; set; }
    }

    // Interface for any AI recommendation service (allows easy swapping)
    public interface IAIRecommendationService
    {
        Task<IEnumerable<Vehicle>> GetRecommendationsAsync(int userId, RecommendationParameters parameters);
    }
}