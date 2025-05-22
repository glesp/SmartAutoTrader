/* <copyright file="ChatMessage.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the ChatMessage class, which represents a user message in a conversation within the Smart Auto Trader application.
</summary>
<remarks>
The ChatMessage class is used to encapsulate details about a user's message during a conversation, including metadata such as timestamps, whether the message is a clarification or follow-up, and the associated conversation ID. This class is typically used for managing and processing user input in chat-based recommendation services.
</remarks>
<dependencies>
- None
</dependencies>
 */

namespace SmartAutoTrader.API.Models
{
    /// <summary>
    /// Represents a user message in a conversation.
    /// </summary>
    /// <remarks>
    /// This class encapsulates the content of the user's message, metadata such as timestamps, and flags indicating whether the message is a clarification or follow-up.
    /// </remarks>
    public class ChatMessage
    {
        /// <summary>
        /// Gets or sets the content of the user's message.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp of when the message was sent.
        /// </summary>
        /// <remarks>
        /// Defaults to the current UTC time when the message is created.
        /// </remarks>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets a value indicating whether the message is a clarification request.
        /// </summary>
        /// <remarks>
        /// If true, the message is intended to clarify a previous interaction.
        /// </remarks>
        public bool IsClarification { get; set; }

        /// <summary>
        /// Gets or sets the original user input, if the message is a clarification or follow-up.
        /// </summary>
        /// <remarks>
        /// This property is used to track the original input that the user is clarifying or following up on.
        /// </remarks>
        public string? OriginalUserInput { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the message is a follow-up to a previous interaction.
        /// </summary>
        public bool IsFollowUp { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the conversation associated with this message.
        /// </summary>
        public string? ConversationId { get; set; }
    }
}