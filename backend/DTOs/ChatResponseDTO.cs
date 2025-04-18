namespace SmartAutoTrader.API.DTOs;

using SmartAutoTrader.API.Controllers;
using SmartAutoTrader.API.Models;

public class ChatResponseDto
{
    public string? Message { get; set; }

    public List<Vehicle> RecommendedVehicles { get; set; } = [];

    public RecommendationParametersDto? Parameters { get; set; }

    public bool ClarificationNeeded { get; set; }

    public string? OriginalUserInput { get; set; }

    public string? ConversationId { get; set; }
}