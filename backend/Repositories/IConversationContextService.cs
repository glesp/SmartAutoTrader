namespace SmartAutoTrader.API.Repositories;

using SmartAutoTrader.API.Models;

public interface IConversationContextService
{
    Task<ConversationContext> GetOrCreateContextAsync(int userId);

    Task UpdateContextAsync(int userId, ConversationContext context);

    Task<ConversationSession> StartNewSessionAsync(int userId);

    Task<ConversationSession?> GetCurrentSessionAsync(int userId);
}