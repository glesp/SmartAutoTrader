// backend/DataSeeding/UserRoleSeeder.cs
// <copyright file="UserRoleSeeder.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using SmartAutoTrader.API.Data;
using SmartAutoTrader.API.Models;
using BC = BCrypt.Net.BCrypt;

namespace SmartAutoTrader.API.DataSeeding
{
    public class UserRoleSeeder
    {
        private readonly ILogger<UserRoleSeeder> _logger;

        // Constructor accepting logger (optional, but good practice)
        public UserRoleSeeder(ILogger<UserRoleSeeder> logger)
        {
            _logger = logger;
        }

        public async Task SeedAdminUserAsync(IServiceProvider serviceProvider)
        {
            // We get the DbContext here from the service provider passed from Program.cs
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            _logger.LogInformation("Checking for admin user seeding...");

            try
            {
                // Check if ANY user exists. If not, create the Admin user.
                if (!await context.Users.AnyAsync())
                {
                    _logger.LogInformation("No users found. Seeding default Admin user.");

                    // 1. Create the Admin User
                    var adminUser = new User
                    {
                        Username = "admin",
                        Email = "admin@admin.com",
                        PasswordHash = BC.HashPassword("AdminPassword123!"),
                        FirstName = "Admin",
                        LastName = "User",
                        DateRegistered = DateTime.UtcNow
                    };
                    context.Users.Add(adminUser);
                    await context.SaveChangesAsync();
                    var adminRole = await context.Roles.FindAsync(1);

                    if (adminRole != null)
                    {
                        var adminUserRole = new UserRole
                        {
                            UserId = adminUser.Id,
                            RoleId = adminRole.Id
                        };
                        context.UserRoles.Add(adminUserRole);
                        await context.SaveChangesAsync();
                        _logger.LogInformation("Default Admin user created and assigned Admin role.");
                    }
                    else
                    {
                        _logger.LogError("Admin role (ID=1) not found in database. Cannot assign role.");
                    }
                }
                else
                {
                    _logger.LogInformation("Users already exist. Checking if User ID 1 needs Admin role...");
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
                                _logger.LogInformation($"Assigned Admin role to existing User ID {adminUser.Id}");
                            }
                            else
                            {
                                _logger.LogInformation($"User ID {adminUser.Id} already has Admin role assigned.");
                            }
                        }
                        else
                        {
                            _logger.LogError(
                                "Admin role (ID=1) not found in database. Cannot assign role to User ID 1.");
                        }
                    }
                    else
                    {
                        _logger.LogInformation("User ID 1 not found. Skipping role assignment check for User ID 1.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during admin user seeding.");
            }
        }
    }
}