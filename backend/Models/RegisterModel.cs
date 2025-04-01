// Models/RegisterModel.cs
namespace SmartAutoTrader.API.Models
{
    public class RegisterModel
    {
        public string Username { get; set; } = null!;
        
        public string Email { get; set; } = null!;
        
        public string Password { get; set; } = null!;
        
        public string FirstName { get; set; } = null!;
        
        public string LastName { get; set; } = null!;
        
        public string PhoneNumber { get; set; } = null!;
    }
}