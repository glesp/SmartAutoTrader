// <copyright file="ChatMessage.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SmartAutoTrader.API.Models
{
    public class ChatMessage
    {
        public string Content { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public bool IsClarification { get; set; }

        public string? OriginalUserInput { get; set; }

        public bool IsFollowUp { get; set; }

        public string? ConversationId { get; set; }
    }
}