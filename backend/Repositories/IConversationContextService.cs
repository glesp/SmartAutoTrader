/* <copyright file="IConversationContextService.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the IConversationContextService interface, which provides methods for managing conversation contexts and sessions in the Smart Auto Trader application.
</summary>
<remarks>
The IConversationContextService interface is designed to handle the lifecycle of conversation contexts and sessions for users interacting with the system. It includes methods for retrieving or creating conversation contexts, updating contexts, starting new sessions, and retrieving the current session. This interface is typically implemented by services that manage conversational state in the application.
</remarks>
<dependencies>
- SmartAutoTrader.API.Models
- System.Threading.Tasks
</dependencies>
 */

namespace SmartAutoTrader.API.Repositories
{
    using SmartAutoTrader.API.Models;

    /// <summary>
    /// Defines methods for managing conversation contexts and sessions.
    /// </summary>
    /// <remarks>
    /// This interface provides methods for retrieving, creating, and updating conversation contexts, as well as managing conversation sessions for users.
    /// </remarks>
    public interface IConversationContextService
    {
        /// <summary>
        /// Retrieves an existing conversation context for the specified user or creates a new one if none exists.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="ConversationContext"/> for the user.</returns>
        /// <example>
        /// <code>
        /// var context = await conversationContextService.GetOrCreateContextAsync(userId);
        /// </code>
        /// </example>
        Task<ConversationContext> GetOrCreateContextAsync(int userId);

        /// <summary>
        /// Updates the conversation context for the specified user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="context">The updated <see cref="ConversationContext"/> object.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <example>
        /// <code>
        /// await conversationContextService.UpdateContextAsync(userId, updatedContext);
        /// </code>
        /// </example>
        Task UpdateContextAsync(int userId, ConversationContext context);

        /// <summary>
        /// Starts a new conversation session for the specified user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the newly created <see cref="ConversationSession"/>.</returns>
        /// <example>
        /// <code>
        /// var newSession = await conversationContextService.StartNewSessionAsync(userId);
        /// </code>
        /// </example>
        Task<ConversationSession> StartNewSessionAsync(int userId);

        /// <summary>
        /// Retrieves the current conversation session for the specified user, if one exists.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the current <see cref="ConversationSession"/> for the user, or null if no session exists.
        /// </returns>
        /// <example>
        /// <code>
        /// var currentSession = await conversationContextService.GetCurrentSessionAsync(userId);
        /// if (currentSession != null)
        /// {
        ///     Console.WriteLine($"Current session ID: {currentSession.Id}");
        /// }
        /// </code>
        /// </example>
        Task<ConversationSession?> GetCurrentSessionAsync(int userId);
    }
}