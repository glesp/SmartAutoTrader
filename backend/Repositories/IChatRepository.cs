// <copyright file="IChatRepository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SmartAutoTrader.API.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using SmartAutoTrader.API.Data;
    using SmartAutoTrader.API.Models;

    public interface IChatRepository
    {
        Task AddChatHistoryAsync(ChatHistory chatHistory);

        Task UpdateSessionAsync(ConversationSession session);

        Task AddSessionAsync(ConversationSession session);

        Task<ConversationSession?> GetRecentSessionAsync(int userId, TimeSpan maxAge);

        Task<List<ConversationTurn>> GetRecentHistoryAsync(int userId, int conversationSessionId, int maxItems = 3);

        Task SaveChangesAsync();
    }

    public class ChatRepository(ApplicationDbContext context) : IChatRepository
    {
        private readonly ApplicationDbContext context = context;

        /// <inheritdoc/>
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
    }
}