using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartAutoTrader.API;
using SmartAutoTrader.API.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http.Headers;
using System;
using SmartAutoTrader.API.Models; // Add this using

namespace SmartAutoTrader.Tests.Helpers
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove the app's ApplicationDbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                
                // Remove seeders
                var seederDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(API.DataSeeding.VehicleSeeder));
                if (seederDescriptor != null)
                    services.Remove(seederDescriptor);

                var roleSeederDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(API.DataSeeding.UserRoleSeeder));
                if (roleSeederDescriptor != null)
                    services.Remove(roleSeederDescriptor);

                // Add in-memory database
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryDbForTesting");
                });

                // Seed a default test user
                var sp = services.BuildServiceProvider();
                using (var scope = sp.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<ApplicationDbContext>();
                    var logger = scopedServices.GetRequiredService<ILogger<CustomWebApplicationFactory>>();

                    try
                    {
                        db.Database.EnsureCreated(); // Ensure the database is created.

                        // Check if the user already exists
                        if (!db.Users.Any(u => u.Id == 1))
                        {
                            var testUser = new User // Assuming SmartAutoTrader.API.Models.User
                            {
                                Id = 1, // Must match the ID used in TestAuthHandler claims
                                Username = "TestUser",
                                Email = "testuser@example.com",
                                PasswordHash = "test_password_hash", // Not used for auth in TestAuthHandler but good for completeness
                                FirstName = "Test",
                                LastName = "User",
                                DateRegistered = DateTime.UtcNow
                            };
                            db.Users.Add(testUser);

                            // If you have a UserRole linking table and Role table:
                            // Ensure "User" role exists
                            // if (!db.Roles.Any(r => r.Name == "User"))
                            // {
                            //     db.Roles.Add(new Role { Id = 1, Name = "User" }); // Adjust Id as needed
                            // }
                            // db.UserRoles.Add(new UserRole { UserId = testUser.Id, RoleId = 1 }); // Adjust RoleId as needed
                            
                            db.SaveChanges();
                            logger.LogInformation("Seeded default user with ID 1 for testing.");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "An error occurred seeding the database with test user.");
                    }
                }

                // Register required services that might be missing
                services.AddScoped<API.Services.IChatRecommendationService, API.Services.ChatRecommendationService>();
                services.AddScoped<API.Services.IAIRecommendationService, TestAIRecommendationService>();
                services.AddHttpClient();
                services.AddMemoryCache();
                
                // Configure authentication
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        "Test", options => { });

                // Configure authorization
                services.AddAuthorization(options =>
                {
                    options.DefaultPolicy = new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .AddAuthenticationSchemes("Test")
                        .Build();

                    options.AddPolicy("Admin", new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .RequireRole("Admin")
                        .AddAuthenticationSchemes("Test")
                        .Build());
                });

                // Ensure controllers are registered
                services.AddControllers()
                    .AddApplicationPart(typeof(API.Controllers.ChatController).Assembly);
            });
        }

        public HttpClient GetAuthenticatedClient()
        {
            var client = CreateClient();
            
            // Add potential auth headers here if needed
            client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Test", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("Dummy")));
            
            return client;
        }
    }

    // Simple mock implementation for AI recommendation service to avoid external API calls
    public class TestAIRecommendationService : API.Services.IAIRecommendationService
    {
        private readonly ApplicationDbContext _dbContext;

        public TestAIRecommendationService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<API.Models.Vehicle>> GetRecommendationsAsync(int userId, API.Models.RecommendationParameters parameters)
        {
            var query = _dbContext.Vehicles!
                .Where(v => v.Status == API.Enums.VehicleStatus.Available);

            if (parameters.PreferredMakes?.Any() == true)
            {
                query = query.Where(v => parameters.PreferredMakes.Contains(v.Make!));
            }
            if (parameters.PreferredVehicleTypes?.Any() == true)
            {
                // parameters.PreferredVehicleTypes is already List<API.Enums.VehicleType>
                // No need to parse, directly use the list.
                query = query.Where(v => parameters.PreferredVehicleTypes.Contains(v.VehicleType));
            }
            if (parameters.PreferredFuelTypes?.Any() == true)
            {
                // parameters.PreferredFuelTypes is already List<API.Enums.FuelType>
                // No need to parse, directly use the list.
                query = query.Where(v => parameters.PreferredFuelTypes.Contains(v.FuelType));
            }
            if (parameters.MinPrice.HasValue)
            {
                query = query.Where(v => v.Price >= parameters.MinPrice.Value);
            }
            if (parameters.MaxPrice.HasValue)
            {
                query = query.Where(v => v.Price <= parameters.MaxPrice.Value);
            }
            if (parameters.MinYear.HasValue)
            {
                query = query.Where(v => v.Year >= parameters.MinYear.Value);
            }
            if (parameters.MaxYear.HasValue)
            {
                query = query.Where(v => v.Year <= parameters.MaxYear.Value);
            }
            if (parameters.MaxMileage.HasValue)
            {
                query = query.Where(v => v.Mileage <= parameters.MaxMileage.Value);
            }
            // Add more filters as needed based on RecommendationParameters properties

            return await query
                .Take(parameters.MaxResults ?? 10)
                .ToListAsync();
        }
    }
}