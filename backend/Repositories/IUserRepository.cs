/* <copyright file="IUserRepository.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the IUserRepository interface and its implementation, UserRepository, which provide methods for managing user-related data in the Smart Auto Trader application.
</summary>
<remarks>
The IUserRepository interface defines methods for retrieving, adding, and managing user data, including user favorites and browsing history. The UserRepository class implements these methods using Entity Framework Core to interact with the database. This repository is typically used in scenarios where user data needs to be accessed or modified, such as authentication, user preferences, and activity tracking.
</remarks>
<dependencies>
- Microsoft.EntityFrameworkCore
- SmartAutoTrader.API.Data
- SmartAutoTrader.API.Models
</dependencies>
 */

namespace SmartAutoTrader.API.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using SmartAutoTrader.API.Data;
    using SmartAutoTrader.API.Models;

    /// <summary>
    /// Defines methods for managing user-related data in the system.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Retrieves a user by their email address.
        /// </summary>
        /// <param name="email">The email address of the user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="User"/> object if found; otherwise, null.</returns>
        Task<User?> GetByEmailAsync(string email);

        /// <summary>
        /// Retrieves a user by their username.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="User"/> object if found; otherwise, null.</returns>
        Task<User?> GetByUsernameAsync(string username);

        /// <summary>
        /// Retrieves a user by their unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="User"/> object if found; otherwise, null.</returns>
        Task<User?> GetByIdAsync(int id);

        /// <summary>
        /// Checks if a user exists with the specified email or username.
        /// </summary>
        /// <param name="email">The email address to check.</param>
        /// <param name="username">The username to check.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if a user exists with the specified email or username; otherwise, false.</returns>
        Task<bool> ExistsAsync(string email, string username);

        /// <summary>
        /// Retrieves the list of user favorites along with their associated vehicles.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see cref="UserFavorite"/> objects with associated vehicles.</returns>
        Task<List<UserFavorite>> GetFavoritesWithVehiclesAsync(int userId);

        /// <summary>
        /// Retrieves the recent browsing history of a user along with the associated vehicles.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="limit">The maximum number of browsing history records to retrieve. Defaults to 5.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see cref="BrowsingHistory"/> objects with associated vehicles.</returns>
        Task<List<BrowsingHistory>> GetRecentBrowsingHistoryWithVehiclesAsync(int userId, int limit = 5);

        /// <summary>
        /// Adds a new user to the database.
        /// </summary>
        /// <param name="user">The <see cref="User"/> object to add.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task AddAsync(User user);

        /// <summary>
        /// Saves changes made to the database context.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task SaveChangesAsync();
    }

    /// <summary>
    /// Implements the <see cref="IUserRepository"/> interface to manage user-related data using Entity Framework Core.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserRepository"/> class.
        /// </summary>
        /// <param name="context">The database context used to interact with the users table.</param>
        public UserRepository(ApplicationDbContext context)
        {
            this.context = context;
        }

        /// <inheritdoc/>
        public Task<User?> GetByEmailAsync(string email)
        {
            return this.context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        /// <inheritdoc/>
        public Task<User?> GetByUsernameAsync(string username)
        {
            return this.context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        /// <inheritdoc/>
        public Task<User?> GetByIdAsync(int id)
        {
            return this.context.Users
                .Include(u => u.Preferences)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        /// <inheritdoc/>
        public Task<bool> ExistsAsync(string email, string username)
        {
            return this.context.Users.AnyAsync(u => u.Email == email || u.Username == username);
        }

        /// <inheritdoc/>
        public Task<List<UserFavorite>> GetFavoritesWithVehiclesAsync(int userId)
        {
            return this.context.UserFavorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Vehicle)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public Task<List<BrowsingHistory>> GetRecentBrowsingHistoryWithVehiclesAsync(int userId, int limit = 5)
        {
            return this.context.BrowsingHistory
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.ViewDate)
                .Take(limit)
                .Include(h => h.Vehicle)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public Task AddAsync(User user)
        {
            _ = this.context.Users.Add(user);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task SaveChangesAsync()
        {
            return this.context.SaveChangesAsync();
        }
    }
}