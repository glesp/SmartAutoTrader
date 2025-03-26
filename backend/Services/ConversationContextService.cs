using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartAutoTrader.API.Data;
using SmartAutoTrader.API.Models;

namespace SmartAutoTrader.API.Services
{
    public interface IConversationContextService
    {
        Task<ConversationContext> GetOrCreateContextAsync(int userId);
        Task UpdateContextAsync(int userId, ConversationContext context);
        Task<ConversationSession> StartNewSessionAsync(int userId);
        Task<ConversationSession> GetCurrentSessionAsync(int userId);
    }

    // This class represents the state we want to track throughout a conversation
    public class ConversationContext
    {
        // Current search parameters
        public RecommendationParameters CurrentParameters { get; set; } = new RecommendationParameters();
        
        // Track conversation flow
        public int MessageCount { get; set; } = 0;
        public DateTime LastInteraction { get; set; } = DateTime.UtcNow;
        
        // Track user intent and context
        public string LastUserIntent { get; set; } = string.Empty;
        public List<string> MentionedVehicleFeatures { get; set; } = new List<string>();
        public List<string> ExplicitlyRejectedOptions { get; set; } = new List<string>();
        
        // Track active conversation topics
        public Dictionary<string, object> TopicContext { get; set; } = new Dictionary<string, object>();
        
        // Track recommendations shown to the user
        public List<int> ShownVehicleIds { get; set; } = new List<int>();
    }

    public class ConversationContextService : IConversationContextService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ConversationContextService> _logger;
        
        // In-memory cache for active conversations (optional)
        private readonly Dictionary<int, ConversationContext> _activeContexts = new Dictionary<int, ConversationContext>();
        
        public ConversationContextService(ApplicationDbContext context, ILogger<ConversationContextService> logger)
        {
            _context = context;
            _logger = logger;
        }
        
        public async Task<ConversationContext> GetOrCreateContextAsync(int userId)
        {
            try
            {
                // Check if we have an active session for the user
                var session = await GetCurrentSessionAsync(userId);
                
                if (session != null)
                {
                    // Check in-memory cache first for performance
                    if (_activeContexts.TryGetValue(userId, out var cachedContext))
                    {
                        return cachedContext;
                    }
                    
                    // Try to deserialize the context from the session
                    if (!string.IsNullOrEmpty(session.SessionContext))
                    {
                        try
                        {
                            var context = JsonSerializer.Deserialize<ConversationContext>(session.SessionContext);
                            if (context != null)
                            {
                                // Cache for subsequent requests
                                _activeContexts[userId] = context;
                                return context;
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogError(ex, "Error deserializing conversation context for user {UserId}", userId);
                        }
                    }
                }
                
                // Create a new context if we couldn't retrieve one
                var newContext = new ConversationContext
                {
                    LastInteraction = DateTime.UtcNow
                };
                
                // Start a new session if needed
                if (session == null)
                {
                    await StartNewSessionAsync(userId);
                }
                
                // Cache and return the new context
                _activeContexts[userId] = newContext;
                return newContext;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving conversation context for user {UserId}", userId);
                return new ConversationContext();
            }
        }
        
        public async Task UpdateContextAsync(int userId, ConversationContext context)
        {
            try
            {
                // Update in-memory cache
                _activeContexts[userId] = context;
                
                // Update the timestamp
                context.LastInteraction = DateTime.UtcNow;
                
                // Get the current session
                var session = await GetCurrentSessionAsync(userId);
                
                if (session == null)
                {
                    // Create a new session if one doesn't exist
                    session = await StartNewSessionAsync(userId);
                }
                
                // Serialize and save the context
                session.SessionContext = JsonSerializer.Serialize(context);
                session.LastInteractionAt = DateTime.UtcNow;
                
                _context.Entry(session).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating conversation context for user {UserId}", userId);
            }
        }
        
        public async Task<ConversationSession> StartNewSessionAsync(int userId)
        {
            var newSession = new ConversationSession
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                LastInteractionAt = DateTime.UtcNow,
                SessionContext = JsonSerializer.Serialize(new ConversationContext())
            };
            
            _context.ConversationSessions.Add(newSession);
            await _context.SaveChangesAsync();
            
            // Clear any cached context
            _activeContexts.Remove(userId);
            
            return newSession;
        }
        
        public async Task<ConversationSession> GetCurrentSessionAsync(int userId)
        {
            // Get the most recent active session (less than 30 minutes old)
            var thirtyMinutesAgo = DateTime.UtcNow.AddMinutes(-30);
            
            return await _context.ConversationSessions
                .Where(s => s.UserId == userId && s.LastInteractionAt > thirtyMinutesAgo)
                .OrderByDescending(s => s.LastInteractionAt)
                .FirstOrDefaultAsync();
        }
    }
}