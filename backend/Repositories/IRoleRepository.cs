/* <copyright file="IRoleRepository.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the IRoleRepository interface and its implementation, RoleRepository, which provide methods for managing user roles in the Smart Auto Trader application.
</summary>
<remarks>
The IRoleRepository interface defines methods for retrieving, assigning, and removing roles for users, as well as checking user role assignments. The RoleRepository class implements these methods using Entity Framework Core to interact with the database. This repository is typically used in scenarios where role-based access control (RBAC) is required.
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
    /// Defines methods for managing user roles in the system.
    /// </summary>
    public interface IRoleRepository
    {
        /// <summary>
        /// Retrieves the roles assigned to a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a collection of role names assigned to the user.</returns>
        Task<IEnumerable<string>> GetUserRolesAsync(int userId);

        /// <summary>
        /// Retrieves a role by its name.
        /// </summary>
        /// <param name="roleName">The name of the role to retrieve.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="Role"/> object if found; otherwise, null.</returns>
        Task<Role?> GetRoleByNameAsync(string roleName);

        /// <summary>
        /// Retrieves a role by its unique identifier.
        /// </summary>
        /// <param name="roleId">The unique identifier of the role to retrieve.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="Role"/> object if found; otherwise, null.</returns>
        Task<Role?> GetRoleByIdAsync(int roleId);

        /// <summary>
        /// Assigns a role to a user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="roleId">The unique identifier of the role to assign.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task AssignRoleToUserAsync(int userId, int roleId);

        /// <summary>
        /// Removes a role from a user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="roleId">The unique identifier of the role to remove.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task RemoveRoleFromUserAsync(int userId, int roleId);

        /// <summary>
        /// Checks if a user has a specific role.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="roleName">The name of the role to check.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the user has the role; otherwise, false.</returns>
        Task<bool> UserHasRoleAsync(int userId, string roleName);

        /// <summary>
        /// Saves changes made to the database context.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task SaveChangesAsync();
    }

    /// <summary>
    /// Implements the <see cref="IRoleRepository"/> interface to manage user roles using Entity Framework Core.
    /// </summary>
    public class RoleRepository : IRoleRepository
    {
        private readonly ApplicationDbContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoleRepository"/> class.
        /// </summary>
        /// <param name="context">The database context used to interact with the roles and user roles tables.</param>
        public RoleRepository(ApplicationDbContext context)
        {
            this.context = context;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> GetUserRolesAsync(int userId)
        {
            return await this.context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Join(
                    this.context.Roles,
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => r.Name)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<Role?> GetRoleByNameAsync(string roleName)
        {
            return await this.context.Roles
                .FirstOrDefaultAsync(r => r.Name == roleName);
        }

        /// <inheritdoc/>
        public async Task<Role?> GetRoleByIdAsync(int roleId)
        {
            return await this.context.Roles
                .FirstOrDefaultAsync(r => r.Id == roleId);
        }

        /// <inheritdoc/>
        public async Task AssignRoleToUserAsync(int userId, int roleId)
        {
            // Check if the user already has this role
            bool hasRole = await this.context.UserRoles
                .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (!hasRole)
            {
                // Create a new UserRole record
                var userRole = new UserRole
                {
                    UserId = userId,
                    RoleId = roleId,
                };

                this.context.UserRoles.Add(userRole);
                await this.SaveChangesAsync();
            }
        }

        /// <inheritdoc/>
        public async Task RemoveRoleFromUserAsync(int userId, int roleId)
        {
            var userRole = await this.context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (userRole != null)
            {
                this.context.UserRoles.Remove(userRole);
                await this.SaveChangesAsync();
            }
        }

        /// <inheritdoc/>
        public async Task<bool> UserHasRoleAsync(int userId, string roleName)
        {
            return await this.context.UserRoles
                .Join(
                    this.context.Roles,
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => new { UserRole = ur, Role = r })
                .AnyAsync(x => x.UserRole.UserId == userId && x.Role.Name == roleName);
        }

        /// <inheritdoc/>
        public Task SaveChangesAsync()
        {
            return this.context.SaveChangesAsync();
        }
    }
}