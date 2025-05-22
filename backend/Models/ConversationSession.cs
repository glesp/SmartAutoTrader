/* <copyright file="ConversationSession.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the ConversationSession class, which represents a user's conversation session in the Smart Auto Trader application.
</summary>
<remarks>
The ConversationSession class is used to track and manage user interactions during a conversation. It includes metadata such as the session's creation time, last interaction time, and the serialized context of the conversation. This class is typically used for maintaining session continuity, analytics, and debugging purposes.
</remarks>
<dependencies>
- None
</dependencies>
 */

namespace SmartAutoTrader.API.Models;

/// <summary>
/// Represents a user's conversation session.
/// </summary>
/// <remarks>
/// This class tracks metadata about a conversation session, including timestamps, the serialized context, and associated user and chat history.
/// </remarks>
public class ConversationSession
{
    /// <summary>
    /// Gets or sets the unique identifier for the conversation session.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the user associated with this conversation session.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the conversation session was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last interaction in the conversation session.
    /// </summary>
    public DateTime LastInteractionAt { get; set; }

    /// <summary>
    /// Gets or sets the serialized context of the conversation session.
    /// </summary>
    /// <remarks>
    /// This property stores the conversation context as a JSON string for persistence and retrieval.
    /// </remarks>
    public string? SessionContext { get; set; }

    /// <summary>
    /// Gets or sets the user associated with this conversation session.
    /// </summary>
    /// <remarks>
    /// This property establishes a relationship between the conversation session and the user entity.
    /// </remarks>
    public virtual User? User { get; set; }

    /// <summary>
    /// Gets or sets the collection of chat history messages associated with this conversation session.
    /// </summary>
    /// <remarks>
    /// This property establishes a relationship between the conversation session and its chat history.
    /// </remarks>
    public virtual ICollection<ChatHistory>? Messages { get; set; }
}