/* <copyright file="UserRoleSeeder.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the UserRoleSeeder class, which is responsible for seeding default user roles and an admin user into the database during application initialization.
</summary>
<remarks>
The UserRoleSeeder class ensures that the database contains at least one admin user and the necessary roles for the application. It uses dependency injection to access the ApplicationDbContext and ILogger services. The seeding process checks for existing users and roles, creates a default admin user if none exist, and assigns the admin role to the user. This class is typically invoked during application startup.
</remarks>
<dependencies>
- Microsoft.EntityFrameworkCore
- SmartAutoTrader.API.Data
- SmartAutoTrader.API.Models
- BCrypt.Net.BCrypt
</dependencies>
 */

namespace SmartAutoTrader.API.DataSeeding
{
    using Microsoft.EntityFrameworkCore;
    using SmartAutoTrader.API.Data;
    using SmartAutoTrader.API.Models;
    using BC = BCrypt.Net.BCrypt;

    public class UserRoleSeeder
    {
        /// <summary>
        /// The logger used for recording application events and errors.
        /// </summary>
        private readonly ILogger<UserRoleSeeder> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserRoleSeeder"/> class.
        /// </summary>
        /// <param name="logger">The logger used for recording application events and errors.</param>
        public UserRoleSeeder(ILogger<UserRoleSeeder> logger)
        {
            this._logger = logger;
        }

        /// <summary>
        /// Seeds the default admin user and assigns the admin role if no users exist in the database.
        /// </summary>
        /// <param name="serviceProvider">The service provider used to resolve the <see cref="ApplicationDbContext"/>.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="Exception">Thrown if an error occurs during the seeding process.</exception>
        /// <remarks>
        /// This method checks if any users exist in the database. If no users are found, it creates a default admin user with a predefined username, email, and password. It also assigns the admin role to the user. If users already exist, it ensures that the first user (ID=1) has the admin role assigned.
        /// </remarks>
        /// <example>
        /// var seeder = new UserRoleSeeder(logger);
        /// await seeder.SeedAdminUserAsync(serviceProvider).
        /// </example>
        public async Task SeedAdminUserAsync(IServiceProvider serviceProvider)
        {
            // We get the DbContext here from the service provider passed from Program.cs
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            this._logger.LogInformation("Checking for admin user seeding...");

            try
            {
                // Check if ANY user exists. If not, create the Admin user.
                if (!await context.Users.AnyAsync())
                {
                    this._logger.LogInformation("No users found. Seeding default Admin user.");

                    // 1. Create the Admin User
                    var adminUser = new User
                    {
                        Username = "admin",
                        Email = "admin@admin.com",
                        PasswordHash = BC.HashPassword("AdminPassword123!"),
                        FirstName = "Admin",
                        LastName = "User",
                        DateRegistered = DateTime.UtcNow,
                    };
                    context.Users.Add(adminUser);
                    await context.SaveChangesAsync();
                    var adminRole = await context.Roles.FindAsync(1);

                    if (adminRole != null)
                    {
                        var adminUserRole = new UserRole
                        {
                            UserId = adminUser.Id,
                            RoleId = adminRole.Id,
                        };
                        context.UserRoles.Add(adminUserRole);
                        await context.SaveChangesAsync();
                        this._logger.LogInformation("Default Admin user created and assigned Admin role.");
                    }
                    else
                    {
                        this._logger.LogError("Admin role (ID=1) not found in database. Cannot assign role.");
                    }
                }
                else
                {
                    this._logger.LogInformation("Users already exist. Checking if User ID 1 needs Admin role...");
                    var adminUser = await context.Users.FindAsync(1);
                    if (adminUser != null)
                    {
                        var adminRole = await context.Roles.FindAsync(1); // Admin Role ID
                        if (adminRole != null)
                        {
                            bool userRoleExists = await context.UserRoles
                                .AnyAsync(ur => ur.UserId == adminUser.Id && ur.RoleId == adminRole.Id);

                            if (!userRoleExists)
                            {
                                context.UserRoles.Add(new UserRole { UserId = adminUser.Id, RoleId = adminRole.Id });
                                await context.SaveChangesAsync();
                                this._logger.LogInformation($"Assigned Admin role to existing User ID {adminUser.Id}");
                            }
                            else
                            {
                                this._logger.LogInformation($"User ID {adminUser.Id} already has Admin role assigned.");
                            }
                        }
                        else
                        {
                            this._logger.LogError(
                                "Admin role (ID=1) not found in database. Cannot assign role to User ID 1.");
                        }
                    }
                    else
                    {
                        this._logger.LogInformation(
                            "User ID 1 not found. Skipping role assignment check for User ID 1.");
                    }
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "An error occurred during admin user seeding.");
            }
        }
    }
}