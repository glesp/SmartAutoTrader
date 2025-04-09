namespace SmartAutoTrader.API.Models
{
    using System.ComponentModel.DataAnnotations;
    using SmartAutoTrader.API.Enums;

    public class Inquiry
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        public User? User { get; set; }

        public int VehicleId { get; set; }

        public Vehicle? Vehicle { get; set; }

        [Required]
        public string Subject { get; set; } = null!;

        [Required]
        public string Message { get; set; } = null!;

        public string? Response { get; set; }

        public DateTime DateSent { get; set; } = DateTime.Now;

        public DateTime? DateReplied { get; set; }

        public InquiryStatus Status { get; set; } = InquiryStatus.New;
    }
}