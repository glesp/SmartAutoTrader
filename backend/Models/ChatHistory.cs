using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartAutoTrader.API.Models;

public class ChatHistory
{
    [Key] public int Id { get; set; }

    public int UserId { get; set; }

    [ForeignKey("UserId")] public User User { get; set; }

    [Required] public string UserMessage { get; set; }

    [Required] public string AIResponse { get; set; }

    public DateTime Timestamp { get; set; }
    public int? ConversationSessionId { get; set; }
    public virtual ConversationSession Session { get; set; }
}

public class ConversationSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastInteractionAt { get; set; }
    public string SessionContext { get; set; } // JSON string to store conversation context
    public virtual User User { get; set; }
    public virtual ICollection<ChatHistory> Messages { get; set; }
}