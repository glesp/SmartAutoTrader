using System.ComponentModel.DataAnnotations;

namespace SmartAutoTrader.API.Models;

public class InquiryCreateDto
{
    public int VehicleId { get; set; }

    [Required]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;
}
