// Models/RegisterModel.cs
namespace SmartAutoTrader.API.Models
{
    public class RegisterModel
    {
        public string? Username { get; set; }
        
        public string? Email { get; set; }
        
        public string? Password { get; set; } 
        
        public string? FirstName { get; set; } 
        
        public string? LastName { get; set; } 
        
        public string? PhoneNumber { get; set; }
    }
}