using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartAutoTrader.API.Data;
using SmartAutoTrader.API.DataSeeding;
using SmartAutoTrader.API.Helpers;
using SmartAutoTrader.API.Repositories;
using SmartAutoTrader.API.Services;
using ZLogger;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

builder.Services.AddScoped<IConversationContextService, ConversationContextService>();
builder.Services.AddMemoryCache();

builder.Services.AddDbContext<ApplicationDbContext>(
    options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Authentication
JwtSettingsValidator.ValidateJwtSettings(builder.Configuration);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(
        options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            };
        });

// Register services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<VehicleSeeder>();
builder.Services.AddScoped<UserRoleSeeder>();
builder.Services.AddHttpClient();
builder.Services.AddAIRecommendationServices(builder.Configuration);
builder.Services.AddScoped<IChatRecommendationService, ChatRecommendationService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();

builder.Services.AddControllers()
    .AddJsonOptions(
        options =>
        {
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        string? allowedOrigin = builder.Configuration["CorsOrigins"];

        if (!string.IsNullOrWhiteSpace(allowedOrigin))
        {
            policy.WithOrigins(allowedOrigin)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();

            Console.WriteLine($"CORS Policy 'AllowFrontend' configured for origin: {allowedOrigin}");
        }
        else
        {
            // Optional: Fallback for local development if the environment variable isn't set
            policy.WithOrigins("http://localhost:5173")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();

            Console.WriteLine("CORS Policy 'AllowFrontend' using fallback origin: http://localhost:5173");
        }
    });
});

// Ensure Logs folder exists
Directory.CreateDirectory("Logs");

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddZLoggerFile("Logs/app_log.txt");

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();

// Add this in your services configuration section
builder.Services.AddSwaggerGen(
    options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "Smart Auto Trader API", Version = "v1" });

        // Define the JWT Bearer authentication scheme
        options.AddSecurityDefinition(
            "Bearer",
            new OpenApiSecurityScheme
            {
                Description =
                    "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
            });

        options.AddSecurityRequirement(
            new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer",
                        },
                    },
                    Array.Empty<string>()
                },
            });
    });

WebApplication app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    _ = app.UseSwagger();
    _ = app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.UseStaticFiles();

// Recommended Seeding Block in Program.cs
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        logger.LogInformation("Applying database migrations...");
        context.Database.Migrate();

        logger.LogInformation("Seeding vehicles...");
        var vehicleSeeder = services.GetRequiredService<VehicleSeeder>();
        vehicleSeeder.SeedVehicles(services);

        logger.LogInformation("Seeding admin user/roles...");
        var userRoleSeeder = services.GetRequiredService<UserRoleSeeder>();
        await userRoleSeeder.SeedAdminUserAsync(services);

        logger.LogInformation("Database seeding/migration check completed.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database seeding/migration.");

        // Consider re-throwing or shutting down if seeding is critical
    }
}

app.Run();