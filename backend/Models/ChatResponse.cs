// <copyright file="ChatResponse.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SmartAutoTrader.API.Models
{
    public class ChatResponse
    {
        public string? Message { get; set; }

        public List<Vehicle> RecommendedVehicles { get; set; } = [];

        public RecommendationParameters? UpdatedParameters { get; set; }

        public bool ClarificationNeeded { get; set; }

        public string? OriginalUserInput { get; set; }

        public string? ConversationId { get; set; }

        public string? MatchedCategory { get; set; }
    }
}