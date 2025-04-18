namespace SmartAutoTrader.API.Models;

public class ConversationSession
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime LastInteractionAt { get; set; }

    public string? SessionContext { get; set; } // JSON string to store conversation context

    public virtual User? User { get; set; }

    public virtual ICollection<ChatHistory>? Messages { get; set; }
}