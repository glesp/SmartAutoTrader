namespace SmartAutoTrader.API.DTOs
{
    using System.ComponentModel.DataAnnotations;

    public class VehicleFeatureDTO
    {
        [Required(ErrorMessage = "Feature name is required.")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Feature name must be between 1 and 100 characters.")]
        public string Name { get; set; } = string.Empty;
    }
}