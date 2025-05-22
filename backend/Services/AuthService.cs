/* <copyright file="AuthService.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the AuthService class and its interface, IAuthService, which provide methods for user authentication, registration, and role management in the Smart Auto Trader application.
</summary>
<remarks>
The AuthService class implements the IAuthService interface and provides functionality for registering users, logging in, generating JWT tokens, retrieving user roles, and assigning roles to users. It uses BCrypt for password hashing and Entity Framework Core for database interactions. The class also integrates with the application's configuration to generate secure JWT tokens.
</remarks>
<dependencies>
- System.IdentityModel.Tokens.Jwt
- System.Security.Claims
- System.Text
- Microsoft.IdentityModel.Tokens
- SmartAutoTrader.API.Models
- SmartAutoTrader.API.Repositories
- BCrypt.Net.BCrypt
</dependencies>
 */

namespace SmartAutoTrader.API.Services
{
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;
    using Microsoft.IdentityModel.Tokens;
    using SmartAutoTrader.API.Models;
    using SmartAutoTrader.API.Repositories;
    using BC = BCrypt.Net.BCrypt;

    /// <summary>
    /// Defines methods for user authentication, registration, and role management.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Registers a new user in the system.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <param name="email">The email address of the user.</param>
        /// <param name="password">The password of the user.</param>
        /// <param name="firstName">The first name of the user.</param>
        /// <param name="lastName">The last name of the user.</param>
        /// <param name="phoneNumber">The phone number of the user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the registered <see cref="User"/> object.</returns>
        /// <exception cref="Exception">Thrown if a user with the same email or username already exists.</exception>
        Task<User> RegisterAsync(
            string username,
            string email,
            string password,
            string firstName,
            string lastName,
            string phoneNumber);

        /// <summary>
        /// Logs in a user and generates a JWT token.
        /// </summary>
        /// <param name="email">The email address of the user.</param>
        /// <param name="password">The password of the user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a tuple with the JWT token and the authenticated <see cref="User"/> object.</returns>
        /// <exception cref="Exception">Thrown if the email or password is invalid.</exception>
        Task<(string token, User user)> LoginAsync(string email, string password);

        /// <summary>
        /// Generates a JWT token for the specified user.
        /// </summary>
        /// <param name="user">The <see cref="User"/> object for which the token is generated.</param>
        /// <returns>A string representing the generated JWT token.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the JWT key is missing from the configuration.</exception>
        string GenerateJwtToken(User user);

        /// <summary>
        /// Retrieves the roles assigned to a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a collection of role names assigned to the user.</returns>
        Task<IEnumerable<string>> GetUserRolesAsync(int userId);

        /// <summary>
        /// Assigns a role to a user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="roleName">The name of the role to assign.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="Exception">Thrown if the specified role does not exist.</exception>
        Task AssignRoleToUserAsync(int userId, string roleName);
    }

    /// <summary>
    /// Implements the <see cref="IAuthService"/> interface to provide user authentication, registration, and role management.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IUserRepository userRepo;
        private readonly IRoleRepository roleRepo;
        private readonly IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthService"/> class.
        /// </summary>
        /// <param name="userRepo">The user repository for managing user data.</param>
        /// <param name="roleRepo">The role repository for managing user roles.</param>
        /// <param name="configuration">The application's configuration object.</param>
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