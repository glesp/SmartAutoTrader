namespace SmartAutoTrader.API.Models;

public class ConversationContext
{
    // Current search parameters
    public RecommendationParameters CurrentParameters { get; set; } = new();

    // Track conversation flow
    public int MessageCount { get; set; }

    public DateTime LastInteraction { get; set; } = DateTime.UtcNow;

    // Track user intent and context
    public string LastUserIntent { get; set; } = string.Empty;

    public List<string> MentionedVehicleFeatures { get; set; } = [];

    public List<string> ExplicitlyRejectedOptions { get; set; } = [];

    // Track active conversation topics
    public Dictionary<string, object> TopicContext { get; set; } = [];

    // Track recommendations shown to the user
    public List<int> ShownVehicleIds { get; set; } = [];

    public string? ModelUsed { get; set; }
}