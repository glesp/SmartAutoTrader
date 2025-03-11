using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SmartAutoTrader.API.Data;
using SmartAutoTrader.API.Models;
using BC = BCrypt.Net.BCrypt;

namespace SmartAutoTrader.API.Services
{
    public interface IAuthService
    {
        Task<User> RegisterAsync(string username, string email, string password, string firstName, string lastName, string phoneNumber);
        Task<(string token, User user)> LoginAsync(string email, string password);
        string GenerateJwtToken(User user);
    }

    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<User> RegisterAsync(string username, string email, string password, string firstName, string lastName, string phoneNumber)
        {
            // Check if user already exists
            if (await _context.Users.AnyAsync(u => u.Email == email || u.Username == username))
            {
                throw new Exception("User with this email or username already exists.");
            }

            // Create new user
            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = BC.HashPassword(password),
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = phoneNumber,
                DateRegistered = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<(string token, User user)> LoginAsync(string email, string password)
        {
            // Find user by email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            
            // Check if user exists and password is correct
            if (user == null || !BC.Verify(password, user.PasswordHash))
            {
                throw new Exception("Invalid email or password.");
            }

            // Generate JWT token
            var token = GenerateJwtToken(user);

            return (token, user);
        }

        public string GenerateJwtToken(User user)
        {
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
            
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email)
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"]
            };
            
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            
            return tokenHandler.WriteToken(token);
        }
    }
}