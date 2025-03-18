using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartAutoTrader.API.Data;
using SmartAutoTrader.API.Models;

namespace SmartAutoTrader.API.Services
{
    public interface IChatRecommendationService
    {
        Task<ChatResponse> ProcessMessageAsync(int userId, ChatMessage message);
    }

    public class ChatMessage
    {
        public string Content { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class ChatResponse
    {
        public string Message { get; set; }
        public List<Vehicle> RecommendedVehicles { get; set; } = new List<Vehicle>();
        public RecommendationParameters UpdatedParameters { get; set; }
    }

    public class ChatRecommendationService : IChatRecommendationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ChatRecommendationService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IAIRecommendationService _recommendationService;
        
        public ChatRecommendationService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<ChatRecommendationService> logger,
            HttpClient httpClient,
            IAIRecommendationService recommendationService)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
            _recommendationService = recommendationService;
        }
        
        public async Task<ChatResponse> ProcessMessageAsync(int userId, ChatMessage message)
{
    try
    {
        _logger.LogInformation("Processing chat message for user ID: {UserId}", userId);
        
        // Get user context for personalization 
        var user = await _context.Users
            .Include(u => u.Preferences)
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user == null)
        {
            _logger.LogWarning("User with ID {UserId} not found", userId);
            return new ChatResponse { Message = "Sorry, I couldn't process your request. Please try again later." };
        }

        // Load related entities separately 
        user.Favorites = await _context.UserFavorites
            .Where(f => f.UserId == userId)
            .Include(f => f.Vehicle)
            .ToListAsync();

        user.BrowsingHistory = await _context.BrowsingHistory
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.ViewDate)
            .Take(5)
            .Include(h => h.Vehicle)
            .ToListAsync();
        
        // Create basic recommendation parameters with just the text prompt
        var parameters = new RecommendationParameters
        {
            TextPrompt = message.Content,
            MaxResults = 5
        };
        
        // Save the chat history
        await SaveChatHistoryAsync(userId, message, 
            $"I'm looking for vehicles that match: {message.Content}");
        
        // Get recommendations based on the parameters
        var recommendations = await _recommendationService.GetRecommendationsAsync(userId, parameters);
        
        return new ChatResponse
        {
            Message = $"Here are some vehicles that match your request for: {message.Content}",
            RecommendedVehicles = recommendations.ToList(),
            UpdatedParameters = parameters
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing chat message for user ID {UserId}", userId);
        return new ChatResponse
        {
            Message = "I'm sorry, but I encountered an error processing your request. Please try again later.",
            RecommendedVehicles = new List<Vehicle>(),
            UpdatedParameters = new RecommendationParameters()
        };
    }
}
        
        private string BuildUserContext(User user)
        {
            var context = new StringBuilder();
            
            // Add basic user info
            context.AppendLine($"User ID: {user.Id}");
            context.AppendLine($"User Name: {user.FirstName} {user.LastName}");
            
            // Add preferences
            if (user.Preferences?.Any() == true)
            {
                context.AppendLine("\nUser Preferences:");
                foreach (var pref in user.Preferences)
                {
                    context.AppendLine($"- {pref.PreferenceType}: {pref.Value} (Weight: {pref.Weight})");
                }
            }
            
            // Add favorite vehicles
            if (user.Favorites?.Any() == true)
            {
                context.AppendLine("\nUser Favorites:");
                foreach (var fav in user.Favorites.Take(3)) // Limit to 3 for brevity
                {
                    context.AppendLine($"- {fav.Vehicle.Year} {fav.Vehicle.Make} {fav.Vehicle.Model}, Price: {fav.Vehicle.Price:C0}, Type: {fav.Vehicle.VehicleType}, Fuel: {fav.Vehicle.FuelType}");
                }
            }
            
            // Add recent browsing history
            if (user.BrowsingHistory?.Any() == true)
            {
                context.AppendLine("\nRecent Browsing History:");
                foreach (var history in user.BrowsingHistory.Take(3)) // Limit to 3 for brevity
                {
                    context.AppendLine($"- {history.Vehicle.Year} {history.Vehicle.Make} {history.Vehicle.Model}, Price: {history.Vehicle.Price:C0}, Type: {history.Vehicle.VehicleType}, View Duration: {history.ViewDurationSeconds}s");
                }
            }
            
            return context.ToString();
        }
        
        private async Task<(string Message, Dictionary<string, object> Parameters)> ProcessWithAIAsync(string message, string userContext)
        {
            try
            {
                // Get the API configuration
                var apiKey = _configuration["ChatAI:ApiKey"];
                var endpoint = _configuration["ChatAI:Endpoint"];
                
                // Prepare the prompt
                var prompt = new StringBuilder();
                prompt.AppendLine("You are an automotive assistant helping a customer find their ideal vehicle.");
                prompt.AppendLine("Your task is to understand their needs and preferences, then suggest appropriate vehicles.");
                prompt.AppendLine("\nUser Context:");
                prompt.AppendLine(userContext);
                prompt.AppendLine("\nUser Message:");
                prompt.AppendLine(message);
                prompt.AppendLine("\nPlease respond conversationally to the user's request.");
                prompt.AppendLine("After your response, include structured information about their preferences in a JSON format to update search parameters:");
                prompt.AppendLine(@"{
  ""minPrice"": null,
  ""maxPrice"": null,
  ""minYear"": null,
  ""maxYear"": null,
  ""preferredMakes"": [],
  ""preferredVehicleTypes"": [],
  ""preferredFuelTypes"": [],
  ""desiredFeatures"": []
}");
                
                // Prepare the request
                var request = new
                {
                    model = _configuration["ChatAI:Model"] ?? "gpt-3.5-turbo",
                    messages = new[]
                    {
                        new { role = "system", content = prompt.ToString() },
                        new { role = "user", content = message }
                    },
                    temperature = 0.7,
                    max_tokens = 500
                };
                
                // Send the request to the AI service
                _httpClient.DefaultRequestHeaders.Clear();
                if (!string.IsNullOrEmpty(apiKey))
                {
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                }
                
                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json");
                
                var response = await _httpClient.PostAsync(endpoint, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("AI service error: {StatusCode}, {ErrorContent}", response.StatusCode, errorContent);
                    return ("I'm sorry, I couldn't understand your request. Could you please provide more details about the type of vehicle you're looking for?", new Dictionary<string, object>());
                }
                
                // Parse the response
                var responseContent = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(responseContent);
                
                // Extract the message content
                var choices = document.RootElement.GetProperty("choices");
                var messageContent = choices[0].GetProperty("message").GetProperty("content").GetString();
                
                // Split the response to extract parameters
                var parts = messageContent.Split("```json", StringSplitOptions.RemoveEmptyEntries);
                
                string responseMessage;
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                
                if (parts.Length >= 2)
                {
                    // Extract the conversational response
                    responseMessage = parts[0].Trim();
                    
                    // Extract and parse the JSON parameters
                    var jsonPart = parts[1].Split("```", StringSplitOptions.RemoveEmptyEntries)[0].Trim();
                    try
                    {
                        parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonPart);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error parsing JSON parameters from AI response");
                    }
                }
                else
                {
                    // If the response doesn't contain structured data, use the whole message
                    responseMessage = messageContent.Trim();
                }
                
                return (responseMessage, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing with AI");
                return ("I'm sorry, I encountered an error while processing your request. Could you please try again?", new Dictionary<string, object>());
            }
        }
        
        private RecommendationParameters ConvertToRecommendationParameters(
            (string Message, Dictionary<string, object> Parameters) processingResult, 
            User user)
        {
            var parameters = new RecommendationParameters();
            
            // Try to extract parameters from the AI response
            if (processingResult.Parameters.TryGetValue("minPrice", out var minPrice) && minPrice != null)
            {
                if (minPrice is JsonElement element && element.ValueKind != JsonValueKind.Null)
                {
                    parameters.MinPrice = element.GetDecimal();
                }
            }
            
            if (processingResult.Parameters.TryGetValue("maxPrice", out var maxPrice) && maxPrice != null)
            {
                if (maxPrice is JsonElement element && element.ValueKind != JsonValueKind.Null)
                {
                    parameters.MaxPrice = element.GetDecimal();
                }
            }
            
            if (processingResult.Parameters.TryGetValue("minYear", out var minYear) && minYear != null)
            {
                if (minYear is JsonElement element && element.ValueKind != JsonValueKind.Null)
                {
                    parameters.MinYear = element.GetInt32();
                }
            }
            
            if (processingResult.Parameters.TryGetValue("maxYear", out var maxYear) && maxYear != null)
            {
                if (maxYear is JsonElement element && element.ValueKind != JsonValueKind.Null)
                {
                    parameters.MaxYear = element.GetInt32();
                }
            }
            
            if (processingResult.Parameters.TryGetValue("preferredMakes", out var makes) && makes != null)
            {
                if (makes is JsonElement element && element.ValueKind == JsonValueKind.Array)
                {
                    parameters.PreferredMakes = new List<string>();
                    foreach (var item in element.EnumerateArray())
                    {
                        parameters.PreferredMakes.Add(item.GetString());
                    }
                }
            }
            
            if (processingResult.Parameters.TryGetValue("preferredVehicleTypes", out var types) && types != null)
            {
                if (types is JsonElement element && element.ValueKind == JsonValueKind.Array)
                {
                    parameters.PreferredVehicleTypes = new List<VehicleType>();
                    foreach (var item in element.EnumerateArray())
                    {
                        if (Enum.TryParse<VehicleType>(item.GetString(), true, out var vehicleType))
                        {
                            parameters.PreferredVehicleTypes.Add(vehicleType);
                        }
                    }
                }
            }
            
            if (processingResult.Parameters.TryGetValue("preferredFuelTypes", out var fuels) && fuels != null)
            {
                if (fuels is JsonElement element && element.ValueKind == JsonValueKind.Array)
                {
                    parameters.PreferredFuelTypes = new List<FuelType>();
                    foreach (var item in element.EnumerateArray())
                    {
                        if (Enum.TryParse<FuelType>(item.GetString(), true, out var fuelType))
                        {
                            parameters.PreferredFuelTypes.Add(fuelType);
                        }
                    }
                }
            }
            
            if (processingResult.Parameters.TryGetValue("desiredFeatures", out var features) && features != null)
            {
                if (features is JsonElement element && element.ValueKind == JsonValueKind.Array)
                {
                    parameters.DesiredFeatures = new List<string>();
                    foreach (var item in element.EnumerateArray())
                    {
                        parameters.DesiredFeatures.Add(item.GetString());
                    }
                }
            }
            
            // Set default Max Results
            parameters.MaxResults = 5;
            
            return parameters;
        }
        
        private async Task SaveChatHistoryAsync(int userId, ChatMessage userMessage, string aiResponse)
        {
            try
            {
                // Create a chat history record
                var chatHistory = new ChatHistory
                {
                    UserId = userId,
                    UserMessage = userMessage.Content,
                    AIResponse = aiResponse,
                    Timestamp = DateTime.UtcNow
                };
                
                _context.ChatHistory.Add(chatHistory);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving chat history for user ID {UserId}", userId);
                // Continue even if saving history fails
            }
        }
    }
}