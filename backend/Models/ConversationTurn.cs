// <copyright file="ConversationTurn.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SmartAutoTrader.API.Models
{
    public class ConversationTurn
    {
        public string UserMessage { get; set; } = string.Empty;

        public string AIResponse { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}