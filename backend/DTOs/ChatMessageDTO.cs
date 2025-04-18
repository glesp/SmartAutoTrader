namespace SmartAutoTrader.API.DTOs;

public class ChatMessageDto
{
    public string? Content { get; set; }

    public bool IsClarification { get; set; }

    public string? OriginalUserInput { get; set; }

    public bool IsFollowUp { get; set; }

    public string? ConversationId { get; set; }
}