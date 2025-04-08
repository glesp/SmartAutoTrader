using System.Diagnostics;
using System.Text;
using System.Text.Json;
using SmartAutoTrader.API.Models;
using SmartAutoTrader.API.Repositories;
using SmartAutoTrader.API.Validators;

namespace SmartAutoTrader.API.Services
{
    public interface IChatRecommendationService
    {
        Task<ChatResponse> ProcessMessageAsync(int userId, ChatMessage message);
    }

    public class ChatRecommendationService(
        IUserRepository userRepo,
        IChatRepository chatRepo,
        IConfiguration configuration,
        ILogger<ChatRecommendationService> logger,
        HttpClient httpClient,
        IAIRecommendationService recommendationService,
        IConversationContextService contextService) : IChatRecommendationService
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly IUserRepository _userRepo = userRepo;
        private readonly IChatRepository _chatRepo = chatRepo;
        private readonly IConversationContextService _contextService = contextService;
        private readonly HttpClient _httpClient = httpClient;
        private readonly ILogger<ChatRecommendationService> _logger = logger;
        private readonly IAIRecommendationService _recommendationService = recommendationService;

        public async Task<ChatResponse> ProcessMessageAsync(int userId, ChatMessage message)
        {
            try
            {
                _logger.LogInformation("Processing chat message for user ID: {UserId}", userId);

                // Get or create conversation session ID
                int? conversationSessionId = null;
                if (!string.IsNullOrEmpty(message.ConversationId) &&
                    int.TryParse(message.ConversationId, out int sessionId))
                {
                    conversationSessionId = sessionId;
                    _logger.LogInformation("Using existing conversation session: {SessionId}", sessionId);
                }

                // Get conversation context
                ConversationContext conversationContext = await _contextService.GetOrCreateContextAsync(userId);

                string? modelUsedForSession = conversationContext.ModelUsed;

                // Update conversation context with basic tracking info
                conversationContext.MessageCount++;
                conversationContext.LastInteraction = DateTime.UtcNow;

                // Get user context for personalization
                User? user = await _userRepo.GetByIdAsync(userId);

                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found", userId);
                    return new ChatResponse
                    {
                        Message = "Sorry, I couldn't process your request. Please try again later.",
                        ConversationId = message.ConversationId,
                    };
                }

                // Load related entities separately
                user.Favorites = await _userRepo.GetFavoritesWithVehiclesAsync(userId);

                user.BrowsingHistory = await _userRepo
                    .GetRecentBrowsingHistoryWithVehiclesAsync(userId, 5);

                // Determine if this is a clarification or a follow-up query
                string messageToProcess = message.Content;

                // Check for conversation continuity
                bool isFollowUpQuery = IsFollowUpQuery(message.Content, conversationContext);

                if (message.IsClarification && !string.IsNullOrEmpty(message.OriginalUserInput))
                {
                    // Combine original query with clarification
                    messageToProcess = $"{message.OriginalUserInput} - Additional info: {message.Content}";
                    _logger.LogInformation("Processing clarification. Combined message: {Message}", messageToProcess);
                }
                else if (isFollowUpQuery || message.IsFollowUp)
                {
                    // This is a follow-up to the previous query, use the context
                    messageToProcess = $"{conversationContext.LastUserIntent} - Follow-up: {message.Content}";
                    _logger.LogInformation("Processing follow-up query. Combined message: {Message}", messageToProcess);

                    // Analyze message for features or preferences
                    UpdateContextBasedOnMessage(message.Content, conversationContext);
                }

                _logger.LogInformation(
                    "About to call parameter extraction for message: {MessageContent}",
                    messageToProcess);

                Stopwatch sw = Stopwatch.StartNew();
                RecommendationParameters extractedParameters = await ExtractParametersAsync(messageToProcess, modelUsedForSession);
                sw.Stop();

                // ⚠️ NEW: Check if this is a vague query from RAG fallback
                if (!string.IsNullOrEmpty(extractedParameters.RetrieverSuggestion))
                {
                    _logger.LogInformation("Vague RAG match triggered. Suggesting clarification: {Suggestion}", extractedParameters.RetrieverSuggestion);

                    return new ChatResponse
                    {
                        Message = extractedParameters.RetrieverSuggestion,
                        ClarificationNeeded = true,
                        OriginalUserInput = message.Content,
                        ConversationId = message.ConversationId,
                        UpdatedParameters = new RecommendationParameters(), // Empty for now
                    };
                }


                _logger.LogInformation("⏱️ LLM extraction took {ElapsedMs}ms", sw.ElapsedMilliseconds);

                if (extractedParameters == null)
                {
                    _logger.LogError(
                        "🛑 ExtractParametersAsync returned NULL — possible timeout, LLM failure, or parsing issue.");
                    _logger.LogError("[LLM_NULL_PARAMETERS] User message: {Message}", messageToProcess);

                    return new ChatResponse
                    {
                        Message =
                            "Sorry, I couldn't process your request. Could you try rephrasing or be a bit more specific?",
                        UpdatedParameters = conversationContext.CurrentParameters ?? new RecommendationParameters(),
                        ClarificationNeeded = true,
                        OriginalUserInput = message.Content,
                        ConversationId = message.ConversationId,
                    };
                }

                if (extractedParameters.IsOffTopic && !string.IsNullOrEmpty(extractedParameters.OffTopicResponse))
                {
                    // Return the off-topic response directly without further processing
                    await SaveChatHistoryAsync(
                        userId,
                        message,
                        extractedParameters.OffTopicResponse,
                        conversationSessionId);

                    return new ChatResponse
                    {
                        Message = extractedParameters.OffTopicResponse,
                        RecommendedVehicles =[],
                        UpdatedParameters = new RecommendationParameters(),
                        ClarificationNeeded = false,
                        ConversationId = message.ConversationId,
                    };
                }

                _logger.LogInformation("Parameter extraction completed successfully");

                // Merge with existing parameters if this is a follow-up or clarification
                RecommendationParameters parameters;
                if ((isFollowUpQuery || message.IsFollowUp || message.IsClarification) &&
                    conversationContext.CurrentParameters != null)
                {
                    parameters = MergeParameters(conversationContext.CurrentParameters, extractedParameters);
                    _logger.LogInformation("Merged parameters from context and new extraction");
                }
                else
                {
                    parameters = extractedParameters;
                }

                // Store the original message as last user intent
                conversationContext.LastUserIntent = messageToProcess;

                // Update the conversation context with new parameters
                conversationContext.CurrentParameters = parameters;

                // Save context
                await _contextService.UpdateContextAsync(userId, conversationContext);

                // Determine if we need further clarification based on parameters
                bool needsClarification = NeedsClarification(parameters, messageToProcess);

                if (needsClarification && !message.IsClarification)
                {
                    _logger.LogInformation("Clarification needed for user query");

                    // Create clarification message based on context
                    string clarificationMessage =
                        GenerateClarificationMessage(parameters, messageToProcess, conversationContext);

                    // Save the chat history
                    await SaveChatHistoryAsync(userId, message, clarificationMessage, conversationSessionId);

                    return new ChatResponse
                    {
                        Message = clarificationMessage,
                        UpdatedParameters = parameters,
                        ClarificationNeeded = true,
                        OriginalUserInput = message.Content,
                        ConversationId = message.ConversationId,
                    };
                }

                // This is either a complete initial query or a follow-up clarification
                parameters.TextPrompt = messageToProcess;

                _logger.LogInformation("Using parameters: {@Parameters}", parameters);

                // Save the chat history with a placeholder response
                await SaveChatHistoryAsync(
                    userId,
                    message,
                    $"I'm looking for vehicles that match: {messageToProcess}",
                    conversationSessionId);

                // Get recommendations based on the parameters
                IEnumerable<Vehicle> recommendations =
                    await _recommendationService.GetRecommendationsAsync(userId, parameters);

                // Track which vehicles were shown to the user
                List<int> vehicleIds = recommendations.Select(v => v.Id).ToList();
                foreach (int id in vehicleIds)
                {
                    if (!conversationContext.ShownVehicleIds.Contains(id))
                    {
                        conversationContext.ShownVehicleIds.Add(id);
                    }
                }

                // Save the updated context with shown vehicles
                await _contextService.UpdateContextAsync(userId, conversationContext);

                // Generate a response based on the parameters, recommendations, and context
                string responseMessage = GenerateResponseMessage(
                    parameters,
                    recommendations.Count(),
                    conversationContext);

                return new ChatResponse
                {
                    Message = responseMessage,
                    RecommendedVehicles = recommendations.ToList(),
                    UpdatedParameters = parameters,
                    ClarificationNeeded = false,
                    ConversationId = message.ConversationId,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat message for user ID {UserId}", userId);
                return new ChatResponse
                {
                    Message = "I'm sorry, I encountered an error while processing your request. Please try again.",
                    RecommendedVehicles =[],
                    UpdatedParameters = new RecommendationParameters(),
                    ConversationId = message.ConversationId,
                };
            }
        }

        // Determine if a message is a follow-up to the previous conversation
        private bool IsFollowUpQuery(string message, ConversationContext context)
        {
            // If this is the first message in the conversation, it's not a follow-up
            if (context.MessageCount <= 1 || string.IsNullOrEmpty(context.LastUserIntent))
            {
                return false;
            }

            // Check if the message is short (likely to be a follow-up)
            if (message.Split(' ').Length <= 4)
            {
                return true;
            }

            // Check for follow-up indicators
            string lowerMessage = message.ToLower(System.Globalization.CultureInfo.CurrentCulture);
            string[] followUpIndicators =
            [
                "instead",
                "also",
                "but",
                "what about",
                "how about",
                "can you",
                "show me",
                "i want",
                "i prefer",
                "i'd like",
                "actually",
                "change",
                "modify",
                "update",
                "rather"
            ];

            if (followUpIndicators.Any(lowerMessage.Contains))
            {
                return true;
            }

            // Check for pronouns that might refer to previous context
            string[] contextualPronouns =["it", "that", "these", "those", "them"];
            if (contextualPronouns.Any(pronoun => lowerMessage.Contains($" {pronoun} ")))
            {
                return true;
            }

            // If the last interaction was recent (< 2 minutes ago), more likely to be a follow-up
            if ((DateTime.UtcNow - context.LastInteraction).TotalMinutes < 2)
            {
                // Apply more relaxed criteria for recent interactions
                return true;
            }

            return false;
        }

        // Save chat history to the database
        private async Task SaveChatHistoryAsync(
            int userId,
            ChatMessage userMessage,
            string aiResponse,
            int? conversationSessionId = null)
        {
            try
            {
                // Create a chat history record
                ChatHistory chatHistory = new()
                {
                    UserId = userId,
                    UserMessage = userMessage.Content,
                    AIResponse = aiResponse,
                    Timestamp = DateTime.UtcNow,
                    ConversationSessionId = conversationSessionId,
                };

                await _chatRepo.AddChatHistoryAsync(chatHistory);
                await _chatRepo.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving chat history for user ID {UserId}", userId);

                // Continue even if saving history fails
            }
        }

        // Update context based on message content
        private void UpdateContextBasedOnMessage(string message, ConversationContext context)
        {
            string[] featureKeywords =
            [
                "bluetooth",
                "navigation",
                "leather",
                "sunroof",
                "camera",
                "cruise control",
                "parking sensors",
                "heated seats",
                "air conditioning",
                "automatic",
                "manual",
                "safety",
                "fuel efficient",
                "family",
                "sport"
            ];

            string lowerMessage = message.ToLower(System.Globalization.CultureInfo.CurrentCulture);

            foreach (string feature in featureKeywords)
            {
                if (lowerMessage.Contains(feature) && !context.MentionedVehicleFeatures.Contains(feature))
                {
                    context.MentionedVehicleFeatures.Add(feature);
                }
            }

            // Detect explicitly rejected options
            if (lowerMessage.Contains("not ") || lowerMessage.Contains("don't want") ||
                lowerMessage.Contains("no ") || lowerMessage.Contains("except"))
            {
                foreach (string? make in new[] { "bmw", "audi", "ford", "toyota", "honda" })
                {
                    if ((lowerMessage.Contains($"not {make}") || lowerMessage.Contains($"no {make}")) &&
                        !context.ExplicitlyRejectedOptions.Contains(make))
                    {
                        context.ExplicitlyRejectedOptions.Add(make);
                    }
                }
            }

            // Track topic context (for conversation flow management)
            if (lowerMessage.Contains("budget") || lowerMessage.Contains("price") ||
                lowerMessage.Contains("cost") || lowerMessage.Contains("afford"))
            {
                context.TopicContext["discussing_budget"] = true;
            }

            if (lowerMessage.Contains("family") || lowerMessage.Contains("kids") ||
                lowerMessage.Contains("children") || lowerMessage.Contains("baby"))
            {
                context.TopicContext["discussing_family_needs"] = true;
            }

            if (lowerMessage.Contains("fuel") || lowerMessage.Contains("gas") ||
                lowerMessage.Contains("diesel") || lowerMessage.Contains("electric") ||
                lowerMessage.Contains("economy") || lowerMessage.Contains("consumption"))
            {
                context.TopicContext["discussing_fuel_economy"] = true;
            }
        }

        // Merge parameters from a new query with existing parameters from context
        private static RecommendationParameters MergeParameters(
            RecommendationParameters existingParams,
            RecommendationParameters newParams)
        {
            // Start with the new parameters
            RecommendationParameters mergedParams = newParams;

            // For each field, prefer new values if present, otherwise keep the existing ones

            // Price range
            mergedParams.MinPrice ??= existingParams.MinPrice;
            mergedParams.MaxPrice ??= existingParams.MaxPrice;

            // Year range
            mergedParams.MinYear ??= existingParams.MinYear;
            mergedParams.MaxYear ??= existingParams.MaxYear;

            // Vehicle types - MERGE instead of replace
            if (mergedParams.PreferredVehicleTypes != null && existingParams.PreferredVehicleTypes != null)
            {
                mergedParams.PreferredVehicleTypes = mergedParams.PreferredVehicleTypes
                    .Union(existingParams.PreferredVehicleTypes)
                    .ToList();
            }
            else if (existingParams.PreferredVehicleTypes != null)
            {
                mergedParams.PreferredVehicleTypes = existingParams.PreferredVehicleTypes;
            }

            // Makes - MERGE instead of replace
            if (mergedParams.PreferredMakes != null && existingParams.PreferredMakes != null)
            {
                mergedParams.PreferredMakes = mergedParams.PreferredMakes
                    .Union(existingParams.PreferredMakes)
                    .ToList();
            }
            else if (existingParams.PreferredMakes != null)
            {
                mergedParams.PreferredMakes = existingParams.PreferredMakes;
            }

            // Fuel types - MERGE instead of replace
            if (mergedParams.PreferredFuelTypes != null && existingParams.PreferredFuelTypes != null)
            {
                mergedParams.PreferredFuelTypes = mergedParams.PreferredFuelTypes
                    .Union(existingParams.PreferredFuelTypes)
                    .ToList();
            }
            else if (existingParams.PreferredFuelTypes != null)
            {
                mergedParams.PreferredFuelTypes = existingParams.PreferredFuelTypes;
            }

            // Features - MERGE instead of replace
            if (mergedParams.DesiredFeatures != null && existingParams.DesiredFeatures != null)
            {
                mergedParams.DesiredFeatures = mergedParams.DesiredFeatures
                    .Union(existingParams.DesiredFeatures)
                    .ToList();
            }
            else if (existingParams.DesiredFeatures != null)
            {
                mergedParams.DesiredFeatures = existingParams.DesiredFeatures;
            }

            // Max mileage
            mergedParams.MaxMileage ??= existingParams.MaxMileage;

            return mergedParams;
        }

        // This method determines if we need clarification based on the extracted parameters
        private static bool NeedsClarification(RecommendationParameters parameters, string message)
        {
            // Count how many parameter types are missing
            int missingParameterTypes = 0;

            // Check if essential parameters are missing
            if (parameters.MinPrice == null && parameters.MaxPrice == null)
            {
                missingParameterTypes++;
            }

            if ((parameters.PreferredVehicleTypes == null || !parameters.PreferredVehicleTypes.Any()) &&
                !message.Contains("any type", StringComparison.CurrentCultureIgnoreCase) && !message.Contains(
                    "any vehicle",
                    StringComparison.CurrentCultureIgnoreCase))
            {
                missingParameterTypes++;
            }

            if ((parameters.PreferredMakes == null || !parameters.PreferredMakes.Any()) &&
                !message.Contains("any make", StringComparison.CurrentCultureIgnoreCase) && !message.Contains(
                    "any brand",
                    StringComparison.CurrentCultureIgnoreCase))
            {
                missingParameterTypes++;
            }

            // same for year
            if (parameters.MinYear == null && parameters.MaxYear == null)
            {
                missingParameterTypes++;
            }

            // same for mileage
            if (parameters.MaxMileage == null)
            {
                missingParameterTypes++;
            }

            // same for preferred fuel type
            if (parameters.PreferredFuelTypes == null || !parameters.PreferredFuelTypes.Any())
            {
                missingParameterTypes++;
            }


            // Ask for clarification is more than 3 parameter types are missing
            return missingParameterTypes >= 3;
        }

        // Generate a personalized clarification message based on context
        private static string GenerateClarificationMessage(
            RecommendationParameters parameters,
            string message,
            ConversationContext context)
        {
            StringBuilder clarification = new();

            // If this is a follow-up question, acknowledge the previous context
            if (context.MessageCount > 1)
            {
                _ = clarification.Append("Building on our previous conversation, ");
            }

            _ = clarification.Append(
                "I'd like to help you find the perfect vehicle, but I need a bit more information. ");

            // Ask about missing parameters, considering context
            if (parameters.MinPrice == null && parameters.MaxPrice == null)
            {
                _ = context.TopicContext.ContainsKey("discussing_budget")
                    ? clarification.Append("Could you provide a specific price range you're comfortable with? ")
                    : clarification.Append("What's your budget range for this vehicle? ");
            }

            if (parameters.PreferredVehicleTypes == null || !parameters.PreferredVehicleTypes.Any())
            {
                _ = context.TopicContext.ContainsKey("discussing_family_needs")
                    ? clarification.Append(
                        "Since you mentioned family needs, what type of vehicle would work best for you - perhaps an SUV, minivan, or sedan? ")
                    : clarification.Append(
                        "What type of vehicle are you interested in (sedan, SUV, hatchback, etc.)? ");
            }

            if (parameters.PreferredMakes == null || !parameters.PreferredMakes.Any())
            {
                // Check if user has rejected any makes
                _ = context.ExplicitlyRejectedOptions.Any()
                    ? clarification.Append(
                        $"You mentioned you don't want {string.Join(", ", context.ExplicitlyRejectedOptions)}. Are there any specific makes you're interested in instead? ")
                    : clarification.Append("Do you have any preferred manufacturers or brands? ");
            }

            if (parameters.MinYear == null && parameters.MaxYear == null)
            {
                _ = clarification.Append("How new would you like the vehicle to be? ");
            }

            _ = clarification.Append(
                "The more details you can provide, the better I can match you with the right vehicle.");

            return clarification.ToString();
        }

        // Generate a personalized response message based on context
        private static string GenerateResponseMessage(
            RecommendationParameters parameters,
            int recommendationCount,
            ConversationContext context)
        {
            StringBuilder response = new();

            if (recommendationCount == 0)
            {
                _ = response.Append("I couldn't find any vehicles matching all your criteria. ");

                // Suggest relaxing constraints based on context
                if (parameters.MinPrice.HasValue && parameters.MaxPrice.HasValue &&
                    parameters.MaxPrice.Value - parameters.MinPrice.Value < 10000)
                {
                    _ = response.Append("Consider broadening your price range. ");
                }

                if (parameters.PreferredMakes?.Count == 1)
                {
                    _ = response.Append("Try including more manufacturers in your search. ");
                }

                if (parameters.PreferredVehicleTypes?.Count == 1)
                {
                    _ = response.Append("Consider exploring different vehicle types. ");
                }

                return response.ToString();
            }

            // Personalize based on conversation history
            if (context.MessageCount > 1)
            {
                _ = response.Append("Based on our conversation, ");
            }

            _ = response.Append($"I found {recommendationCount} vehicles that match your preferences. ");

            // Add details about what was matched
            if (parameters.PreferredVehicleTypes?.Any() == true)
            {
                _ = response.Append($"Vehicle type: {string.Join(", ", parameters.PreferredVehicleTypes)}. ");
            }

            if (parameters.PreferredMakes?.Any() == true)
            {
                _ = response.Append($"Make: {string.Join(", ", parameters.PreferredMakes)}. ");
            }

            if (parameters.MinPrice.HasValue || parameters.MaxPrice.HasValue)
            {
                _ = response.Append("Price range: ");
                if (parameters.MinPrice.HasValue)
                {
                    _ = response.Append($"€{parameters.MinPrice:N0} ");
                }

                _ = response.Append("to ");
                _ = parameters.MaxPrice.HasValue ? response.Append($"€{parameters.MaxPrice:N0}. ") : response.Append("any. ");
            }

            if (parameters.MinYear.HasValue || parameters.MaxYear.HasValue)
            {
                _ = response.Append("Year: ");
                if (parameters.MinYear.HasValue)
                {
                    _ = response.Append($"{parameters.MinYear} ");
                }

                _ = response.Append("to ");
                _ = parameters.MaxYear.HasValue ? response.Append($"{parameters.MaxYear}. ") : response.Append("present. ");
            }

            // Add personalized guidance based on context
            if (context.TopicContext.ContainsKey("discussing_family_needs"))
            {
                _ = response.Append(
                    "These options should provide good space and safety features for your family needs. ");
            }

            if (context.TopicContext.ContainsKey("discussing_fuel_economy"))
            {
                _ = response.Append("I've focused on vehicles with good fuel efficiency based on your requirements. ");
            }

            // Contextual follow-up cues
            if (context.MentionedVehicleFeatures.Any())
            {
                _ = response.Append(
                    "You can ask me for more details about any of these vehicles, including specific features. ");
            }

            return response.ToString();
        }

        // Extract parameters from message using the Python parameter extraction service
        private async Task<RecommendationParameters> ExtractParametersAsync(string message, string? forceModelOverride = null)
        {
            try
            {
                // Get the parameter extraction endpoint from configuration
                string endpoint = _configuration["Services:ParameterExtraction:Endpoint"] ??
                                  "http://localhost:5006/extract_parameters";
                int timeoutSeconds = int.TryParse(
                    _configuration["Services:ParameterExtraction:Timeout"],
                    out int timeout) ? timeout : 30;

                // Optionally get the force model flag from configuration (e.g., "fast", "refine", or "clarify")
                string? forceModel = _configuration["Services:ParameterExtraction:ForceModel"];

                _logger.LogInformation("Calling parameter extraction service at {Endpoint}", endpoint);

                // Prepare the request payload including the forceModel flag if set
                var requestPayload = new
                {
                    query = message,
                    forceModel = forceModelOverride,
                };

                StringContent content = new(
                    JsonSerializer.Serialize(requestPayload),
                    Encoding.UTF8,
                    "application/json");

                // Configure timeout
                CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(timeoutSeconds));

                _logger.LogInformation(
                    "SENDING REQUEST to {Endpoint} with payload: {Payload}",
                    endpoint, JsonSerializer.Serialize(requestPayload));
                HttpResponseMessage response = await _httpClient.PostAsync(endpoint, content, timeoutCts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError(
                        "Parameter extraction service error: {StatusCode}, {ErrorContent}",
                        response.StatusCode,
                        errorContent);

                    return new RecommendationParameters
                    {
                        TextPrompt = message,
                        MaxResults = 5,
                    };
                }

                // Parse the response
                string responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Parameter extraction service response: {Response}", responseContent);

                using JsonDocument jsonDoc = JsonDocument.Parse(responseContent);
                RecommendationParameters parameters = new()
                {
                    TextPrompt = message,
                    MaxResults = 5,

                    // Parse numerical values safely
                    MinPrice = jsonDoc.RootElement.TryGetProperty("minPrice", out JsonElement minPriceElement) &&
                               minPriceElement.ValueKind == JsonValueKind.Number
                        ? minPriceElement.GetDecimal()
                        : null,

                    MaxPrice = jsonDoc.RootElement.TryGetProperty("maxPrice", out JsonElement maxPriceElement) &&
                               maxPriceElement.ValueKind == JsonValueKind.Number
                        ? maxPriceElement.GetDecimal()
                        : null,

                    MinYear = jsonDoc.RootElement.TryGetProperty("minYear", out JsonElement minYearElement) &&
                              minYearElement.ValueKind == JsonValueKind.Number
                        ? minYearElement.GetInt32()
                        : null,

                    MaxYear = jsonDoc.RootElement.TryGetProperty("maxYear", out JsonElement maxYearElement) &&
                              maxYearElement.ValueKind == JsonValueKind.Number
                        ? maxYearElement.GetInt32()
                        : null,

                    MaxMileage = jsonDoc.RootElement.TryGetProperty("maxMileage", out JsonElement mileageElement) &&
                                 mileageElement.ValueKind == JsonValueKind.Number
                        ? mileageElement.GetInt32()
                        : null,

                    // Parse array values safely
                    PreferredMakes =
                        jsonDoc.RootElement.TryGetProperty("preferredMakes", out JsonElement makesElement) &&
                        makesElement.ValueKind == JsonValueKind.Array
                            ? makesElement.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String)
                                .Select(e => e.GetString()).ToList()
                            :[],

                    DesiredFeatures =
                        jsonDoc.RootElement.TryGetProperty("desiredFeatures", out JsonElement featuresElement) &&
                        featuresElement.ValueKind == JsonValueKind.Array
                            ? featuresElement.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String)
                                .Select(e => e.GetString()).ToList()
                            :[],

                    // Parse enums correctly
                    PreferredFuelTypes = jsonDoc.RootElement.TryGetProperty("preferredFuelTypes", out JsonElement fuelTypesElement) &&
                                         fuelTypesElement.ValueKind == JsonValueKind.Array
                        ? fuelTypesElement.EnumerateArray()
                            .Where(e => e.ValueKind == JsonValueKind.String)
                            .Select(e => Enum.TryParse(e.GetString(), true, out FuelType fuel)
                                ? fuel
                                : (FuelType?)null)
                            .Where(f => f.HasValue)
                            .Select(f => f.Value)
                            .ToList()
                        :[],

                    PreferredVehicleTypes = jsonDoc.RootElement.TryGetProperty("preferredVehicleTypes", out JsonElement vehicleTypesElement) &&
                                            vehicleTypesElement.ValueKind == JsonValueKind.Array
                        ? vehicleTypesElement.EnumerateArray()
                            .Where(e => e.ValueKind == JsonValueKind.String)
                            .Select(e => Enum.TryParse(e.GetString(), true, out VehicleType vehicle)
                                ? vehicle
                                : (VehicleType?)null)
                            .Where(v => v.HasValue)
                            .Select(v => v.Value)
                            .ToList()
                        :[],
                };

                // Validate the parameters
                if (!RecommendationParameterValidator.Validate(parameters, out string? errorMessage))
                {
                    _logger.LogWarning("Parameter validation failed: {ErrorMessage}", errorMessage);
                }

                // Parse the off-topic flags
                parameters.IsOffTopic = jsonDoc.RootElement.TryGetProperty("isOffTopic", out JsonElement isOffTopicElement)
                                        && isOffTopicElement.ValueKind == JsonValueKind.True;

                if (parameters.IsOffTopic && jsonDoc.RootElement.TryGetProperty("offTopicResponse", out JsonElement responseElement)
                    && responseElement.ValueKind == JsonValueKind.String)
                {
                    parameters.OffTopicResponse = responseElement.GetString();
                }

                // Parse retriever suggestion
                if (jsonDoc.RootElement.TryGetProperty("retrieverSuggestion", out JsonElement retrieverSuggestionElement) &&
                    retrieverSuggestionElement.ValueKind == JsonValueKind.String)
                {
                    parameters.RetrieverSuggestion = retrieverSuggestionElement.GetString();
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
                    MaxResults = 5,
                };
            }
        }

    }
}