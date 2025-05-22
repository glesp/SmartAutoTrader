namespace SmartAutoTrader.API.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using SmartAutoTrader.API.Data;
    using SmartAutoTrader.API.Models;

    /// <summary>
    /// Repository interface for managing chat conversations and history.
    /// </summary>
    public interface IChatRepository
    {
        /// <summary>
        /// Adds a new chat history entry to the database.
        /// </summary>
        /// <param name="chatHistory">The chat history entry to add.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        Task AddChatHistoryAsync(ChatHistory chatHistory);

        /// <summary>
        /// Updates an existing conversation session in the database.
        /// </summary>
        /// <param name="session">The session to update.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        Task UpdateSessionAsync(ConversationSession session);

        /// <summary>
        /// Adds a new conversation session to the database.
        /// </summary>
        /// <param name="session">The session to add.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        Task AddSessionAsync(ConversationSession session);

        /// <summary>
        /// Retrieves the most recent conversation session for a user.
        /// </summary>
        /// <param name="userId">ID of the user.</param>
        /// <param name="maxAge">Maximum age of the session to be considered recent.</param>
        /// <returns>The most recent conversation session if found, otherwise null.</returns>
        Task<ConversationSession?> GetRecentSessionAsync(int userId, TimeSpan maxAge);

        /// <summary>
        /// Retrieves recent conversation history for a user within a specific session.
        /// </summary>
        /// <param name="userId">ID of the user.</param>
        /// <param name="conversationSessionId">ID of the conversation session.</param>
        /// <param name="maxItems">Maximum number of items to retrieve.</param>
        /// <returns>List of recent conversation turns.</returns>
        Task<List<ConversationTurn>> GetRecentHistoryAsync(int userId, int conversationSessionId, int maxItems = 3);

        /// <summary>
        /// Saves changes to the database.
        /// </summary>
        /// <returns>Task representing the asynchronous operation.</returns>
        Task SaveChangesAsync();

        /// <summary>
        /// Retrieves a specific conversation session by its ID.
        /// </summary>
        /// <param name="sessionId">ID of the session to retrieve.</param>
        /// <param name="userId">ID of the user who owns the session.</param>
        /// <returns>The requested conversation session if found, otherwise null.</returns>
        Task<ConversationSession?> GetSessionByIdAsync(int sessionId, int userId);
    }

    public class ChatRepository : IChatRepository
    {
        private readonly ApplicationDbContext context;

        public ChatRepository(ApplicationDbContext context)
        {
            this.context = context;
        }

        public Task AddChatHistoryAsync(ChatHistory chatHistory)
        {
            _ = this.context.ChatHistory.Add(chatHistory);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task UpdateSessionAsync(ConversationSession session)
        {
            this.context.Entry(session).State = EntityState.Modified;
            return this.context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public Task AddSessionAsync(ConversationSession session)
        {
            _ = this.context.ConversationSessions.Add(session);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<List<ConversationTurn>> GetRecentHistoryAsync(
        int userId,
        int conversationSessionId,
        int maxItems = 3)
        {
            return await this.context.ChatHistory
                .Where(h => h.UserId == userId && h.ConversationSessionId == conversationSessionId)
                .OrderByDescending(h => h.Timestamp)
                .Take(maxItems)
                .Select(h => new ConversationTurn
                {
                    UserMessage = h.UserMessage,
                    AIResponse = h.AIResponse,
                    Timestamp = h.Timestamp,
                })
                .ToListAsync();
        }

        /// <inheritdoc/>
        public Task<ConversationSession?> GetRecentSessionAsync(int userId, TimeSpan maxAge)
        {
            DateTime since = DateTime.UtcNow.Subtract(maxAge);

            return this.context.ConversationSessions
                .Where(s => s.UserId == userId && s.LastInteractionAt > since)
                .OrderByDescending(s => s.LastInteractionAt)
                .FirstOrDefaultAsync();
        }

        /// <inheritdoc/>
        public Task SaveChangesAsync()
        {
            return this.context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task<ConversationSession?> GetSessionByIdAsync(int sessionId, int userId)
        {
            return await this.context.ConversationSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);
        }
    }
}