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

        Task SaveChangesAsync();
    }

    public class ChatRepository(ApplicationDbContext context) : IChatRepository
    {
        private readonly ApplicationDbContext _context = context;

        public Task AddChatHistoryAsync(ChatHistory chatHistory)
        {
            _ = _context.ChatHistory.Add(chatHistory);
            return Task.CompletedTask;
        }

        public Task UpdateSessionAsync(ConversationSession session)
        {
            _context.Entry(session).State = EntityState.Modified;
            return _context.SaveChangesAsync();
        }

        public Task AddSessionAsync(ConversationSession session)
        {
            _ = _context.ConversationSessions.Add(session);
            return Task.CompletedTask;
        }

        public Task<ConversationSession?> GetRecentSessionAsync(int userId, TimeSpan maxAge)
        {
            DateTime since = DateTime.UtcNow.Subtract(maxAge);

            return _context.ConversationSessions
                .Where(s => s.UserId == userId && s.LastInteractionAt > since)
                .OrderByDescending(s => s.LastInteractionAt)
                .FirstOrDefaultAsync();
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}