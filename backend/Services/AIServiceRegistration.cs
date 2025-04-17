// <copyright file="AIServiceRegistration.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SmartAutoTrader.API.Services
{
    using System.Globalization;

    public static class AIServiceRegistration
    {
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