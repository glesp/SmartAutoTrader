using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartAutoTrader.API.Data;
using SmartAutoTrader.API.Models;
using SmartAutoTrader.API.Validators;

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
        public bool IsClarification { get; set; } = false;
        public string? OriginalUserInput { get; set; } // Made nullable
    }

    public class ChatResponse
    {
        public string Message { get; set; }
        public List<Vehicle> RecommendedVehicles { get; set; } = new List<Vehicle>();
        public RecommendationParameters UpdatedParameters { get; set; }
        public bool ClarificationNeeded { get; set; } = false;
        public string? OriginalUserInput { get; set; }
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
                
                // Determine if this is a clarification or a new query
                string messageToProcess = message.Content;
                if (message.IsClarification && !string.IsNullOrEmpty(message.OriginalUserInput))
                {
                    // Combine original query with clarification
                    messageToProcess = $"{message.OriginalUserInput} - Additional info: {message.Content}";
                    _logger.LogInformation("Processing clarification. Combined message: {Message}", messageToProcess);
                }
                
                // Extract parameters using the existing working service
                _logger.LogInformation("About to call parameter extraction for message: {MessageContent}", messageToProcess);
                var parameters = await ExtractParametersAsync(messageToProcess);
                _logger.LogInformation("Parameter extraction completed: {HasParameters}", parameters != null);
                
                // Determine if we need further clarification based on parameters
                bool needsClarification = NeedsClarification(parameters, messageToProcess);
                
                if (needsClarification && !message.IsClarification)
                {
                    _logger.LogInformation("Clarification needed for user query");
                    
                    // Create clarification message
                    string clarificationMessage = GenerateClarificationMessage(parameters, messageToProcess);
                    
                    // Save the chat history
                    await SaveChatHistoryAsync(userId, message, clarificationMessage);
                    
                    return new ChatResponse
                    {
                        Message = clarificationMessage,
                        UpdatedParameters = parameters,
                        ClarificationNeeded = true,
                        OriginalUserInput = message.Content // Store original query for follow-up
                    };
                }
                else
                {
                    // This is either a complete initial query or a follow-up clarification
                    // Use existing parameters to get recommendations
                    parameters.TextPrompt = messageToProcess;
                    
                    _logger.LogInformation("Extracted parameters: {@Parameters}", parameters);
                    
                    // Save the chat history with a placeholder response
                    await SaveChatHistoryAsync(userId, message, 
                        $"I'm looking for vehicles that match: {messageToProcess}");
                    
                    // Get recommendations based on the parameters
                    var recommendations = await _recommendationService.GetRecommendationsAsync(userId, parameters);
                    
                    // Generate a response based on the parameters and recommendations count
                    string responseMessage = GenerateResponseMessage(parameters, recommendations.Count());
                    
                    return new ChatResponse
                    {
                        Message = responseMessage,
                        RecommendedVehicles = recommendations.ToList(),
                        UpdatedParameters = parameters,
                        ClarificationNeeded = false
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat message for user ID {UserId}", userId);
                return new ChatResponse
                {
                    Message = "I'm sorry, I encountered an error while processing your request. Please try again.",
                    RecommendedVehicles = new List<Vehicle>(),
                    UpdatedParameters = new RecommendationParameters()
                };
            }
        }
        
        // This method determines if we need clarification based on the extracted parameters
        private bool NeedsClarification(RecommendationParameters parameters, string message)
        {
            // Count how many parameter types are missing
            int missingParameterTypes = 0;
            
            // Check if essential parameters are missing
            if (parameters.MinPrice == null && parameters.MaxPrice == null)
                missingParameterTypes++;
                
            if ((parameters.PreferredVehicleTypes == null || !parameters.PreferredVehicleTypes.Any()) &&
                !message.ToLower().Contains("any type") && !message.ToLower().Contains("any vehicle"))
                missingParameterTypes++;
                
            if ((parameters.PreferredMakes == null || !parameters.PreferredMakes.Any()) && 
                !message.ToLower().Contains("any make") && !message.ToLower().Contains("any brand"))
                missingParameterTypes++;
                
            // For first-time queries, if more than one essential parameter type is missing, ask for clarification
            return missingParameterTypes >= 2;
        }
        
        // Generate a clarification message based on missing parameters
        private string GenerateClarificationMessage(RecommendationParameters parameters, string message)
        {
            StringBuilder clarification = new StringBuilder("I'd like to help you find the perfect vehicle, but I need a bit more information. ");
            
            if (parameters.MinPrice == null && parameters.MaxPrice == null)
                clarification.Append("What's your budget range for this vehicle? ");
                
            if (parameters.PreferredVehicleTypes == null || !parameters.PreferredVehicleTypes.Any())
                clarification.Append("What type of vehicle are you interested in (sedan, SUV, hatchback, etc.)? ");
                
            if (parameters.PreferredMakes == null || !parameters.PreferredMakes.Any())
                clarification.Append("Do you have any preferred manufacturers or brands? ");
                
            if (parameters.MinYear == null && parameters.MaxYear == null)
                clarification.Append("How new would you like the vehicle to be? ");
                
            clarification.Append("The more details you can provide, the better I can match you with the right vehicle.");
            
            return clarification.ToString();
        }
        
        // Generate a response message based on parameters and recommendation count
        private string GenerateResponseMessage(RecommendationParameters parameters, int recommendationCount)
        {
            StringBuilder response = new StringBuilder();
            
            if (recommendationCount == 0)
            {
                response.Append("I couldn't find any vehicles matching all your criteria. ");
                response.Append("Consider broadening your search by adjusting your price range, including more vehicle types, or exploring different manufacturers.");
                return response.ToString();
            }
            
            response.Append($"I found {recommendationCount} vehicles that match your preferences. ");
            
            // Add details about what was matched
            if (parameters.PreferredVehicleTypes?.Any() == true)
                response.Append($"Vehicle type: {string.Join(", ", parameters.PreferredVehicleTypes)}. ");
                
            if (parameters.PreferredMakes?.Any() == true)
                response.Append($"Make: {string.Join(", ", parameters.PreferredMakes)}. ");
                
            if (parameters.MinPrice.HasValue || parameters.MaxPrice.HasValue)
            {
                response.Append("Price range: ");
                if (parameters.MinPrice.HasValue)
                    response.Append($"€{parameters.MinPrice:N0} ");
                response.Append("to ");
                if (parameters.MaxPrice.HasValue)
                    response.Append($"€{parameters.MaxPrice:N0}. ");
                else
                    response.Append("any. ");
            }
            
            if (parameters.MinYear.HasValue || parameters.MaxYear.HasValue)
            {
                response.Append("Year: ");
                if (parameters.MinYear.HasValue)
                    response.Append($"{parameters.MinYear} ");
                response.Append("to ");
                if (parameters.MaxYear.HasValue)
                    response.Append($"{parameters.MaxYear}. ");
                else
                    response.Append("present. ");
            }
            
            return response.ToString();
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

        private async Task<RecommendationParameters> ExtractParametersAsync(string message)
        {
            try
            {
                // Get the parameter extraction endpoint from configuration
                var endpoint = _configuration["Services:ParameterExtraction:Endpoint"] ?? "http://localhost:5006/extract_parameters";
                var timeoutSeconds = int.TryParse(_configuration["Services:ParameterExtraction:Timeout"], out var timeout) ? timeout : 30;
                
                _logger.LogInformation("Calling parameter extraction service at {Endpoint}", endpoint);
                
                // Prepare the request
                var request = new { query = message };
                
                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json");
                
                // Configure timeout
                var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                
                // Call the parameter extraction service
                _logger.LogInformation("SENDING REQUEST to {Endpoint} with payload: {Query}", endpoint, message);
                var response = await _httpClient.PostAsync(endpoint, content, timeoutCts.Token);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Parameter extraction service error: {StatusCode}, {ErrorContent}", 
                        response.StatusCode, errorContent);
                    
                    return new RecommendationParameters 
                    { 
                        TextPrompt = message,
                        MaxResults = 5
                    };
                }
                
                // Parse the response
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Parameter extraction service response: {Response}", responseContent);

                using var jsonDoc = JsonDocument.Parse(responseContent);
                var parameters = new RecommendationParameters
                {
                    TextPrompt = message,
                    MaxResults = 5
                };

                // ✅ Parse numerical values safely
                parameters.MinPrice = jsonDoc.RootElement.TryGetProperty("minPrice", out var minPriceElement) && minPriceElement.ValueKind == JsonValueKind.Number
                    ? minPriceElement.GetDecimal()
                    : (decimal?)null;

                parameters.MaxPrice = jsonDoc.RootElement.TryGetProperty("maxPrice", out var maxPriceElement) && maxPriceElement.ValueKind == JsonValueKind.Number
                    ? maxPriceElement.GetDecimal()
                    : (decimal?)null;

                parameters.MinYear = jsonDoc.RootElement.TryGetProperty("minYear", out var minYearElement) && minYearElement.ValueKind == JsonValueKind.Number
                    ? minYearElement.GetInt32()
                    : (int?)null;

                parameters.MaxYear = jsonDoc.RootElement.TryGetProperty("maxYear", out var maxYearElement) && maxYearElement.ValueKind == JsonValueKind.Number
                    ? maxYearElement.GetInt32()
                    : (int?)null;
                parameters.MaxMileage = jsonDoc.RootElement.TryGetProperty("maxMileage", out var mileageElement) &&
                                        mileageElement.ValueKind == JsonValueKind.Number
                    ? mileageElement.GetInt32()
                    : (int?)null;


                // ✅ Parse array values safely
                parameters.PreferredMakes = jsonDoc.RootElement.TryGetProperty("preferredMakes", out var makesElement) && makesElement.ValueKind == JsonValueKind.Array
                    ? makesElement.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String).Select(e => e.GetString()).ToList()
                    : new List<string>();

                parameters.DesiredFeatures = jsonDoc.RootElement.TryGetProperty("desiredFeatures", out var featuresElement) && featuresElement.ValueKind == JsonValueKind.Array
                    ? featuresElement.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String).Select(e => e.GetString()).ToList()
                    : new List<string>();

                // ✅ Parse Enums correctly (Fixes `null` conversion issue)
                if (jsonDoc.RootElement.TryGetProperty("preferredFuelTypes", out var fuelTypesElement) && fuelTypesElement.ValueKind == JsonValueKind.Array)
                {
                    parameters.PreferredFuelTypes = fuelTypesElement.EnumerateArray()
                        .Where(e => e.ValueKind == JsonValueKind.String)
                        .Select(e => Enum.TryParse<FuelType>(e.GetString(), true, out var fuel) ? fuel : (FuelType?)null)
                        .Where(f => f.HasValue)
                        .Select(f => f.Value)
                        .ToList();
                }
                else
                {
                    parameters.PreferredFuelTypes = new List<FuelType>();
                }

                if (jsonDoc.RootElement.TryGetProperty("preferredVehicleTypes", out var vehicleTypesElement) && vehicleTypesElement.ValueKind == JsonValueKind.Array)
                {
                    parameters.PreferredVehicleTypes = vehicleTypesElement.EnumerateArray()
                        .Where(e => e.ValueKind == JsonValueKind.String)
                        .Select(e => Enum.TryParse<VehicleType>(e.GetString(), true, out var vehicle) ? vehicle : (VehicleType?)null)
                        .Where(v => v.HasValue)
                        .Select(v => v.Value)
                        .ToList();
                }
                else
                {
                    parameters.PreferredVehicleTypes = new List<VehicleType>();
                }

                // ✅ Validate the parameters
                if (!RecommendationParameterValidator.Validate(parameters, out var errorMessage))
                {
                    _logger.LogWarning("Parameter validation failed: {ErrorMessage}", errorMessage);
                }
                
                _logger.LogInformation("Final extracted parameters: {Params}", JsonSerializer.Serialize(parameters));
                return parameters;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting parameters from message");

                return new RecommendationParameters 
                { 
                    TextPrompt = message,
                    MaxResults = 5
                };
            }
        }
    }
}