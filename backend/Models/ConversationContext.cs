namespace SmartAutoTrader.API.Models;

using SmartAutoTrader.API.Enums;

public class ConversationContext
{
    // Current search parameters
    public RecommendationParameters CurrentParameters { get; set; } = new();

    // Track conversation flow
    public int MessageCount { get; set; }

    public DateTime LastInteraction { get; set; } = DateTime.UtcNow;

    // NEW: Track clarification attempts to prevent loops
    public int ConsecutiveClarificationAttempts { get; set; } = 0;
    
    // NEW: Track recently asked clarification parameters
    public List<string> RecentClarificationParameters { get; set; } = [];
    
    // NEW: Store the last few questions to enable more sophisticated loop detection
    public List<string> LastQuestionsAsked { get; set; } = [];

    // Track user intent and context
    public string LastUserIntent { get; set; } = string.Empty;

    public List<string> MentionedVehicleFeatures { get; set; } = [];

    public List<string> ExplicitlyRejectedOptions { get; set; } = [];

    // Track active conversation topics
    public Dictionary<string, object> TopicContext { get; set; } = [];

    // Track recommendations shown to the user
    public List<int> ShownVehicleIds { get; set; } = [];

    public string? ModelUsed { get; set; }

    // Confirmed criteria
    public decimal? ConfirmedMinPrice { get; set; }

    public decimal? ConfirmedMaxPrice { get; set; }

    public int? ConfirmedMinYear { get; set; }

    public int? ConfirmedMaxYear { get; set; }

    public int? ConfirmedMaxMileage { get; set; }

    public List<string> ConfirmedMakes { get; set; } = [];

    public List<string> ConfirmedModels { get; set; } = [];

    public List<VehicleType> ConfirmedVehicleTypes { get; set; } = [];

    public List<FuelType> ConfirmedFuelTypes { get; set; } = [];

    public TransmissionType? ConfirmedTransmission { get; set; }

    public List<string> ConfirmedFeatures { get; set; } = [];

    // Added engine size and horsepower confirmed criteria
    public double? ConfirmedMinEngineSize { get; set; }

    public double? ConfirmedMaxEngineSize { get; set; }

    public int? ConfirmedMinHorsePower { get; set; }

    public int? ConfirmedMaxHorsePower { get; set; }

    // Rejected criteria
    public List<string> RejectedMakes { get; set; } = [];

    public List<string> RejectedModels { get; set; } = [];

    public List<VehicleType> RejectedVehicleTypes { get; set; } = [];

    public List<FuelType> RejectedFuelTypes { get; set; } = [];

    public TransmissionType? RejectedTransmission { get; set; }

    public List<string> RejectedFeatures { get; set; } = [];

    // Question tracking
    public string? LastQuestionAskedByAI { get; set; }
}