/* <copyright file="ChatMessageDTO.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the ChatMessageDto class, which serves as a Data Transfer Object (DTO) for representing chat messages in the Smart Auto Trader application.
</summary>
<remarks>
The ChatMessageDto class is used to encapsulate data related to individual chat messages exchanged between users and the AI assistant. It includes properties for storing the message content, metadata about the message (e.g., whether it is a clarification or follow-up), and the conversation identifier. This DTO is typically used for transferring chat message data between the backend and frontend or other application layers.
</remarks>
<dependencies>
- None
</dependencies>
 */

namespace SmartAutoTrader.API.DTOs
{
    /// <summary>
    /// Represents a Data Transfer Object (DTO) for chat messages.
    /// </summary>
    /// <remarks>
    /// This class is used to encapsulate data for individual chat messages, including the message content, metadata, and conversation identifiers. It is designed for use in API requests or other data transfer scenarios.
    /// </remarks>
    public class ChatMessageDto
    {
        /// <summary>
        /// Gets or sets the content of the chat message.
        /// </summary>
        /// <value>A string containing the content of the user's message.</value>
        /// <example>"What is the price of the Tesla Model 3?".</example>
        public string? Content { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the message is a clarification of a previous message.
        /// </summary>
        /// <value>A boolean indicating if the message is a clarification.</value>
        /// <example>true.</example>
        public bool IsClarification { get; set; }

        /// <summary>
        /// Gets or sets the original user input that this message clarifies, if applicable.
        /// </summary>
        /// <value>A string containing the original user input, or null if not applicable.</value>
        /// <example>"I meant the Tesla Model Y, not the Model 3.".</example>
        public string? OriginalUserInput { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the message is a follow-up to a previous message.
        /// </summary>
        /// <value>A boolean indicating if the message is a follow-up.</value>
        /// <example>false.</example>
        public bool IsFollowUp { get; set; }

        /// <summary>
        /// Gets or sets the identifier for the conversation to which this message belongs.
        /// </summary>
        /// <value>A string representing the unique ID of the conversation.</value>
        /// <example>"abc123-conversation-id".</example>
        public string? ConversationId { get; set; }
    }
}