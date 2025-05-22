/* <copyright file="RecommendationParameters.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the RecommendationParameters class, which encapsulates user preferences and context for generating vehicle recommendations in the Smart Auto Trader application.
</summary>
<remarks>
The RecommendationParameters class is used to pass user-defined preferences, such as price range, vehicle types, and desired features, to the recommendation service. It also tracks rejected options and clarification needs to refine the recommendation process dynamically.
</remarks>
<dependencies>
- SmartAutoTrader.API.Enums
</dependencies>
 */

namespace SmartAutoTrader.API.Models
{
    using SmartAutoTrader.API.Enums;

    /// <summary>
    /// Represents user preferences and context for generating vehicle recommendations.
    /// </summary>
    /// <remarks>
    /// This class encapsulates various parameters, such as price range, vehicle types, and desired features, to guide the recommendation service in providing personalized results.
    /// </remarks>
    public class RecommendationParameters
    {
        /// <summary>
        /// Gets or sets the minimum price for the vehicle search.
        /// </summary>
        public decimal? MinPrice { get; set; }

        /// <summary>
        /// Gets or sets the maximum price for the vehicle search.
        /// </summary>
        public decimal? MaxPrice { get; set; }

        /// <summary>
        /// Gets or sets the minimum year of manufacture for the vehicle search.
        /// </summary>
        public int? MinYear { get; set; }

        /// <summary>
        /// Gets or sets the maximum year of manufacture for the vehicle search.
        /// </summary>
        public int? MaxYear { get; set; }

        /// <summary>
        /// Gets or sets the maximum mileage for the vehicle search.
        /// </summary>
        public int? MaxMileage { get; set; }

        /// <summary>
        /// Gets or sets the list of preferred fuel types for the vehicle search.
        /// </summary>
        public List<FuelType> PreferredFuelTypes { get; set; } = new();

        /// <summary>
        /// Gets or sets the list of preferred vehicle types for the vehicle search.
        /// </summary>
        public List<VehicleType> PreferredVehicleTypes { get; set; } = new();

        /// <summary>
        /// Gets or sets the list of preferred vehicle makes for the vehicle search.
        /// </summary>
        public List<string> PreferredMakes { get; set; } = new();

        /// <summary>
        /// Gets or sets the list of desired features for the vehicle search.
        /// </summary>
        public List<string> DesiredFeatures { get; set; } = new();

        /// <summary>
        /// Gets or sets the preferred transmission type for the vehicle search.
        /// </summary>
        public TransmissionType? Transmission { get; set; }

        /// <summary>
        /// Gets or sets the minimum engine size (in liters) for the vehicle search.
        /// </summary>
        public double? MinEngineSize { get; set; }

        /// <summary>
        /// Gets or sets the maximum engine size (in liters) for the vehicle search.
        /// </summary>
        public double? MaxEngineSize { get; set; }

        /// <summary>
        /// Gets or sets the minimum horsepower for the vehicle search.
        /// </summary>
        public int? MinHorsePower { get; set; }

        /// <summary>
        /// Gets or sets the maximum horsepower for the vehicle search.
        /// </summary>
        public int? MaxHorsePower { get; set; }

        /// <summary>
        /// Gets or sets the text prompt provided by the user for contextual recommendations.
        /// </summary>
        public string? TextPrompt { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of results to return in the recommendation.
        /// </summary>
        public int? MaxResults { get; set; } = 5;

        /// <summary>
        /// Gets or sets a value indicating whether the user's query is off-topic.
        /// </summary>
        public bool IsOffTopic { get; set; }

        /// <summary>
        /// Gets or sets the response to be shown if the query is off-topic.
        /// </summary>
        public string? OffTopicResponse { get; set; }

        /// <summary>
        /// Gets or sets the suggestion provided by the retriever for refining the query.
        /// </summary>
        public string? RetrieverSuggestion { get; set; }

        /// <summary>
        /// Gets or sets the AI model used for generating recommendations.
        /// </summary>
        public string? ModelUsed { get; set; }

        /// <summary>
        /// Gets or sets the detected intent of the user's query.
        /// </summary>
        public string? Intent { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether clarification is needed from the user.
        /// </summary>
        public bool ClarificationNeeded { get; set; }

        /// <summary>
        /// Gets or sets the list of parameters requiring clarification.
        /// </summary>
        public List<string> ClarificationNeededFor { get; set; } = new();

        /// <summary>
        /// Gets or sets the list of explicitly negated vehicle makes.
        /// </summary>
        public List<string> ExplicitlyNegatedMakes { get; set; } = new();

        /// <summary>
        /// Gets or sets the list of explicitly negated vehicle types.
        /// </summary>
        public List<VehicleType> ExplicitlyNegatedVehicleTypes { get; set; } = new();

        /// <summary>
        /// Gets or sets the list of explicitly negated fuel types.
        /// </summary>
        public List<FuelType> ExplicitlyNegatedFuelTypes { get; set; } = new();

        /// <summary>
        /// Gets or sets the list of rejected vehicle makes for filtering.
        /// </summary>
        public List<string> RejectedMakes { get; set; } = new();

        /// <summary>
        /// Gets or sets the list of rejected vehicle types for filtering.
        /// </summary>
        public List<VehicleType> RejectedVehicleTypes { get; set; } = new();

        /// <summary>
        /// Gets or sets the list of rejected fuel types for filtering.
        /// </summary>
        public List<FuelType> RejectedFuelTypes { get; set; } = new();

        /// <summary>
        /// Gets or sets the list of rejected features for filtering.
        /// </summary>
        public List<string> RejectedFeatures { get; set; } = new();

        /// <summary>
        /// Gets or sets the rejected transmission type for filtering.
        /// </summary>
        public TransmissionType? RejectedTransmission { get; set; }
    }
}