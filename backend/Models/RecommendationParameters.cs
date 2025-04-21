// <copyright file="RecommendationParameters.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SmartAutoTrader.API.Models
{
    using SmartAutoTrader.API.Enums;

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

        // Transmission preference
        public TransmissionType? Transmission { get; set; }

        // Engine size range in liters
        public double? MinEngineSize { get; set; }
        
        public double? MaxEngineSize { get; set; }

        // Horsepower range
        public int? MinHorsePower { get; set; }
        
        public int? MaxHorsePower { get; set; }

        public string? TextPrompt { get; set; }

        public int? MaxResults { get; set; } = 5;

        public bool IsOffTopic { get; set; }

        public string? OffTopicResponse { get; set; }

        public string? RetrieverSuggestion { get; set; }

        public string? ModelUsed { get; set; }

        public string? Intent { get; set; }

        public List<string> ClarificationNeededFor { get; set; } = [];
    }
}