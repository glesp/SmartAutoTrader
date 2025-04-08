using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartAutoTrader.API.Data;
using SmartAutoTrader.API.Models;
using SmartAutoTrader.API.Repositories;
using BC = BCrypt.Net.BCrypt;

namespace SmartAutoTrader.API.Services
{
    public interface IAuthService
    {
        Task<User> RegisterAsync(string username, string email, string password, string firstName, string lastName,
            string phoneNumber);

        Task<(string token, User user)> LoginAsync(string email, string password);

        string GenerateJwtToken(User user);
    }

    public class AuthService(IUserRepository userRepo, IConfiguration configuration) : IAuthService
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly IUserRepository _userRepo = userRepo;

        public async Task<User> RegisterAsync(string username, string email, string password, string firstName,
            string lastName, string phoneNumber)
        {
            // Check if user already exists
            if (await _userRepo.ExistsAsync(email, username))
            {
                throw new Exception("User with this email or username already exists.");
            }

            // Create new user
            User user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = BC.HashPassword(password),
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = phoneNumber,
                DateRegistered = DateTime.Now,
            };

            await _userRepo.AddAsync(user);
            await _userRepo.SaveChangesAsync();

            return user;
        }

        public async Task<(string token, User user)> LoginAsync(string email, string password)
        {
            // Find user by email
            User? user = await _userRepo.GetByEmailAsync(email);

            // Check if user exists and password is correct
            if (user == null || !BC.Verify(password, user.PasswordHash))
            {
                throw new Exception("Invalid email or password.");
            }

            // Generate JWT token
            string token = GenerateJwtToken(user);

            return (token, user);
        }

        public string GenerateJwtToken(User user)
        {
            byte[] key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);

            SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials =
                    new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
            };

            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}