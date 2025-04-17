// <copyright file="AuthService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SmartAutoTrader.API.Services
{
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;
    using Microsoft.IdentityModel.Tokens;
    using SmartAutoTrader.API.Models;
    using SmartAutoTrader.API.Repositories;
    using BC = BCrypt.Net.BCrypt;

    public interface IAuthService
    {
        Task<User> RegisterAsync(
            string username,
            string email,
            string password,
            string firstName,
            string lastName,
            string phoneNumber);

        Task<(string token, User user)> LoginAsync(string email, string password);

        string GenerateJwtToken(User user);
    }

    public class AuthService(IUserRepository userRepo, IConfiguration configuration) : IAuthService
    {
        private readonly IConfiguration configuration = configuration;
        private readonly IUserRepository userRepo = userRepo;

        /// <inheritdoc/>
        public async Task<User> RegisterAsync(
            string username,
            string email,
            string password,
            string firstName,
            string lastName,
            string phoneNumber)
        {
            // Check if user already exists
            if (await this.userRepo.ExistsAsync(email, username))
            {
                throw new Exception("User with this email or username already exists.");
            }

            // Create new user
            User user = new()
            {
                Username = username,
                Email = email,
                PasswordHash = BC.HashPassword(password),
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = phoneNumber,
                DateRegistered = DateTime.Now,
            };

            await this.userRepo.AddAsync(user);
            await this.userRepo.SaveChangesAsync();

            return user;
        }

        /// <inheritdoc/>
        public async Task<(string token, User user)> LoginAsync(string email, string password)
        {
            // Find user by email
            User? user = await this.userRepo.GetByEmailAsync(email);

            // Check if user exists and password is correct
            if (user == null || !BC.Verify(password, user.PasswordHash))
            {
                throw new Exception("Invalid email or password.");
            }

            // Generate JWT token
            string token = this.GenerateJwtToken(user);

            return (token, user);
        }

        /// <inheritdoc/>
        public string GenerateJwtToken(User user)
        {
            string? jwtKey = this.configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new InvalidOperationException("JWT key is missing from configuration.");
            }

            byte[] key = Encoding.ASCII.GetBytes(jwtKey);
            SecurityTokenDescriptor tokenDescriptor = new()
            {
                Subject = new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Name, user.Username!),
                        new Claim(ClaimTypes.Email, user.Email!),
                    }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials =
                    new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = this.configuration["Jwt:Issuer"],
                Audience = this.configuration["Jwt:Audience"],
            };

            JwtSecurityTokenHandler tokenHandler = new();
            SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}