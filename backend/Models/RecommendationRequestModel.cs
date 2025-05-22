/* <copyright file="RecommendationRequestModel.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the RecommendationRequestModel class, which represents the data structure for user requests to generate vehicle recommendations in the Smart Auto Trader application.
</summary>
<remarks>
The RecommendationRequestModel class is used to encapsulate user-defined preferences, such as price range, vehicle types, and desired features, for generating vehicle recommendations. It serves as the input model for recommendation services, ensuring that user preferences are properly structured and passed to the backend.
</remarks>
<dependencies>
- SmartAutoTrader.API.Enums
</dependencies>
 */

namespace SmartAutoTrader.API.Models
{
    using SmartAutoTrader.API.Enums;

    /// <summary>
    /// Represents the data structure for user requests to generate vehicle recommendations.
    /// </summary>
    /// <remarks>
    /// This class encapsulates user-defined preferences, such as price range, vehicle types, and desired features, to guide the recommendation service in providing personalized results.
    /// </remarks>
    public class RecommendationRequestModel
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
        /// Gets or sets the list of preferred fuel types for the vehicle search.
        /// </summary>
        public List<FuelType>? FuelTypes { get; set; }

        /// <summary>
        /// Gets or sets the list of preferred vehicle types for the vehicle search.
        /// </summary>
        public List<VehicleType>? VehicleTypes { get; set; }

        /// <summary>
        /// Gets or sets the list of preferred vehicle makes for the vehicle search.
        /// </summary>
        public List<string>? Makes { get; set; }

        /// <summary>
        /// Gets or sets the list of desired features for the vehicle search.
        /// </summary>
        public List<string>? Features { get; set; }

        /// <summary>
        /// Gets or sets the text prompt provided by the user for contextual recommendations.
        /// </summary>
        public string? TextPrompt { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of results to return in the recommendation.
        /// </summary>
        public int? MaxResults { get; set; }
    }
}