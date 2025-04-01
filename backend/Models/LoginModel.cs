// Models/LoginModel.cs
namespace SmartAutoTrader.API.Models
{
    public class LoginModel
    {
        public string Email { get; set; } = null!;
        
        public string Password { get; set; } = null!;
    }
}