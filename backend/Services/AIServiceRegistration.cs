/* <copyright file="AIServiceRegistration.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the AIServiceRegistration class, which provides extension methods for registering AI-based recommendation services in the Smart Auto Trader application.
</summary>
<remarks>
The AIServiceRegistration class is a static helper class designed to simplify the registration of AI recommendation services in the application's dependency injection container. It determines the appropriate AI provider to use based on the application's configuration and registers the corresponding service implementation. This class supports extensibility for adding new AI providers in the future.
</remarks>
<dependencies>
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Configuration
- System.Globalization
</dependencies>
 */

namespace SmartAutoTrader.API.Services
{
    using System.Globalization;

    /// <summary>
    /// Provides extension methods for registering AI-based recommendation services.
    /// </summary>
    /// <remarks>
    /// This static class simplifies the process of configuring and registering AI recommendation services in the application's dependency injection container. It supports multiple AI providers and allows for future extensibility.
    /// </remarks>
    public static class AIServiceRegistration
    {
        /// <summary>
        /// Registers AI-based recommendation services in the application's dependency injection container.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to which the services will be added.</param>
        /// <param name="configuration">The application's configuration object, used to determine the AI provider.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> with the registered AI recommendation services.</returns>
        /// <exception cref="NotImplementedException">
        /// Thrown if the specified AI provider is not implemented, such as "openai" or "fallback.".
        /// </exception>
        /// <remarks>
        /// This method determines the AI provider to use based on the "AI:Provider" configuration key. If no provider is specified, it defaults to "openrouter." The method supports extensibility for adding new AI providers in the future.
        /// </remarks>
        /// <example>
        /// <code>
        /// var builder = WebApplication.CreateBuilder(args);
        /// builder.Services.AddAIRecommendationServices(builder.Configuration);
        /// </code>
        /// </example>
        public static IServiceCollection AddAIRecommendationServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Register HttpClient for API calls
            _ = services.AddHttpClient();

            // Determine which AI provider to use based on configuration
            string aiProvider = configuration["AI:Provider"]?.ToLower(CultureInfo.CurrentCulture) ?? "openrouter";

            _ = aiProvider switch
            {
                "openrouter" => services.AddScoped<IAIRecommendationService, OpenRouterRecommendationService>(),
                "openai" => throw new NotImplementedException(
                    "OpenAI provider not yet implemented"), // If you implement OpenAI in the future

                // services.AddScoped<IAIRecommendationService, OpenAIRecommendationService>();
                "none" or "fallback" => throw new NotImplementedException(
                    "Fallback provider not yet implemented"), // For a simple fallback without AI, implement a FallbackRecommendationService

                // services.AddScoped<IAIRecommendationService, FallbackRecommendationService>();
                _ => services
                    .AddScoped<IAIRecommendationService, OpenRouterRecommendationService>(), // Default to Hugging Face
            };
            return services;
        }
    }
}