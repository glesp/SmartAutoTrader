/* <copyright file="ChatResponseDTO.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the ChatResponseDto class, which serves as a Data Transfer Object (DTO) for encapsulating responses from the chat system in the Smart Auto Trader application.
</summary>
<remarks>
The ChatResponseDto class is used to structure data returned by the chat system, including the AI-generated message, recommended vehicles, parameters used for recommendations, and metadata such as whether clarification is needed. This DTO is typically used for transferring chat response data between the backend and frontend or other application layers.
</remarks>
<dependencies>
- SmartAutoTrader.API.Controllers
- SmartAutoTrader.API.Models
</dependencies>
 */

namespace SmartAutoTrader.API.DTOs
{
    using SmartAutoTrader.API.Models;

    /// <summary>
    /// Represents a Data Transfer Object (DTO) for chat responses.
    /// </summary>
    /// <remarks>
    /// This class encapsulates the response data from the chat system, including the AI-generated message, recommended vehicles, and additional metadata. It is designed for use in API responses or other data transfer scenarios.
    /// </remarks>
    public class ChatResponseDto
    {
        /// <summary>
        /// Gets or sets the AI-generated message in the chat response.
        /// </summary>
        /// <value>A string containing the AI's response message.</value>
        /// <example>"Here are some vehicles that match your preferences.".</example>
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets the list of vehicles recommended by the AI.
        /// </summary>
        /// <value>A list of <see cref="Vehicle"/> objects representing the recommended vehicles.</value>
        /// <example>
        /// [
        ///     { "Make": "Tesla", "Model": "Model 3", "Year": 2023 },
        ///     { "Make": "Toyota", "Model": "Camry", "Year": 2022 }
        /// ].
        /// </example>
        public List<Vehicle> RecommendedVehicles { get; set; } = [];

        /// <summary>
        /// Gets or sets the parameters used for generating the recommendations.
        /// </summary>
        /// <value>A <see cref="RecommendationParametersDto"/> object containing the recommendation parameters.</value>
        /// <example>
        /// { "MinPrice": 20000, "MaxPrice": 50000, "VehicleType": "SUV" }.
        /// </example>
        public RecommendationParametersDto? Parameters { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether clarification is needed for the user's input.
        /// </summary>
        /// <value>A boolean indicating if clarification is required.</value>
        /// <example>true.</example>
        public bool ClarificationNeeded { get; set; }

        /// <summary>
        /// Gets or sets the original user input that requires clarification, if applicable.
        /// </summary>
        /// <value>A string containing the original user input, or null if not applicable.</value>
        /// <example>"I want a car with good mileage.".</example>
        public string? OriginalUserInput { get; set; }

        /// <summary>
        /// Gets or sets the identifier for the conversation to which this response belongs.
        /// </summary>
        /// <value>A string representing the unique ID of the conversation.</value>
        /// <example>"abc123-conversation-id".</example>
        public string? ConversationId { get; set; }

        /// <summary>
        /// Gets or sets the category that matched the user's input, if applicable.
        /// </summary>
        /// <value>A string representing the matched category, or null if no category was matched.</value>
        /// <example>"Electric Vehicles".</example>
        public string? MatchedCategory { get; set; }
    }
}