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

        Task<IEnumerable<string>> GetUserRolesAsync(int userId);

        Task AssignRoleToUserAsync(int userId, string roleName);
    }

    public class AuthService : IAuthService
    {
        private readonly IUserRepository userRepo;
        private readonly IRoleRepository roleRepo;
        private readonly IConfiguration configuration;

        public AuthService(
            IUserRepository userRepo,
            IRoleRepository roleRepo,
            IConfiguration configuration)
        {
            this.userRepo = userRepo;
            this.roleRepo = roleRepo;
            this.configuration = configuration;
        }

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

            // Assign default "User" role
            await this.AssignRoleToUserAsync(user.Id, "User");

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

            // Get user roles asynchronously but wait for result
            var roles = this.GetUserRolesAsync(user.Id).GetAwaiter().GetResult();

            // Create claims list with basic user info
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username!),
                new Claim(ClaimTypes.Email, user.Email!),
            };

            // Add role claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            byte[] key = Encoding.ASCII.GetBytes(jwtKey);
            SecurityTokenDescriptor tokenDescriptor = new()
            {
                Subject = new ClaimsIdentity(claims),
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

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> GetUserRolesAsync(int userId)
        {
            return await this.roleRepo.GetUserRolesAsync(userId);
        }

        /// <inheritdoc/>
        public async Task AssignRoleToUserAsync(int userId, string roleName)
        {
            // Get the role ID for the specified role name
            var role = await this.roleRepo.GetRoleByNameAsync(roleName);
            if (role == null)
            {
                throw new Exception($"Role '{roleName}' not found.");
            }

            // Assign the role to the user
            await this.roleRepo.AssignRoleToUserAsync(userId, role.Id);
        }
    }
}