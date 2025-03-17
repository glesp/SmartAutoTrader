using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace SmartAutoTrader.API.Services
{
    public static class AIServiceRegistration
    {
        public static IServiceCollection AddAIRecommendationServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Register HttpClient for API calls
            services.AddHttpClient();
            
            // Determine which AI provider to use based on configuration
            var aiProvider = configuration["AI:Provider"]?.ToLower() ?? "huggingface";
            
            switch (aiProvider)
            {
                case "huggingface":
                    services.AddScoped<IAIRecommendationService, HuggingFaceRecommendationService>();
                    break;
                    
                case "openai":
                    // If you implement OpenAI in the future
                    // services.AddScoped<IAIRecommendationService, OpenAIRecommendationService>();
                    throw new NotImplementedException("OpenAI provider not yet implemented");
                    
                case "none":
                case "fallback":
                    // For a simple fallback without AI, implement a FallbackRecommendationService
                    // services.AddScoped<IAIRecommendationService, FallbackRecommendationService>();
                    throw new NotImplementedException("Fallback provider not yet implemented");
                    
                default:
                    // Default to Hugging Face
                    services.AddScoped<IAIRecommendationService, HuggingFaceRecommendationService>();
                    break;
            }
            
            return services;
        }
    }
}