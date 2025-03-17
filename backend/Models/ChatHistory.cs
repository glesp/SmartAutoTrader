using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartAutoTrader.API.Models
{
    public class ChatHistory
    {
        [Key]
        public int Id { get; set; }
        
        public int UserId { get; set; }
        
        [ForeignKey("UserId")]
        public User User { get; set; }
        
        [Required]
        public string UserMessage { get; set; }
        
        [Required]
        public string AIResponse { get; set; }
        
        public DateTime Timestamp { get; set; }
    }
}