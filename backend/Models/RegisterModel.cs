// Models/RegisterModel.cs

using System.ComponentModel.DataAnnotations;

namespace SmartAutoTrader.API.Models
{
    public class RegisterModel
    {
        [Required]
        public string Username { get; set; } = default!;

        [Required] [EmailAddress] public string Email { get; set; } = default!;

        [Required] public string Password { get; set; } = default!;

        [Required]
        public string FirstName { get; set; } = default!;

        [Required] public string LastName { get; set; } = default!;

        [Required] public string PhoneNumber { get; set; } = default!;
    }
}