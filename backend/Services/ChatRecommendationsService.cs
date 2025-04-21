// <copyright file="ChatRecommendationsService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SmartAutoTrader.API.Services
{
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;
    using System.Text.Json;
    using SmartAutoTrader.API.Enums;
    using SmartAutoTrader.API.Helpers;
    using SmartAutoTrader.API.Models;
    using SmartAutoTrader.API.Repositories;

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
        private readonly IChatRepository chatRepo = chatRepo;
        private readonly IConfiguration configuration = configuration;
        private readonly IConversationContextService contextService = contextService;
        private readonly HttpClient httpClient = httpClient;
        private readonly ILogger<ChatRecommendationService> logger = logger;
        private readonly IAIRecommendationService recommendationService = recommendationService;
        private readonly IUserRepository userRepo = userRepo;

        // Define the model strategies
        private readonly string[] modelStrategies = ["fast", "refine", "clarify"];

        /// <inheritdoc/>
        public async Task<ChatResponse> ProcessMessageAsync(int userId, ChatMessage message)
        {
            try
            {
                this.logger.LogInformation("Processing chat message for user ID: {UserId}", userId);

                // Explain: Log the ConversationId received by the Service from the Controller.
                this.logger.LogInformation(
                    "ChatService received message with ConversationId: {ConversationId}",
                    message.ConversationId ?? "NULL");

                // Get or create conversation session ID
                int? conversationSessionId = null;
                if (!string.IsNullOrEmpty(message.ConversationId) &&
                    int.TryParse(message.ConversationId, out int sessionId))
                {
                    conversationSessionId = sessionId;
                    this.logger.LogInformation(
                        "Parsed conversationSessionId as integer: {SessionId}",
                        conversationSessionId);
                }
                else
                {
                    // Explain: Log warning if parsing failed or ID was null/empty.
                    this.logger.LogWarning(
                        "Could not parse or find ConversationId from message. conversationSessionId will be null.");
                }

                // Get conversation context
                ConversationContext conversationContext = await this.contextService.GetOrCreateContextAsync(userId);

                // Determine the model strategy to use
                string modelUsedForSession;
                if (string.IsNullOrEmpty(conversationContext.ModelUsed))
                {
                    // Randomly select a model strategy if none is set
                    Random random = new();
                    modelUsedForSession = this.modelStrategies[random.Next(this.modelStrategies.Length)];

                    // Set the selected strategy in the context
                    conversationContext.ModelUsed = modelUsedForSession;

                    // Save the updated context with the selected model
                    await this.contextService.UpdateContextAsync(userId, conversationContext);
                    this.logger.LogInformation(
                        "Randomly selected model strategy: {ModelStrategy}",
                        modelUsedForSession);
                }
                else
                {
                    // Use the existing model strategy
                    modelUsedForSession = conversationContext.ModelUsed;
                    this.logger.LogInformation("Using existing model strategy: {ModelStrategy}", modelUsedForSession);
                }

                // Update conversation context with basic tracking info
                conversationContext.MessageCount++;
                conversationContext.LastInteraction = DateTime.UtcNow;

                // Get recent conversation history
                List<ConversationTurn> recentHistory = [];
                if (conversationSessionId.HasValue)
                {
                    // Explain: Log just before attempting to fetch history from the database.
                    this.logger.LogInformation(
                        "Attempting to fetch history for Session ID: {SessionId}",
                        conversationSessionId.Value);

                    recentHistory = await this.chatRepo.GetRecentHistoryAsync(
                        userId,
                        conversationSessionId.Value,
                        3); // Get last 3 exchanges

                    // Explain: Log how many history items were actually returned by the repository for this session ID.
                    this.logger.LogInformation(
                        "Fetched {HistoryCount} items from history for Session ID: {SessionId}",
                        recentHistory.Count, conversationSessionId.Value);
                }
                else
                {
                    // Explain: Log warning because history fetching was skipped due to missing ID.
                    this.logger.LogWarning("No valid conversationSessionId available, history fetching skipped.");
                }

                this.logger.LogInformation(
                    "Retrieved {HistoryCount} conversation history items for user {UserId}",
                    recentHistory.Count, userId);

                // Get user context for personalization
                User? user = await this.userRepo.GetByIdAsync(userId);

                if (user == null)
                {
                    this.logger.LogWarning("User with ID {UserId} not found", userId);
                    return new ChatResponse
                    {
                        Message = "Sorry, I couldn't process your request. Please try again later.",
                        UpdatedParameters = new RecommendationParameters(),
                        ClarificationNeeded = false,
                        ConversationId = message.ConversationId,
                    };
                }

                // Load related entities separately
                user.Favorites = await this.userRepo.GetFavoritesWithVehiclesAsync(userId);

                user.BrowsingHistory = await this.userRepo
                    .GetRecentBrowsingHistoryWithVehiclesAsync(userId);

                // Determine if this is a clarification or a follow-up query
                string messageToProcess = message.Content;

                // Check for conversation continuity
                bool isFollowUpQuery = this.IsFollowUpQuery(message.Content, conversationContext);

                if (message.IsClarification && !string.IsNullOrEmpty(message.OriginalUserInput))
                {
                    // Combine original query with clarification
                    messageToProcess = $"{message.OriginalUserInput} - Additional info: {message.Content}";
                    this.logger.LogInformation(
                        "Processing clarification. Combined message: {Message}",
                        messageToProcess);
                }
                else if (isFollowUpQuery || message.IsFollowUp)
                {
                    // This is a follow-up to the previous query, use the context
                    messageToProcess = $"{conversationContext.LastUserIntent} - Follow-up: {message.Content}";
                    this.logger.LogInformation(
                        "Processing follow-up query. Combined message: {Message}",
                        messageToProcess);

                    // Analyze message for features or preferences
                    this.UpdateContextBasedOnMessage(message.Content, conversationContext);
                }

                this.logger.LogInformation(
                    "About to call parameter extraction for message: {MessageContent}",
                    messageToProcess);

                Stopwatch sw = Stopwatch.StartNew();

                // Pass the recent history and model strategy to the extraction method
                RecommendationParameters extractedParameters = await this.ExtractParametersAsync(
                    messageToProcess,
                    modelUsedForSession, // Pass the model strategy
                    recentHistory,
                    conversationContext); // Pass the context
                sw.Stop();

                // Update the structured context to process any new confirmations/rejections
                await this.UpdateStructuredContextFromParametersAsync(
                    extractedParameters, 
                    conversationContext, 
                    message.Content);

                // Merge parameters, passing the context to check against rejections
                RecommendationParameters parameters = MergeParameters(
                    conversationContext.CurrentParameters,
                    extractedParameters,
                    extractedParameters.Intent,
                    conversationContext);

                // Update the context's CurrentParameters property
                conversationContext.CurrentParameters = parameters;

                // ⚠️ NEW: Check if this is a vague query from RAG fallback
                if (!string.IsNullOrEmpty(extractedParameters.RetrieverSuggestion))
                {
                    this.logger.LogInformation(
                        "Vague RAG match triggered. Suggesting clarification: {Suggestion}",
                        extractedParameters.RetrieverSuggestion);

                    // Explain: Add history saving before returning the RAG clarification suggestion.
                    await this.SaveChatHistoryAsync(userId, message, extractedParameters.RetrieverSuggestion,
                        conversationSessionId);

                    return new ChatResponse
                    {
                        Message = extractedParameters.RetrieverSuggestion,
                        ClarificationNeeded = true,
                        OriginalUserInput = message.Content,
                        ConversationId = message.ConversationId,
                        UpdatedParameters = new RecommendationParameters(), // Empty for now
                    };
                }

                this.logger.LogInformation("⏱️ LLM extraction took {ElapsedMs}ms", sw.ElapsedMilliseconds);

                if (extractedParameters == null)
                {
                    this.logger.LogError(
                        "🛑 ExtractParametersAsync returned NULL — possible timeout, LLM failure, or parsing issue.");
                    this.logger.LogError("[LLM_NULL_PARAMETERS] User message: {Message}", messageToProcess);

                    return new ChatResponse
                    {
                        Message =
                            "Sorry, I couldn't process your request. Could you try rephrasing or be a bit more specific?",
                        UpdatedParameters = conversationContext.CurrentParameters,
                        ClarificationNeeded = true,
                        OriginalUserInput = message.Content,
                        ConversationId = message.ConversationId,
                    };
                }

                if (extractedParameters.IsOffTopic && !string.IsNullOrEmpty(extractedParameters.OffTopicResponse))
                {
                    // Return the off-topic response directly without further processing
                    await this.SaveChatHistoryAsync(
                        userId,
                        message,
                        extractedParameters.OffTopicResponse,
                        conversationSessionId);

                    return new ChatResponse
                    {
                        Message = extractedParameters.OffTopicResponse,
                        RecommendedVehicles = [],
                        UpdatedParameters = new RecommendationParameters(),
                        ClarificationNeeded = false,
                        ConversationId = message.ConversationId,
                    };
                }

                this.logger.LogInformation("Parameter extraction completed successfully");

                // Determine if we need further clarification based on parameters
                bool needsClarification = false;
                bool hasExplicitNegations = false;

                // Check for explicitly negated collections in the extracted parameters
                if (extractedParameters != null)
                {
                    // Check if any of the explicitly_negated collections exist and have items
                    hasExplicitNegations = 
                        (extractedParameters.ExplicitlyNegatedMakes?.Any() == true) ||
                        (extractedParameters.ExplicitlyNegatedVehicleTypes?.Any() == true) ||
                        (extractedParameters.ExplicitlyNegatedFuelTypes?.Any() == true);
                    
                    this.logger.LogInformation("Explicit negations detected: {HasNegations}", hasExplicitNegations);
                }

                // Skip clarification if the intent is 'refine_criteria' and we have explicit negations
                if (parameters.Intent?.Equals("refine_criteria", StringComparison.OrdinalIgnoreCase) == true && 
                    hasExplicitNegations)
                {
                    this.logger.LogInformation("Explicit negation detected with refine intent - skipping clarification");
                    needsClarification = false;
                }
                else
                {
                    // Otherwise use the normal clarification logic
                    needsClarification = NeedsClarification(parameters, messageToProcess, conversationContext);
                }

                if (needsClarification && !message.IsClarification)
                {
                    this.logger.LogInformation("Clarification needed for user query");

                    // Create clarification message based on context
                    string clarificationMessage = GenerateClarificationMessage(parameters, messageToProcess, conversationContext);

                    // Avoid asking the same question again
                    if (clarificationMessage == conversationContext.LastQuestionAskedByAI)
                    {
                        this.logger.LogInformation("Avoiding duplicate question, generating an alternate");
                        // Generate an alternative question
                        clarificationMessage = "Could you tell me more about what you're looking for in a vehicle?";
                        conversationContext.LastQuestionAskedByAI = clarificationMessage;
                    }

                    // Save the chat history
                    await this.SaveChatHistoryAsync(userId, message, clarificationMessage, conversationSessionId);

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

                this.logger.LogInformation("Using parameters: {@Parameters}", parameters);

                // Save the chat history with a placeholder response
                await this.SaveChatHistoryAsync(
                    userId,
                    message,
                    $"I'm looking for vehicles that match: {messageToProcess}",
                    conversationSessionId);

                // Get recommendations based on the parameters
                IEnumerable<Vehicle> recommendations =
                    await this.recommendationService.GetRecommendationsAsync(userId, parameters);

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
                await this.contextService.UpdateContextAsync(userId, conversationContext);

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
                this.logger.LogError(ex, "Error processing chat message for user ID {UserId}", userId);
                return new ChatResponse
                {
                    Message = "I'm sorry, I encountered an error while processing your request. Please try again.",
                    RecommendedVehicles = [],
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
            string lowerMessage = message.ToLower(CultureInfo.CurrentCulture);
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
            string[] contextualPronouns = ["it", "that", "these", "those", "them"];
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

                // Explain: Log the Session ID *being assigned* to the history record before saving.
                this.logger.LogInformation(
                    "Attempting SaveChatHistoryAsync for User: {UserId}, Session ID being saved: {SessionId}",
                    userId, conversationSessionId ?? -1); // Log -1 if null for clarity

                await this.chatRepo.AddChatHistoryAsync(chatHistory);
                await this.chatRepo.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error saving chat history for user ID {UserId}", userId);

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

            string lowerMessage = message.ToLower(CultureInfo.CurrentCulture);

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
            RecommendationParameters newParams,
            string? userIntent = null,
            ConversationContext? context = null) // Add context parameter
        {
            // First, filter out any rejected makes from newParams
            if (context != null && newParams.PreferredMakes.Any() == true)
            {
                newParams.PreferredMakes = newParams.PreferredMakes
                    .Where(m => !context.RejectedMakes.Contains(m, StringComparer.OrdinalIgnoreCase))
                    .ToList();
            }

            // Filter out rejected vehicle types
            if (context != null && newParams.PreferredVehicleTypes.Any() == true)
            {
                newParams.PreferredVehicleTypes = newParams.PreferredVehicleTypes
                    .Where(t => !context.RejectedVehicleTypes.Contains(t))
                    .ToList();
            }

            // Filter out rejected fuel types 
            if (context != null && newParams.PreferredFuelTypes.Any() == true)
            {
                newParams.PreferredFuelTypes = newParams.PreferredFuelTypes
                    .Where(f => !context.RejectedFuelTypes.Contains(f))
                    .ToList();
            }

            // Filter out rejected features
            if (context != null && newParams.DesiredFeatures.Any() == true)
            {
                newParams.DesiredFeatures = newParams.DesiredFeatures
                    .Where(f => !context.RejectedFeatures.Contains(f, StringComparer.OrdinalIgnoreCase))
                    .ToList();
            }

            // Start with a copy of the existing parameters for refinement scenarios
            RecommendationParameters mergedParams =
                userIntent?.Equals("refine_criteria", StringComparison.OrdinalIgnoreCase) == true
                    ? new RecommendationParameters
                    {
                        // Copy the existing parameters first
                        MinPrice = existingParams.MinPrice,
                        MaxPrice = existingParams.MaxPrice,
                        MinYear = existingParams.MinYear,
                        MaxYear = existingParams.MaxYear,
                        MaxMileage = existingParams.MaxMileage,
                        PreferredMakes = existingParams.PreferredMakes.ToList(),
                        PreferredFuelTypes = existingParams.PreferredFuelTypes.ToList(),
                        PreferredVehicleTypes = existingParams.PreferredVehicleTypes.ToList(),
                        DesiredFeatures = existingParams.DesiredFeatures.ToList(),
                        Transmission = existingParams.Transmission,
                        MinEngineSize = existingParams.MinEngineSize,
                        MaxEngineSize = existingParams.MaxEngineSize,
                        MinHorsePower = existingParams.MinHorsePower,
                        MaxHorsePower = existingParams.MaxHorsePower,
                        TextPrompt = newParams.TextPrompt, // Use the new text prompt
                        MaxResults = existingParams.MaxResults,
                        RetrieverSuggestion = newParams.RetrieverSuggestion,
                        ModelUsed = newParams.ModelUsed,
                        Intent = newParams.Intent,
                        ClarificationNeededFor = newParams.ClarificationNeededFor,
                    }
                    : newParams; // For other intents, start with new params

            // For refine or new queries, selectively overwrite only the fields that are present in newParams
            if (userIntent?.Equals("refine_criteria", StringComparison.OrdinalIgnoreCase) == true ||
                userIntent?.Equals("new_query", StringComparison.OrdinalIgnoreCase) == true)
            {
                // Only overwrite the price fields if they were explicitly set in the new params
                if (newParams.MinPrice.HasValue)
                {
                    mergedParams.MinPrice = newParams.MinPrice;
                }

                if (newParams.MaxPrice.HasValue)
                {
                    mergedParams.MaxPrice = newParams.MaxPrice;
                }

                // Only overwrite the year fields if they were explicitly set in the new params
                if (newParams.MinYear.HasValue)
                {
                    mergedParams.MinYear = newParams.MinYear;
                }

                if (newParams.MaxYear.HasValue)
                {
                    mergedParams.MaxYear = newParams.MaxYear;
                }

                // Only overwrite the mileage field if it was explicitly set in the new params
                if (newParams.MaxMileage.HasValue)
                {
                    mergedParams.MaxMileage = newParams.MaxMileage;
                }

                // For list parameters, only overwrite if non-empty in new params
                if (newParams.PreferredMakes.Any() == true)
                {
                    mergedParams.PreferredMakes = newParams.PreferredMakes;
                }

                if (newParams.PreferredFuelTypes.Any() == true)
                {
                    mergedParams.PreferredFuelTypes = newParams.PreferredFuelTypes;
                }

                if (newParams.PreferredVehicleTypes.Any() == true)
                {
                    mergedParams.PreferredVehicleTypes = newParams.PreferredVehicleTypes;
                }

                // Fix for null issue in the DesiredFeatures section
                if ((newParams.DesiredFeatures?.Any() == true) || (existingParams.DesiredFeatures?.Any() == true))
                {
                    // Use null coalescing operators to ensure we never work with null collections
                    List<string> newFeatures = newParams.DesiredFeatures ?? new List<string>();
                    List<string> existingFeatures = existingParams.DesiredFeatures ?? new List<string>();
                    
                    mergedParams.DesiredFeatures = newFeatures.Union(existingFeatures).ToList();
                }
                else
                {
                    mergedParams.DesiredFeatures = new List<string>();
                }

                // NEW: Transmission
                if (newParams.Transmission.HasValue)
                {
                    // Only apply if it doesn't match a rejected transmission
                    if (context == null || newParams.Transmission != context.RejectedTransmission)
                    {
                        mergedParams.Transmission = newParams.Transmission;
                    }
                }

                // NEW: Engine size range
                if (newParams.MinEngineSize.HasValue)
                {
                    mergedParams.MinEngineSize = newParams.MinEngineSize;
                }

                if (newParams.MaxEngineSize.HasValue)
                {
                    mergedParams.MaxEngineSize = newParams.MaxEngineSize;
                }

                // NEW: Horsepower range
                if (newParams.MinHorsePower.HasValue)
                {
                    mergedParams.MinHorsePower = newParams.MinHorsePower;
                }

                if (newParams.MaxHorsePower.HasValue)
                {
                    mergedParams.MaxHorsePower = newParams.MaxHorsePower;
                }
            }
            else if (userIntent?.Equals("add_criteria", StringComparison.OrdinalIgnoreCase) == true)
            {
                // Handle additive intent - specifically union lists rather than replace

                // For makes
                if (newParams.PreferredMakes.Any() == true && existingParams.PreferredMakes.Any() == true)
                {
                    mergedParams.PreferredMakes = newParams.PreferredMakes
                        .Union(existingParams.PreferredMakes)
                        .ToList();
                }

                // For fuel types
                if (newParams.PreferredFuelTypes.Any() == true && existingParams.PreferredFuelTypes.Any() == true)
                {
                    mergedParams.PreferredFuelTypes = newParams.PreferredFuelTypes
                        .Union(existingParams.PreferredFuelTypes)
                        .ToList();
                }

                // For vehicle types
                if (newParams.PreferredVehicleTypes.Any() == true &&
                    existingParams.PreferredVehicleTypes.Any() == true)
                {
                    mergedParams.PreferredVehicleTypes = newParams.PreferredVehicleTypes
                        .Union(existingParams.PreferredVehicleTypes)
                        .ToList();
                }

                // Features are always additive
                if (newParams.DesiredFeatures.Any() == true || existingParams.DesiredFeatures.Any() == true)
                {
                    mergedParams.DesiredFeatures = (newParams.DesiredFeatures)
                        .Union(existingParams.DesiredFeatures)
                        .ToList();
                }
                else
                {
                    mergedParams.DesiredFeatures = new List<string>();
                }

                // For transmission, engine size, and horsepower, we use the latest value rather than merging
                // since these are not list types that can be unioned
                if (newParams.Transmission.HasValue)
                {
                    mergedParams.Transmission = newParams.Transmission;
                }

                if (newParams.MinEngineSize.HasValue)
                {
                    mergedParams.MinEngineSize = newParams.MinEngineSize;
                }

                if (newParams.MaxEngineSize.HasValue)
                {
                    mergedParams.MaxEngineSize = newParams.MaxEngineSize;
                }

                if (newParams.MinHorsePower.HasValue)
                {
                    mergedParams.MinHorsePower = newParams.MinHorsePower;
                }

                if (newParams.MaxHorsePower.HasValue)
                {
                    mergedParams.MaxHorsePower = newParams.MaxHorsePower;
                }
            }

            // For replace_criteria (default), we've already started with newParams so no special handling
            return mergedParams;
        }

        // This method determines if we need clarification based on the extracted parameters
        private static bool NeedsClarification(RecommendationParameters parameters, string message, ConversationContext context)
        {
            // If Python explicitly determined a "clarify" intent, respect it
            if (parameters.Intent?.Equals("clarify", StringComparison.OrdinalIgnoreCase) == true)
            {
                return true;
            }
            
            // Check structured context fields first
            bool hasVehicleType = context.ConfirmedVehicleTypes.Any();
            bool hasPrice = context.ConfirmedMinPrice.HasValue || context.ConfirmedMaxPrice.HasValue;
            bool hasMakes = context.ConfirmedMakes.Any();
            
            // If we have a viable combination, we don't need clarification
            if (hasVehicleType && (hasPrice || hasMakes))
            {
                return false;
            }
            
            // Count missing parameter types
            double missingParameterTypes = 0;
            
            if (!hasPrice) missingParameterTypes++;
            if (!hasVehicleType) missingParameterTypes++;
            if (!hasMakes) missingParameterTypes++;
            if (!context.ConfirmedMinYear.HasValue && !context.ConfirmedMaxYear.HasValue) missingParameterTypes++;
            if (!context.ConfirmedMaxMileage.HasValue) missingParameterTypes++;
            if (!context.ConfirmedFuelTypes.Any()) missingParameterTypes++;
            
            // NEW: Count transmission, engine size, and horsepower as optional parameters
            // that don't necessarily trigger clarification if they're the only ones missing
            if (!context.ConfirmedTransmission.HasValue) missingParameterTypes += 0.5;
            if (!context.ConfirmedMinEngineSize.HasValue && !context.ConfirmedMaxEngineSize.HasValue) missingParameterTypes += 0.5;
            if (!context.ConfirmedMinHorsePower.HasValue && !context.ConfirmedMaxHorsePower.HasValue) missingParameterTypes += 0.5;
            return missingParameterTypes >= 3;
        }

        // Generate a personalized clarification message based on context and specific clarification needs
        private static string GenerateClarificationMessage(
            RecommendationParameters parameters,
            string message,
            ConversationContext context)
        {
            StringBuilder clarification = new();

            // Shorter, more direct introduction
            _ = context.MessageCount > 1
                ? clarification.Append("To refine your search, ")
                : clarification.Append("To find your ideal vehicle, ");

            // List to collect questions in priority order
            List<string> questions = new();

            // PRIORITIZED QUESTION LOGIC
            bool hasVehicleType = parameters.PreferredVehicleTypes.Any() == true;
            bool hasPrice = parameters.MinPrice.HasValue || parameters.MaxPrice.HasValue;
            bool hasMakes = parameters.PreferredMakes.Any() == true;

            // Priority 1: VehicleType
            if (!hasVehicleType)
            {
                string vehicleTypeQuestion = context.TopicContext.ContainsKey("discussing_family_needs")
                    ? "What type of vehicle would work best (e.g., SUV, Minivan, Sedan)?"
                    : "What type of vehicle are you interested in (e.g., Sedan, SUV, Hatchback)?";
                questions.Add(vehicleTypeQuestion);
            }

            // Priority 2: Price or Make (if VehicleType is present)
            if (hasVehicleType)
            {
                if (!hasPrice)
                {
                    string priceQuestion = context.TopicContext.ContainsKey("discussing_budget")
                        ? "What specific price range are you comfortable with?"
                        : "What's your budget for this vehicle?";
                    questions.Add(priceQuestion);
                }
                else if (!hasMakes)
                {
                    string makeQuestion = context.ExplicitlyRejectedOptions.Any()
                        ? $"Besides {string.Join(", ", context.ExplicitlyRejectedOptions)} that you don't want, any preferred makes (e.g., Toyota, Ford, BMW)?"
                        : "Any preferred makes (e.g., Toyota, Ford, BMW)?";
                    questions.Add(makeQuestion);
                }
            }

            // Priority 3: If multiple key elements are missing or we've reached here without questions
            if ((!hasVehicleType && !hasPrice) || (!hasVehicleType && !hasMakes) || questions.Count == 0)
            {
                // Year
                if (parameters.MinYear == null && parameters.MaxYear == null)
                {
                    questions.Add("How new should the vehicle be (e.g., 2018+, newer than 5 years)?");
                }

                // Fuel Type
                if (parameters.PreferredFuelTypes.Count == 0)
                {
                    questions.Add("Which fuel type do you prefer (e.g., Petrol, Diesel, Electric, Hybrid)?");
                }

                // Make (if not already added)
                if (!hasMakes && !questions.Any(q => q.Contains("preferred makes")))
                {
                    string makeQuestion = context.ExplicitlyRejectedOptions.Any()
                        ? $"Besides {string.Join(", ", context.ExplicitlyRejectedOptions)} that you don't want, any preferred makes (e.g., Toyota, Ford, BMW)?"
                        : "Any preferred makes (e.g., Toyota, Ford, BMW)?";
                    questions.Add(makeQuestion);
                }

                // Price (if not already added)
                if (!hasPrice && !questions.Any(q => q.Contains("price") || q.Contains("budget")))
                {
                    string priceQuestion = context.TopicContext.ContainsKey("discussing_budget")
                        ? "What specific price range are you comfortable with?"
                        : "What's your budget for this vehicle?";
                    questions.Add(priceQuestion);
                }

                // NEW: Transmission - only ask if we have some vehicle preferences established
                bool hasVehiclePreference = parameters.PreferredVehicleTypes.Any() == true || 
                                           parameters.PreferredMakes.Any() == true;
                                           
                if (parameters.Transmission == null && hasVehiclePreference && 
                    context.RejectedTransmission == null && questions.Count < 2)
                {
                    questions.Add("Do you prefer automatic or manual transmission?");
                }
                
                // NEW: Engine size - only ask if we have some performance or vehicle type context
                bool mightCareAboutEngineSize = (parameters.PreferredVehicleTypes.Any() == true && 
                                               parameters.PreferredVehicleTypes.Any(t => 
                                                   t == VehicleType.SUV || t == VehicleType.Pickup)) ||
                                               context.TopicContext.ContainsKey("discussing_performance");
                                               
                if (!parameters.MinEngineSize.HasValue && !parameters.MaxEngineSize.HasValue && 
                    mightCareAboutEngineSize && questions.Count < 2)
                {
                    questions.Add("Any preferences about engine size (e.g., 2.0L or smaller, larger than 3.0L)?");
                }
                
                // NEW: Horsepower - only ask if we have sports car or performance context
                bool mightCareAboutHorsepower = context.TopicContext.ContainsKey("discussing_performance") || 
                                              (parameters.PreferredVehicleTypes.Any() == true && 
                                               parameters.PreferredVehicleTypes.Contains(VehicleType.Coupe));
                                               
                if (!parameters.MinHorsePower.HasValue && !parameters.MaxHorsePower.HasValue && 
                    mightCareAboutHorsepower && questions.Count < 2)
                {
                    questions.Add("Any minimum horsepower requirement for the vehicle?");
                }
            }

            // Use clarificationNeededFor if provided and we haven't built our own questions
            if (questions.Count == 0 && parameters.ClarificationNeededFor.Any() == true)
            {
                foreach (string reason in parameters.ClarificationNeededFor)
                {
                    switch (reason.ToLowerInvariant())
                    {
                        case "price":
                        case "budget":
                            questions.Add("What's your budget for this vehicle?");
                            break;
                        case "vehicle_type":
                        case "type":
                            questions.Add("What type of vehicle are you interested in (e.g., Sedan, SUV, Hatchback)?");
                            break;
                        case "make":
                        case "manufacturer":
                        case "brand":
                            questions.Add("Any preferred makes (e.g., Toyota, Ford, BMW)?");
                            break;
                        case "year":
                            questions.Add("How new should the vehicle be (e.g., 2018+, newer than 5 years)?");
                            break;
                        case "fuel_type":
                        case "fuel":
                            questions.Add("Which fuel type do you prefer (e.g., Petrol, Diesel, Electric, Hybrid)?");
                            break;
                        case "category":
                            if (!string.IsNullOrEmpty(parameters.RetrieverSuggestion))
                            {
                                return parameters.RetrieverSuggestion;
                            }

                            questions.Add("Could you specify what type of vehicle you're looking for?");
                            break;
                        case "ambiguous":
                            questions.Add("Could you provide more specific details about what you're looking for?");
                            break;
                        case "transmission":
                            questions.Add("Do you prefer automatic or manual transmission?");
                            break;
                        case "engine_size":
                        case "enginesize":
                            questions.Add("Any preferences regarding engine size (e.g., 2.0L, 3.0L)?");
                            break;
                        case "horsepower":
                        case "power":
                            questions.Add("Are you looking for a specific level of horsepower or performance?");
                            break;
                        default:
                            break;
                    }
                }
            }

            // Final fallback if no questions were generated
            if (questions.Count == 0)
            {
                questions.Add("Could you provide more details about what you're looking for?");
            }

            // Add main text requesting information
            _ = clarification.Append("I need to know: ");

            // Format questions appropriately
            if (questions.Count == 1)
            {
                // Single question - just append it directly
                _ = clarification.Append(questions[0]);
            }
            else
            {
                // Multiple questions - format as a numbered list
                _ = clarification.AppendLine();
                for (int i = 0; i < questions.Count; i++)
                {
                    _ = clarification.AppendLine($"{i + 1}. {questions[i]}");
                }
            }

            // Before returning, store the generated question in context
            string clarificationMessage = clarification.ToString().Trim();
            context.LastQuestionAskedByAI = clarificationMessage;

            return clarificationMessage;
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
                // No matching vehicles found - create response explaining why
                _ = response.Append("Unfortunately, I couldn't find any vehicles matching all your criteria. ");
                
                // Rest of the "no results" code...
            }
            else
            {
                // Vehicles found - create response with appropriate intro based on conversation context
                if (context.MessageCount > 1)
                    _ = response.Append("Based on our conversation, ");
                else
                    _ = response.Append("Great! ");

                // Rest of the "vehicles found" code...
            }

            return response.ToString();
        }

        // Extract parameters from message using the Python parameter extraction service
        private async Task<RecommendationParameters> ExtractParametersAsync(
            string message,
            string? modelStrategy = null,
            List<ConversationTurn>? recentHistory = null,
            ConversationContext context = null)
        {
            try
            {
                // Get the parameter extraction endpoint from configuration
                string endpoint = this.configuration["Services:ParameterExtraction:Endpoint"] ??
                                  "http://localhost:5006/extract_parameters";
                int timeoutSeconds = int.TryParse(
                    this.configuration["Services:ParameterExtraction:Timeout"],
                    out int timeout)
                    ? timeout
                    : 30;

                this.logger.LogInformation(
                    "Calling parameter extraction service at {Endpoint} with model strategy: {ModelStrategy}", endpoint,
                    modelStrategy);

                // Format the conversation history
                List<object> formattedHistory = [];
                if (recentHistory != null)
                {
                    foreach (ConversationTurn turn in recentHistory)
                    {
                        formattedHistory.Add(new
                        {
                            user = turn.UserMessage,
                            ai = turn.AIResponse,
                        });
                    }
                }

                // Enhanced request payload with structured context
                var requestPayload = new
                {
                    query = message,
                    forceModel = modelStrategy,
                    conversationHistory = formattedHistory,
                    lastQuestionAskedByAI = context.LastQuestionAskedByAI,
                    confirmedContext = context != null ? new
                    {
                        confirmedMakes = context.ConfirmedMakes,
                        confirmedVehicleTypes = context.ConfirmedVehicleTypes.Select(t => t.ToString()),
                        confirmedFuelTypes = context.ConfirmedFuelTypes.Select(f => f.ToString()),
                        confirmedMinPrice = context.ConfirmedMinPrice,
                        confirmedMaxPrice = context.ConfirmedMaxPrice,
                        confirmedMinYear = context.ConfirmedMinYear,
                        confirmedMaxYear = context.ConfirmedMaxYear,
                        confirmedMaxMileage = context.ConfirmedMaxMileage,
                        confirmedTransmission = context.ConfirmedTransmission?.ToString(),
                        confirmedMinEngineSize = context.ConfirmedMinEngineSize,
                        confirmedMaxEngineSize = context.ConfirmedMaxEngineSize,
                        confirmedMinHorsePower = context.ConfirmedMinHorsePower,
                        confirmedMaxHorsePower = context.ConfirmedMaxHorsePower
                    } : null,
                    rejectedContext = context != null ? new
                    {
                        rejectedMakes = context.RejectedMakes,
                        rejectedVehicleTypes = context.RejectedVehicleTypes.Select(t => t.ToString()),
                        rejectedFuelTypes = context.RejectedFuelTypes.Select(f => f.ToString()),
                        rejectedTransmission = context.RejectedTransmission?.ToString()
                    } : null
                };

                this.logger.LogInformation(
                    "Sending request to parameter extraction with {HistoryCount} history items",
                    formattedHistory.Count);

                StringContent content = new(
                    JsonSerializer.Serialize(requestPayload),
                    Encoding.UTF8,
                    "application/json");

                // Configure timeout
                CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(timeoutSeconds));

                this.logger.LogInformation(
                    "SENDING REQUEST to {Endpoint} with payload: {Payload}",
                    endpoint, JsonSerializer.Serialize(requestPayload));
                HttpResponseMessage response = await this.httpClient.PostAsync(endpoint, content, timeoutCts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    this.logger.LogError(
                        "Parameter extraction service error: {StatusCode}, {ErrorContent}",
                        response.StatusCode,
                        errorContent);

                    return new RecommendationParameters
                    {
                        TextPrompt = message,
                        MaxResults = 10, // CHANGED FROM 5 TO 10
                        Intent = "new_query",
                    };
                }

                // Parse the response
                string responseContent = await response.Content.ReadAsStringAsync();
                this.logger.LogDebug("Parameter extraction service response: {Response}", responseContent);

                using JsonDocument jsonDoc = JsonDocument.Parse(responseContent);
                RecommendationParameters parameters = new()
                {
                    TextPrompt = message,
                    MaxResults = 10,

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
                            ? makesElement.EnumerateArray()
                                .Where(e => e.ValueKind == JsonValueKind.String)
                                .Select(e => e.GetString())
                                .Where(s => s != null)
                                .Select(s => s!)
                                .ToList()
                            : new List<string>(),

                    DesiredFeatures =
                        jsonDoc.RootElement.TryGetProperty("desiredFeatures", out JsonElement featuresElement) &&
                        featuresElement.ValueKind == JsonValueKind.Array
                            ? featuresElement.EnumerateArray()
                                .Where(e => e.ValueKind == JsonValueKind.String)
                                .Select(e => e.GetString())
                                .Where(s => s != null)
                                .Select(s => s!)
                                .ToList()
                            : new List<string>(),

                    // Parse enums correctly
                    PreferredFuelTypes =
                        jsonDoc.RootElement.TryGetProperty("preferredFuelTypes", out JsonElement fuelTypesElement) &&
                        fuelTypesElement.ValueKind == JsonValueKind.Array
                            ? fuelTypesElement.EnumerateArray()
                                .Where(e => e.ValueKind == JsonValueKind.String)
                                .Select(e =>
                                    EnumHelpers.TryParseFuelType(
                                        e.GetString() ?? string.Empty,
                                        out FuelType fuel)
                                        ? fuel
                                        : (FuelType?)null)
                                .Where(f => f.HasValue)
                                .Select(f => f!.Value)
                                .ToList()
                            : new List<FuelType>(),

                    PreferredVehicleTypes =
                        jsonDoc.RootElement.TryGetProperty(
                            "preferredVehicleTypes",
                            out JsonElement vehicleTypesElement) &&
                        vehicleTypesElement.ValueKind == JsonValueKind.Array
                            ? vehicleTypesElement.EnumerateArray()
                                .Where(e => e.ValueKind == JsonValueKind.String)
                                .Select(e =>
                                    EnumHelpers.TryParseVehicleType(
                                        e.GetString() ?? string.Empty,
                                        out VehicleType vehicle)
                                        ? vehicle
                                        : (VehicleType?)null)
                                .Where(v => v.HasValue)
                                .Select(v => v!.Value)
                                .ToList()
                            : new List<VehicleType>(),

                    // Parse the off-topic flags
                    IsOffTopic = jsonDoc.RootElement.TryGetProperty("isOffTopic", out JsonElement isOffTopicElement)
                                 && isOffTopicElement.ValueKind == JsonValueKind.True,

                    // NEW: Parse transmission type
                    Transmission = jsonDoc.RootElement.TryGetProperty("transmission", out JsonElement transmissionElement) &&
                                 transmissionElement.ValueKind == JsonValueKind.String
                        ? EnumHelpers.TryParseTransmissionType(
                            transmissionElement.GetString() ?? string.Empty,
                            out TransmissionType transmission)
                            ? transmission
                            : null
                        : null,
                        
                    // NEW: Parse engine size range
                    MinEngineSize = jsonDoc.RootElement.TryGetProperty("minEngineSize", out JsonElement minEngineSizeElement) &&
                                  minEngineSizeElement.ValueKind == JsonValueKind.Number
                        ? minEngineSizeElement.GetDouble()
                        : null,
                        
                    MaxEngineSize = jsonDoc.RootElement.TryGetProperty("maxEngineSize", out JsonElement maxEngineSizeElement) &&
                                  maxEngineSizeElement.ValueKind == JsonValueKind.Number
                        ? maxEngineSizeElement.GetDouble()
                        : null,
                        
                    // NEW: Parse horsepower range
                    MinHorsePower = jsonDoc.RootElement.TryGetProperty("minHorsepower", out JsonElement minHorsepowerElement) &&
                                  minHorsepowerElement.ValueKind == JsonValueKind.Number
                        ? (int?)Convert.ToInt32(minHorsepowerElement.GetDouble())
                        : null,
                        
                    MaxHorsePower = jsonDoc.RootElement.TryGetProperty("maxHorsepower", out JsonElement maxHorsepowerElement) &&
                                  maxHorsepowerElement.ValueKind == JsonValueKind.Number
                        ? (int?)Convert.ToInt32(maxHorsepowerElement.GetDouble())
                        : null,
                };

                if (jsonDoc.RootElement.TryGetProperty("explicitly_negated_makes", out JsonElement negatedMakesElement) &&
                    negatedMakesElement.ValueKind == JsonValueKind.Array)
                {
                    parameters.ExplicitlyNegatedMakes = negatedMakesElement.EnumerateArray()
                        .Where(e => e.ValueKind == JsonValueKind.String)
                        .Select(e => e.GetString())
                        .Where(s => s != null)
                        .Select(s => s!)
                        .ToList();
                }

                if (jsonDoc.RootElement.TryGetProperty("explicitly_negated_vehicle_types", out JsonElement negatedVehicleTypesElement) &&
                    negatedVehicleTypesElement.ValueKind == JsonValueKind.Array)
                {
                    parameters.ExplicitlyNegatedVehicleTypes = negatedVehicleTypesElement.EnumerateArray()
                        .Where(e => e.ValueKind == JsonValueKind.String)
                        .Select(e =>
                            EnumHelpers.TryParseVehicleType(
                                e.GetString() ?? string.Empty,
                                out VehicleType vehicle)
                                ? vehicle
                                : (VehicleType?)null)
                        .Where(v => v.HasValue)
                        .Select(v => v!.Value)
                        .ToList();
                }

                if (jsonDoc.RootElement.TryGetProperty("explicitly_negated_fuel_types", out JsonElement negatedFuelTypesElement) &&
                    negatedFuelTypesElement.ValueKind == JsonValueKind.Array)
                {
                    parameters.ExplicitlyNegatedFuelTypes = negatedFuelTypesElement.EnumerateArray()
                        .Where(e => e.ValueKind == JsonValueKind.String)
                        .Select(e =>
                            EnumHelpers.TryParseFuelType(
                                e.GetString() ?? string.Empty,
                                out FuelType fuel)
                                ? fuel
                                : (FuelType?)null)
                        .Where(f => f.HasValue)
                        .Select(f => f!.Value)
                        .ToList();
                }

                if (parameters.IsOffTopic && jsonDoc.RootElement.TryGetProperty(
                    "offTopicResponse",
                    out JsonElement responseElement)
                                          && responseElement.ValueKind == JsonValueKind.String)
                {
                    parameters.OffTopicResponse = responseElement.GetString();
                }

                // Parse retriever suggestion
                if (jsonDoc.RootElement.TryGetProperty(
                    "retrieverSuggestion",
                    out JsonElement retrieverSuggestionElement) &&
                    retrieverSuggestionElement.ValueKind == JsonValueKind.String)
                {
                    parameters.RetrieverSuggestion = retrieverSuggestionElement.GetString();
                }

                // NEW: Parse user intent
                if (jsonDoc.RootElement.TryGetProperty("intent", out JsonElement intentElement) &&
                    intentElement.ValueKind == JsonValueKind.String)
                {
                    parameters.Intent = intentElement.GetString();
                }
                else
                {
                    parameters.Intent = "new_query"; // Default intent
                }

                // NEW: Parse clarificationNeededFor
                if (jsonDoc.RootElement.TryGetProperty(
                    "clarificationNeededFor",
                    out JsonElement clarificationNeededForElement) &&
                    clarificationNeededForElement.ValueKind == JsonValueKind.Array)
                {
                    parameters.ClarificationNeededFor = clarificationNeededForElement.EnumerateArray()
                        .Where(e => e.ValueKind == JsonValueKind.String)
                        .Select(e => e.GetString())
                        .Where(s => s != null)
                        .Select(s => s!)
                        .ToList();
                }

                this.logger.LogInformation(
                    "Final extracted parameters: {Params}",
                    JsonSerializer.Serialize(parameters));
                return parameters;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error extracting parameters from message");

                return new RecommendationParameters
                {
                    TextPrompt = message,
                    MaxResults = 10, // CHANGED FROM 5 TO 10
                    Intent = "new_query",
                };
            }
        }

        private async Task UpdateStructuredContextFromParametersAsync(
            RecommendationParameters parameters,
            ConversationContext context,
            string userMessage)
        {
            // Update confirmed parameters with new values from extracted parameters
            if (parameters.MinPrice.HasValue)
                context.ConfirmedMinPrice = parameters.MinPrice;

            if (parameters.MaxPrice.HasValue)
                context.ConfirmedMaxPrice = parameters.MaxPrice;

            if (parameters.MinYear.HasValue)
                context.ConfirmedMinYear = parameters.MinYear;

            if (parameters.MaxYear.HasValue)
                context.ConfirmedMaxYear = parameters.MaxYear;

            if (parameters.MaxMileage.HasValue)
                context.ConfirmedMaxMileage = parameters.MaxMileage;

            // Detect explicit negatives and positives for makes/models/types
            string lowerMessage = userMessage.ToLower(CultureInfo.CurrentCulture);

            // Handle makes (e.g., "I want a BMW" vs "No BMWs")
            if (parameters.PreferredMakes?.Any() == true)
            {
                foreach (var make in parameters.PreferredMakes)
                {
                    // If make was previously rejected but now explicitly requested, remove from rejected
                    if (context.RejectedMakes.Contains(make))
                        context.RejectedMakes.Remove(make);

                    // Add to confirmed makes if not already there
                    if (!context.ConfirmedMakes.Contains(make))
                        context.ConfirmedMakes.Add(make);
                }
            }

            // Similar logic for vehicle types
            if (parameters.PreferredVehicleTypes?.Any() == true)
            {
                foreach (var type in parameters.PreferredVehicleTypes)
                {
                    if (context.RejectedVehicleTypes.Contains(type))
                        context.RejectedVehicleTypes.Remove(type);

                    if (!context.ConfirmedVehicleTypes.Contains(type))
                        context.ConfirmedVehicleTypes.Add(type);
                }
            }

            // Similar logic for fuel types
            if (parameters.PreferredFuelTypes?.Any() == true)
            {
                foreach (var fuel in parameters.PreferredFuelTypes)
                {
                    if (context.RejectedFuelTypes.Contains(fuel))
                        context.RejectedFuelTypes.Remove(fuel);

                    if (!context.ConfirmedFuelTypes.Contains(fuel))
                        context.ConfirmedFuelTypes.Add(fuel);
                }
            }

            // Add features
            if (parameters.DesiredFeatures.Any() == true)
            {
                foreach (var feature in parameters.DesiredFeatures)
                {
                    if (context.RejectedFeatures.Contains(feature))
                        context.RejectedFeatures.Remove(feature);

                    if (!context.ConfirmedFeatures.Contains(feature))
                        context.ConfirmedFeatures.Add(feature);
                }
            }

            // NEW: Update transmission preference
            if (parameters.Transmission.HasValue)
            {
                // If a transmission type was previously rejected but now explicitly requested, remove from rejected
                if (context.RejectedTransmission == parameters.Transmission.Value)
                    context.RejectedTransmission = null;
                    
                context.ConfirmedTransmission = parameters.Transmission.Value;
            }
            
            // NEW: Update engine size range
            if (parameters.MinEngineSize.HasValue)
                context.ConfirmedMinEngineSize = parameters.MinEngineSize;
                
            if (parameters.MaxEngineSize.HasValue)
                context.ConfirmedMaxEngineSize = parameters.MaxEngineSize;
                
            // NEW: Update horsepower range
            if (parameters.MinHorsePower.HasValue)
                context.ConfirmedMinHorsePower = parameters.MinHorsePower;
                
            if (parameters.MaxHorsePower.HasValue)
                context.ConfirmedMaxHorsePower = parameters.MaxHorsePower;

            // Process negative statements to populate rejected collections
            ProcessNegativePreferences(lowerMessage, context, parameters.Intent);

            // Update the current parameters in context to reflect confirmed/rejected preferences
            SynchronizeCurrentParametersWithConfirmedValues(context);
        }

        private void ProcessNegativePreferences(string lowerMessage, ConversationContext context, string? intent = null)
        {
            // Be more careful with negation detection in clarification responses
            bool isClarification = intent?.Equals("clarify", StringComparison.OrdinalIgnoreCase) == true;

            // Only process negations if this isn't a direct clarification response
            // or if the message explicitly contains negation markers
            bool shouldProcessNegations = !isClarification ||
                                          (lowerMessage.Contains("not ") ||
                                           lowerMessage.Contains("don't want") ||
                                           lowerMessage.Contains("no "));

            if (shouldProcessNegations &&
                (lowerMessage.Contains("not ") || lowerMessage.Contains("don't want") ||
                 lowerMessage.Contains("no ") || lowerMessage.Contains("except") ||
                 lowerMessage.Contains("avoid")))
            {
                // === PROCESS NEGATED MAKES ===
                string[] commonMakes = ["bmw", "audi", "ford", "toyota", "honda", "volkswagen",
                                      "mercedes", "volvo", "nissan", "hyundai", "kia", "mazda"];

                foreach (string make in commonMakes)
                {
                    if ((lowerMessage.Contains($"not {make}") || lowerMessage.Contains($"no {make}") ||
                        lowerMessage.Contains($"don't want {make}") || lowerMessage.Contains($"except {make}")))
                    {
                        // Only add to rejected if not already confirmed
                        if (!context.ConfirmedMakes.Contains(make, StringComparer.OrdinalIgnoreCase) &&
                            !context.RejectedMakes.Contains(make, StringComparer.OrdinalIgnoreCase))
                        {
                            context.RejectedMakes.Add(make);
                        }
                    }
                }

                // === PROCESS NEGATED MODELS ===
                string[] commonModels = ["camry", "civic", "f-150", "golf", "3 series", "model 3", "rav4", "mustang"];

                foreach (string model in commonModels)
                {
                    if ((lowerMessage.Contains($"not {model}") || lowerMessage.Contains($"no {model}") ||
                        lowerMessage.Contains($"don't want {model}") || lowerMessage.Contains($"except {model}")))
                    {
                        if (!context.ConfirmedModels.Contains(model, StringComparer.OrdinalIgnoreCase) &&
                            !context.RejectedModels.Contains(model, StringComparer.OrdinalIgnoreCase))
                        {
                            context.RejectedModels.Add(model);
                        }
                    }
                }

                // === PROCESS NEGATED VEHICLE TYPES ===
                Dictionary<string, VehicleType> vehicleTypeMap = new()
                {
                    {"suv", VehicleType.SUV},
                    {"sedan", VehicleType.Sedan},
                    {"hatchback", VehicleType.Hatchback},
                    {"convertible", VehicleType.Convertible},
                    {"pickup", VehicleType.Pickup},
                    {"coupe", VehicleType.Coupe},
                    {"van", VehicleType.Van},
                    {"estate", VehicleType.Estate},
                    {"wagon", VehicleType.Estate}
                };

                foreach (var pair in vehicleTypeMap)
                {
                    if (lowerMessage.Contains($"not {pair.Key}") || lowerMessage.Contains($"no {pair.Key}") ||
                        lowerMessage.Contains($"don't want {pair.Key}") || lowerMessage.Contains($"except {pair.Key}"))
                    {
                        if (!context.ConfirmedVehicleTypes.Contains(pair.Value) &&
                            !context.RejectedVehicleTypes.Contains(pair.Value))
                        {
                            context.RejectedVehicleTypes.Add(pair.Value);
                        }
                    }
                }

                // === PROCESS NEGATED FUEL TYPES ===
                Dictionary<string, FuelType> fuelTypeMap = new()
                {
                    {"petrol", FuelType.Petrol},
                    {"gas", FuelType.Petrol},
                    {"gasoline", FuelType.Petrol},
                    {"diesel", FuelType.Diesel},
                    {"electric", FuelType.Electric},
                    {"ev", FuelType.Electric},
                    {"hybrid", FuelType.Hybrid},
                    {"plugin", FuelType.Plugin},
                    {"plug-in", FuelType.Plugin}
                };

                foreach (var pair in fuelTypeMap)
                {
                    if (lowerMessage.Contains($"not {pair.Key}") || lowerMessage.Contains($"no {pair.Key}") ||
                        lowerMessage.Contains($"don't want {pair.Key}") || lowerMessage.Contains($"except {pair.Key}"))
                    {
                        if (!context.ConfirmedFuelTypes.Contains(pair.Value) &&
                            !context.RejectedFuelTypes.Contains(pair.Value))
                        {
                            context.RejectedFuelTypes.Add(pair.Value);
                        }
                    }
                }

                // === PROCESS NEGATED TRANSMISSION === (Update with more comprehensive detection)
                if (lowerMessage.Contains("not automatic") || lowerMessage.Contains("no automatic") ||
                    lowerMessage.Contains("don't want automatic") || lowerMessage.Contains("hate automatic") ||
                    lowerMessage.Contains("dislike automatic"))
                {
                    if (context.ConfirmedTransmission != TransmissionType.Automatic)
                        context.RejectedTransmission = TransmissionType.Automatic;
                }
                else if (lowerMessage.Contains("not manual") || lowerMessage.Contains("no manual") ||
                         lowerMessage.Contains("don't want manual") || lowerMessage.Contains("hate manual") ||
                         lowerMessage.Contains("dislike manual") || lowerMessage.Contains("not stick shift"))
                {
                    if (context.ConfirmedTransmission != TransmissionType.Manual)
                        context.RejectedTransmission = TransmissionType.Manual;
                }

                // === PROCESS NEGATED FEATURES ===
                string[] featureKeywords = [
                    "bluetooth", "navigation", "leather", "sunroof", "camera",
                    "cruise control", "parking sensors", "heated seats", "air conditioning",
                    "apple carplay", "android auto", "third row", "tow"
                ];

                foreach (string feature in featureKeywords)
                {
                    if ((lowerMessage.Contains($"not {feature}") || lowerMessage.Contains($"no {feature}") ||
                         lowerMessage.Contains($"don't need {feature}") || lowerMessage.Contains($"don't want {feature}")))
                    {
                        if (!context.ConfirmedFeatures.Contains(feature) &&
                            !context.RejectedFeatures.Contains(feature))
                        {
                            context.RejectedFeatures.Add(feature);
                        }
                    }
                }
            }

            // Update ExplicitlyRejectedOptions for backward compatibility
            foreach (var make in context.RejectedMakes)
            {
                if (!context.ExplicitlyRejectedOptions.Contains(make))
                    context.ExplicitlyRejectedOptions.Add(make);
            }
        }

        private void SynchronizeCurrentParametersWithConfirmedValues(ConversationContext context)
        {
                context.CurrentParameters = new RecommendationParameters();
                
            // Sync all numeric parameters
            context.CurrentParameters.MinPrice = context.ConfirmedMinPrice;
            context.CurrentParameters.MaxPrice = context.ConfirmedMaxPrice;
            context.CurrentParameters.MinYear = context.ConfirmedMinYear;
            context.CurrentParameters.MaxYear = context.ConfirmedMaxYear;
            context.CurrentParameters.MaxMileage = context.ConfirmedMaxMileage;
            
            // Sync makes, excluding rejected ones
            context.CurrentParameters.PreferredMakes = context.ConfirmedMakes
                .Where(m => !context.RejectedMakes.Contains(m, StringComparer.OrdinalIgnoreCase))
                .ToList();
                
            // Sync vehicle types, excluding rejected ones
            context.CurrentParameters.PreferredVehicleTypes = context.ConfirmedVehicleTypes
                .Where(t => !context.RejectedVehicleTypes.Contains(t))
                .ToList();
                
            // Sync fuel types, excluding rejected ones
            context.CurrentParameters.PreferredFuelTypes = context.ConfirmedFuelTypes
                .Where(f => !context.RejectedFuelTypes.Contains(f))
                .ToList();
                
            // Sync features, excluding rejected ones
            context.CurrentParameters.DesiredFeatures = context.ConfirmedFeatures
                .Where(f => !context.RejectedFeatures.Contains(f, StringComparer.OrdinalIgnoreCase))
                .ToList();
            
            // Sync transmission preference
            if (context.ConfirmedTransmission.HasValue && 
                context.ConfirmedTransmission != context.RejectedTransmission)
            {
                context.CurrentParameters.Transmission = context.ConfirmedTransmission;
            }
            else
            {
                context.CurrentParameters.Transmission = null;
            }
            
            // Sync engine size range
            context.CurrentParameters.MinEngineSize = context.ConfirmedMinEngineSize;
            context.CurrentParameters.MaxEngineSize = context.ConfirmedMaxEngineSize;
            
            // Sync horsepower range
            context.CurrentParameters.MinHorsePower = context.ConfirmedMinHorsePower;
            context.CurrentParameters.MaxHorsePower = context.ConfirmedMaxHorsePower;
            
            // Update MentionedVehicleFeatures for backward compatibility
            foreach (var feature in context.ConfirmedFeatures)
            {
                if (!context.MentionedVehicleFeatures.Contains(feature))
                    context.MentionedVehicleFeatures.Add(feature);
            }
        }
    }
}