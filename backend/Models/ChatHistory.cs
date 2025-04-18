// <copyright file="ChatHistory.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SmartAutoTrader.API.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class ChatHistory
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required]
        public string UserMessage { get; set; } = null!;

        [Required]
        public string AIResponse { get; set; } = null!;

        public DateTime Timestamp { get; set; }

        public int? ConversationSessionId { get; set; }

        public virtual ConversationSession? Session { get; set; }
    }
}