/* <copyright file="ConversationContext.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the ConversationContext class, which represents the state and context of a user conversation in the Smart Auto Trader application.
</summary>
<remarks>
The ConversationContext class is used to track and manage the state of a user conversation, including search parameters, user intent, clarification attempts, and recommendations shown. It facilitates dynamic query refinement, loop detection, and personalized recommendations by maintaining a detailed record of the conversation flow and user preferences.
</remarks>
<dependencies>
- SmartAutoTrader.API.Enums
</dependencies>
 */

namespace SmartAutoTrader.API.Models;

using SmartAutoTrader.API.Enums;

/// <summary>
/// Represents the state and context of a user conversation.
/// </summary>
/// <remarks>
/// This class tracks various aspects of a user conversation, including search parameters, user intent, clarification attempts, and recommendations shown. It is designed to support dynamic query refinement and personalized recommendations.
/// </remarks>
public class ConversationContext
{
    /// <summary>
    /// Gets or sets the current search parameters for the conversation.
    /// </summary>
    public RecommendationParameters CurrentParameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the total number of messages exchanged in the conversation.
    /// </summary>
    public int MessageCount { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last interaction in the conversation.
    /// </summary>
    public DateTime LastInteraction { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the number of consecutive clarification attempts made during the conversation.
    /// </summary>
    public int ConsecutiveClarificationAttempts { get; set; } = 0;

    /// <summary>
    /// Gets or sets the list of recently asked clarification parameters.
    /// </summary>
    public List<string> RecentClarificationParameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of the last few questions asked by the AI.
    /// </summary>
    public List<string> LastQuestionsAsked { get; set; } = new();

    /// <summary>
    /// Gets or sets the last detected user intent in the conversation.
    /// </summary>
    public string LastUserIntent { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of vehicle features mentioned by the user.
    /// </summary>
    public List<string> MentionedVehicleFeatures { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of options explicitly rejected by the user.
    /// </summary>
    public List<string> ExplicitlyRejectedOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets the active conversation topics and their associated context.
    /// </summary>
    public Dictionary<string, object> TopicContext { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of vehicle IDs that have been shown to the user.
    /// </summary>
    public List<int> ShownVehicleIds { get; set; } = new();

    /// <summary>
    /// Gets or sets the AI model used for generating recommendations in the conversation.
    /// </summary>
    public string? ModelUsed { get; set; }

    /// <summary>
    /// Gets or sets the confirmed minimum price for the user's search criteria.
    /// </summary>
    public decimal? ConfirmedMinPrice { get; set; }

    /// <summary>
    /// Gets or sets the confirmed maximum price for the user's search criteria.
    /// </summary>
    public decimal? ConfirmedMaxPrice { get; set; }

    /// <summary>
    /// Gets or sets the confirmed minimum year for the user's search criteria.
    /// </summary>
    public int? ConfirmedMinYear { get; set; }

    /// <summary>
    /// Gets or sets the confirmed maximum year for the user's search criteria.
    /// </summary>
    public int? ConfirmedMaxYear { get; set; }

    /// <summary>
    /// Gets or sets the confirmed maximum mileage for the user's search criteria.
    /// </summary>
    public int? ConfirmedMaxMileage { get; set; }

    /// <summary>
    /// Gets or sets the list of confirmed makes for the user's search criteria.
    /// </summary>
    public List<string> ConfirmedMakes { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of confirmed models for the user's search criteria.
    /// </summary>
    public List<string> ConfirmedModels { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of confirmed vehicle types for the user's search criteria.
    /// </summary>
    public List<VehicleType> ConfirmedVehicleTypes { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of confirmed fuel types for the user's search criteria.
    /// </summary>
    public List<FuelType> ConfirmedFuelTypes { get; set; } = new();

    /// <summary>
    /// Gets or sets the confirmed transmission type for the user's search criteria.
    /// </summary>
    public TransmissionType? ConfirmedTransmission { get; set; }

    /// <summary>
    /// Gets or sets the list of confirmed features for the user's search criteria.
    /// </summary>
    public List<string> ConfirmedFeatures { get; set; } = new();

    /// <summary>
    /// Gets or sets the confirmed minimum engine size for the user's search criteria.
    /// </summary>
    public double? ConfirmedMinEngineSize { get; set; }

    /// <summary>
    /// Gets or sets the confirmed maximum engine size for the user's search criteria.
    /// </summary>
    public double? ConfirmedMaxEngineSize { get; set; }

    /// <summary>
    /// Gets or sets the confirmed minimum horsepower for the user's search criteria.
    /// </summary>
    public int? ConfirmedMinHorsePower { get; set; }

    /// <summary>
    /// Gets or sets the confirmed maximum horsepower for the user's search criteria.
    /// </summary>
    public int? ConfirmedMaxHorsePower { get; set; }

    /// <summary>
    /// Gets or sets the list of rejected makes for the user's search criteria.
    /// </summary>
    public List<string> RejectedMakes { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of rejected models for the user's search criteria.
    /// </summary>
    public List<string> RejectedModels { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of rejected vehicle types for the user's search criteria.
    /// </summary>
    public List<VehicleType> RejectedVehicleTypes { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of rejected fuel types for the user's search criteria.
    /// </summary>
    public List<FuelType> RejectedFuelTypes { get; set; } = new();

    /// <summary>
    /// Gets or sets the rejected transmission type for the user's search criteria.
    /// </summary>
    public TransmissionType? RejectedTransmission { get; set; }

    /// <summary>
    /// Gets or sets the list of rejected features for the user's search criteria.
    /// </summary>
    public List<string> RejectedFeatures { get; set; } = new();

    /// <summary>
    /// Gets or sets the last question asked by the AI during the conversation.
    /// </summary>
    public string? LastQuestionAskedByAI { get; set; }
}