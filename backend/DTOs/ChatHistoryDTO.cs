namespace SmartAutoTrader.API.DTOs;

public class ChatHistoryDto
{
    public int Id { get; set; }

    public string? UserMessage { get; set; }

    public string? AIResponse { get; set; }

    public string? Timestamp { get; set; }

    public string? ConversationId { get; set; }
}