namespace SmartAutoTrader.API.Services
{
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq.Expressions;
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

        // Method to determine which LLM strategy to use
        private string DetermineModelStrategy(ConversationContext context)
        {
            // Simple strategy: Rotate through models based on message count
            // More sophisticated logic could depend on intent, clarification needs, etc.
            int strategyIndex = (context.MessageCount - 1) % this.modelStrategies.Length;
            string selectedStrategy = this.modelStrategies[strategyIndex];
            this.logger.LogInformation("Determined model strategy: {Strategy} for message count {Count}", selectedStrategy, context.MessageCount);
            return selectedStrategy;
        }

        public async Task<ChatResponse> ProcessMessageAsync(int userId, ChatMessage message)
        {
            Stopwatch sw = Stopwatch.StartNew();
            this.logger.LogInformation("Processing message for user ID {UserId}: {MessageContent}", userId, message.Content);

            try
            {
                // Get or create conversation context using the correct method
                ConversationContext conversationContext = await this.contextService.GetOrCreateContextAsync(userId); // Corrected method call
                conversationContext.MessageCount++; // Increment message count

                // Determine model strategy based on context or message flags
                string modelUsedForSession = this.DetermineModelStrategy(conversationContext);

                // --- Get Current Session and ID ---
                ConversationSession? currentSession = await this.contextService.GetCurrentSessionAsync(userId);
                int? conversationSessionId = currentSession?.Id; // Get ID from the session object

                // Fetch recent history if session ID exists
                List<ConversationTurn> recentHistory = [];
                if (conversationSessionId.HasValue)
                {
                    // Use the correct GetRecentHistoryAsync overload from IChatRepository
                    recentHistory = await this.chatRepo.GetRecentHistoryAsync(userId, conversationSessionId.Value, 10); // Corrected call
                    this.logger.LogInformation(
                        "Fetched {HistoryCount} items from history for Session ID: {SessionId}",
                        recentHistory.Count, conversationSessionId.Value);
                }
                else
                {
                    // If no active session, try starting one (optional, depends on desired behavior)
                    // currentSession = await this.contextService.StartNewSessionAsync(userId);
                    // conversationSessionId = currentSession.Id;
                    // logger.LogWarning("No active session found, started new session ID: {SessionId}", conversationSessionId);
                    this.logger.LogWarning("No valid conversationSessionId available, history fetching skipped.");
                }

                // Get user context for personalization (Favorites, History) - simplified for brevity
                User? user = await this.userRepo.GetByIdAsync(userId);
                if (user == null)
                {
                    // Handle missing user - Log error and return appropriate response
                    this.logger.LogError("User with ID {UserId} not found.", userId);
                    return new ChatResponse { Message = "Sorry, I couldn't find your user profile.", ConversationId = message.ConversationId };
                }

                // Load related entities if needed (Favorites, BrowsingHistory)

                // Determine if this is a clarification or a follow-up query
                string messageToProcess = message.Content;

                // Add logic for clarification/follow-up if needed

                // --- Parameter Extraction ---
                sw.Restart();
                RecommendationParameters extractedParameters = await this.ExtractParametersAsync(
                    messageToProcess,
                    modelUsedForSession,
                    recentHistory,
                    conversationContext);
                sw.Stop();
                this.logger.LogInformation("⏱️ LLM extraction took {ElapsedMs}ms", sw.ElapsedMilliseconds);

                if (extractedParameters == null)
                {
                    this.logger.LogError("Parameter extraction returned null for user {UserId}.", userId);

                    // Handle error, maybe return a generic error response
                    return new ChatResponse { Message = "Sorry, I couldn't understand your request.", ConversationId = message.ConversationId };
                }

                // Check for CONFUSED_FALLBACK state from Python service
                if (extractedParameters.Intent?.Equals("CONFUSED_FALLBACK", StringComparison.OrdinalIgnoreCase) == true)
                {
                    this.logger.LogWarning("Python service indicated confusion. Returning fallback prompt.");
                    string fallbackMessage = extractedParameters.RetrieverSuggestion ?? "Sorry, I'm having trouble understanding. Could you please rephrase simply?";

                    // Save history with the fallback message as AI response
                    await this.SaveChatHistoryAsync(userId, message, fallbackMessage, conversationSessionId);

                    // Return a specific response for the confused state
                    return new ChatResponse
                    {
                        Message = fallbackMessage,
                        ClarificationNeeded = true, // Still requires user input
                        ConversationId = message.ConversationId,

                        // Keep RecommendedVehicles and UpdatedParameters null/empty
                    };
                }

                // Handle Off-Topic directly
                if (extractedParameters.IsOffTopic && !string.IsNullOrEmpty(extractedParameters.OffTopicResponse))
                {
                    await this.SaveChatHistoryAsync(userId, message, extractedParameters.OffTopicResponse, conversationSessionId);
                    return new ChatResponse { Message = extractedParameters.OffTopicResponse, ConversationId = message.ConversationId };
                }

                // --- Context Update ---
                // Always update the structured context based on the extracted parameters
                await this.UpdateStructuredContextFromParametersAsync(
                    extractedParameters,
                    conversationContext,
                    message.Content);

                // --- Parameter Synchronization ---
                // Always create the final set of parameters to use for searching
                this.SynchronizeCurrentParametersWithConfirmedValues(conversationContext);
                RecommendationParameters finalParametersForSearch = conversationContext.CurrentParameters;

                // Check if the Python service explicitly indicated clarification is NOT needed
                bool pythonServiceSaysClarificationNotNeeded =
                    extractedParameters.ClarificationNeededFor == null ||
                    !extractedParameters.ClarificationNeededFor.Any();

                if (pythonServiceSaysClarificationNotNeeded)
                {
                    // Log that we're bypassing clarification check based on Python's decision
                    this.logger.LogInformation("Python extraction service explicitly indicated NO clarification needed - bypassing C# clarification check");

                    // Skip to recommendation fetching directly
                    this.logger.LogInformation("Proceeding directly to recommendations using parameters: {@Parameters}", finalParametersForSearch);

                    sw.Restart();
                    IEnumerable<Vehicle> recommendations =
                        await this.recommendationService.GetRecommendationsAsync(userId, finalParametersForSearch);
                    sw.Stop();
                    this.logger.LogInformation("⏱️ Recommendation fetching took {ElapsedMs}ms", sw.ElapsedMilliseconds);

                    // Track shown vehicles
                    List<int> vehicleIds = recommendations.Select(v => v.Id).ToList();
                    foreach (int id in vehicleIds)
                    {
                        if (!conversationContext.ShownVehicleIds.Contains(id))
                        {
                            conversationContext.ShownVehicleIds.Add(id);
                        }
                    }

                    // Save the updated context
                    await this.contextService.UpdateContextAsync(userId, conversationContext);

                    // --- Response Generation ---
                    string responseMessage = GenerateResponseMessage(
                        finalParametersForSearch,
                        recommendations.Count(),
                        conversationContext);

                    // Save final AI response
                    await this.SaveChatHistoryAsync(userId, message, responseMessage, conversationSessionId);

                    return new ChatResponse
                    {
                        Message = responseMessage,
                        RecommendedVehicles = recommendations.ToList(),
                        UpdatedParameters = finalParametersForSearch,
                        ClarificationNeeded = false,
                        ConversationId = message.ConversationId,
                    };
                }
                else
                {
                    // Allow existing clarification logic to run
                    this.logger.LogInformation("Python extraction service did not explicity indicate NO clarification - proceeding with C# clarification check");

                    // --- Clarification Check ---
                    // Check if clarification is needed based on the *updated context*
                    bool needsClarification = NeedsClarification(finalParametersForSearch, message.Content, conversationContext) ||
                                              extractedParameters.Intent?.Equals("clarify", StringComparison.OrdinalIgnoreCase) == true;

                    if (needsClarification)
                    {
                        string clarificationMessage = GenerateClarificationMessage(finalParametersForSearch, message.Content, conversationContext);
                        this.logger.LogInformation("Clarification needed. Generated message: {ClarificationMessage}", clarificationMessage);

                        // Enhanced logging for loop detection
                        this.logger.LogWarning(
                            "Loop Detection Check - New Question: '{NewQuestion}', Previous Question: '{PreviousQuestion}'",
                            clarificationMessage.Substring(0, Math.Min(50, clarificationMessage.Length)),
                            conversationContext.LastQuestionAskedByAI?.Substring(0, Math.Min(50, conversationContext.LastQuestionAskedByAI?.Length ?? 0)) ?? "null");

                        // Check for a clarification loop - compare with the previous question
                        if (!string.IsNullOrEmpty(conversationContext.LastQuestionAskedByAI) &&
                            string.Equals(clarificationMessage, conversationContext.LastQuestionAskedByAI, StringComparison.Ordinal))
                        {
                            // Log detailed information about the loop
                            this.logger.LogError(
                                "CLARIFICATION LOOP DETECTED! Same question generated twice in a row.\n" +
                                "User message: '{UserMessage}'\n" +
                                "Parameters: {@Parameters}\n" +
                                "Context MessageCount: {MessageCount}",
                                message.Content,
                                finalParametersForSearch,
                                conversationContext.MessageCount);

                            // Loop detected - provide a fallback response
                            string fallbackMessage = "Sorry, I seem to be stuck. Could you try rephrasing your request clearly? Please provide specific details about the vehicle you're looking for.";
                            this.logger.LogWarning("Loop detected: Same clarification question generated consecutively. Providing fallback response.");

                            // Save the fallback response to chat history
                            await this.SaveChatHistoryAsync(userId, message, fallbackMessage, conversationSessionId);

                            // Return a fallback response
                            return new ChatResponse
                            {
                                Message = fallbackMessage,
                                ClarificationNeeded = true,
                                OriginalUserInput = message.Content,
                                ConversationId = message.ConversationId,

                                // Keep RecommendedVehicles and UpdatedParameters null/empty
                            };
                        }

                        // Normal flow (no loop detected) - proceed with clarification
                        await this.SaveChatHistoryAsync(userId, message, clarificationMessage, conversationSessionId);

                        return new ChatResponse
                        {
                            Message = clarificationMessage,
                            ClarificationNeeded = true,
                            OriginalUserInput = message.Content, // Keep original input for context
                            ConversationId = message.ConversationId,
                            UpdatedParameters = finalParametersForSearch, // Return the current state
                        };
                    }

                    // --- Recommendation Fetching ---
                    this.logger.LogInformation("Proceeding with recommendations using parameters: {@Parameters}", finalParametersForSearch);

                    // Continue with the rest of the existing recommendation logic
                    // (Note: The rest of the code matches what's in the "true" branch above, for consistency)
                    sw.Restart();
                    IEnumerable<Vehicle> recommendations =
                        await this.recommendationService.GetRecommendationsAsync(userId, finalParametersForSearch);
                    sw.Stop();
                    this.logger.LogInformation("⏱️ Recommendation fetching took {ElapsedMs}ms", sw.ElapsedMilliseconds);

                    // Track shown vehicles
                    List<int> vehicleIds = recommendations.Select(v => v.Id).ToList();
                    foreach (int id in vehicleIds)
                    {
                        if (!conversationContext.ShownVehicleIds.Contains(id))
                        {
                            conversationContext.ShownVehicleIds.Add(id);
                        }
                    }

                    // Save the updated context
                    await this.contextService.UpdateContextAsync(userId, conversationContext);

                    // --- Response Generation ---
                    string responseMessage = GenerateResponseMessage(
                        finalParametersForSearch,
                        recommendations.Count(),
                        conversationContext);

                    // Save final AI response
                    await this.SaveChatHistoryAsync(userId, message, responseMessage, conversationSessionId);

                    return new ChatResponse
                    {
                        Message = responseMessage,
                        RecommendedVehicles = recommendations.ToList(),
                        UpdatedParameters = finalParametersForSearch,
                        ClarificationNeeded = false,
                        ConversationId = message.ConversationId,
                    };
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error processing chat message for user ID {UserId}", userId);

                // Return error response
                return new ChatResponse { Message = "I'm sorry, I encountered an error...", ConversationId = message.ConversationId };
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
            RecommendationParameters mergedParams =
                userIntent != null && string.Equals(userIntent, "refine_criteria", StringComparison.OrdinalIgnoreCase)
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
                    : newParams;

            if (userIntent != null && (string.Equals(userIntent, "refine_criteria", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(userIntent, "new_query", StringComparison.OrdinalIgnoreCase)))
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
            else if (userIntent != null && string.Equals(userIntent, "add_criteria", StringComparison.OrdinalIgnoreCase))
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
                    mergedParams.DesiredFeatures = newParams.DesiredFeatures
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

            if (!hasPrice)
            {
                missingParameterTypes++;
            }

            if (!hasVehicleType)
            {
                missingParameterTypes++;
            }

            if (!hasMakes)
            {
                missingParameterTypes++;
            }

            if (!context.ConfirmedMinYear.HasValue && !context.ConfirmedMaxYear.HasValue)
            {
                missingParameterTypes++;
            }

            if (!context.ConfirmedMaxMileage.HasValue)
            {
                missingParameterTypes++;
            }

            if (!context.ConfirmedFuelTypes.Any())
            {
                missingParameterTypes++;
            }

            // NEW: Count transmission, engine size, and horsepower as optional parameters
            // that don't necessarily trigger clarification if they're the only ones missing
            if (!context.ConfirmedTransmission.HasValue)
            {
                missingParameterTypes += 0.5;
            }

            if (!context.ConfirmedMinEngineSize.HasValue && !context.ConfirmedMaxEngineSize.HasValue)
            {
                missingParameterTypes += 0.5;
            }

            if (!context.ConfirmedMinHorsePower.HasValue && !context.ConfirmedMaxHorsePower.HasValue)
            {
                missingParameterTypes += 0.5;
            }

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
                {
                    _ = response.Append("Based on our conversation, ");
                }
                else
                {
                    _ = response.Append("Great! ");
                }

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
                        confirmedMaxHorsePower = context.ConfirmedMaxHorsePower,
                    }
                    : null,
                    rejectedContext = context != null ? new
                    {
                        rejectedMakes = context.RejectedMakes,
                        rejectedVehicleTypes = context.RejectedVehicleTypes.Select(t => t.ToString()),
                        rejectedFuelTypes = context.RejectedFuelTypes.Select(f => f.ToString()),
                        rejectedTransmission = context.RejectedTransmission?.ToString(),
                    }
                    : null,
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

                    Intent = jsonDoc.RootElement.TryGetProperty("intent", out JsonElement intentElement) && intentElement.ValueKind == JsonValueKind.String
                             ? intentElement.GetString()
                             : "new_query", // Default intent

                    RetrieverSuggestion = jsonDoc.RootElement.TryGetProperty("retrieverSuggestion", out JsonElement retrieverSuggestionElement) && retrieverSuggestionElement.ValueKind == JsonValueKind.String
                                          ? retrieverSuggestionElement.GetString()
                                          : null,

                    // Initialize ClarificationNeeded in the constructor
                    ClarificationNeeded = jsonDoc.RootElement.TryGetProperty("clarificationNeeded", out JsonElement clarificationNeededElement) &&
                                          clarificationNeededElement.ValueKind == JsonValueKind.True,

                    // Initialize ClarificationNeededFor as empty list
                    ClarificationNeededFor = new List<string>(),
                };

                // Parse the clarificationNeededFor array properly
                if (jsonDoc.RootElement.TryGetProperty("clarificationNeededFor", out JsonElement cnForElement) &&
                    cnForElement.ValueKind == JsonValueKind.Array)
                {
                    parameters.ClarificationNeededFor = cnForElement.EnumerateArray()
                        .Where(e => e.ValueKind == JsonValueKind.String)
                        .Select(e => e.GetString())
                        .Where(s => s != null)
                        .Select(s => s!)
                        .ToList();
                }

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

                // Parse retriever suggestion - use a different variable name to avoid duplication
                if (jsonDoc.RootElement.TryGetProperty(
                    "retrieverSuggestion",
                    out JsonElement suggestionElement) &&
                    suggestionElement.ValueKind == JsonValueKind.String)
                {
                    parameters.RetrieverSuggestion = suggestionElement.GetString();
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
            {
                context.ConfirmedMinPrice = parameters.MinPrice;
            }

            if (parameters.MaxPrice.HasValue)
            {
                context.ConfirmedMaxPrice = parameters.MaxPrice;
            }

            if (parameters.MinYear.HasValue)
            {
                context.ConfirmedMinYear = parameters.MinYear;
            }

            if (parameters.MaxYear.HasValue)
            {
                context.ConfirmedMaxYear = parameters.MaxYear;
            }

            if (parameters.MaxMileage.HasValue)
            {
                context.ConfirmedMaxMileage = parameters.MaxMileage;
            }

            // Update confirmed makes, removing from rejected if necessary
            if (parameters.PreferredMakes?.Any() == true)
            {
                foreach (var make in parameters.PreferredMakes)
                {
                    // Remove from rejected list if present (case-insensitive)
                    context.RejectedMakes.RemoveAll(m =>
                        string.Equals(m, make, StringComparison.OrdinalIgnoreCase));

                    // Add to confirmed list if not already present (case-insensitive)
                    bool alreadyConfirmed = context.ConfirmedMakes.Any(m =>
                        string.Equals(m, make, StringComparison.OrdinalIgnoreCase));

                    if (!alreadyConfirmed)
                    {
                        context.ConfirmedMakes.Add(make);
                    }
                }
            }

            // Update confirmed vehicle types, removing from rejected if necessary
            if (parameters.PreferredVehicleTypes?.Any() == true)
            {
                foreach (var type in parameters.PreferredVehicleTypes)
                {
                    // Remove this type from rejected using direct enum equality
                    if (context.RejectedVehicleTypes.Contains(type))
                    {
                        context.RejectedVehicleTypes.Remove(type);
                    }

                    // Add to confirmed using direct enum equality
                    if (!context.ConfirmedVehicleTypes.Contains(type))
                    {
                        context.ConfirmedVehicleTypes.Add(type);
                    }
                }
            }

            // Update confirmed fuel types, removing from rejected if necessary
            if (parameters.PreferredFuelTypes?.Any() == true)
            {
                foreach (var fuel in parameters.PreferredFuelTypes)
                {
                    // Remove this fuel type from rejected using direct enum equality
                    if (context.RejectedFuelTypes.Contains(fuel))
                    {
                        context.RejectedFuelTypes.Remove(fuel);
                    }

                    // Add to confirmed using direct enum equality
                    if (!context.ConfirmedFuelTypes.Contains(fuel))
                    {
                        context.ConfirmedFuelTypes.Add(fuel);
                    }
                }
            }

            // Update confirmed features, removing from rejected if necessary
            if (parameters.DesiredFeatures?.Any() == true)
            {
                foreach (var feature in parameters.DesiredFeatures)
                {
                    // Remove from rejected features if present (case-insensitive)
                    context.RejectedFeatures.RemoveAll(f =>
                        string.Equals(f, feature, StringComparison.OrdinalIgnoreCase));

                    // Add to confirmed features if not already present (case-insensitive)
                    bool alreadyConfirmed = context.ConfirmedFeatures.Any(f =>
                        string.Equals(f, feature, StringComparison.OrdinalIgnoreCase));

                    if (!alreadyConfirmed)
                    {
                        context.ConfirmedFeatures.Add(feature);
                    }
                }
            }

            // Update transmission preference
            if (parameters.Transmission.HasValue)
            {
                // Only remove from rejected if it matches exactly
                if (context.RejectedTransmission == parameters.Transmission.Value)
                {
                    context.RejectedTransmission = null;
                }

                context.ConfirmedTransmission = parameters.Transmission.Value;
            }

            // Update engine size range
            if (parameters.MinEngineSize.HasValue)
            {
                context.ConfirmedMinEngineSize = parameters.MinEngineSize;
            }

            if (parameters.MaxEngineSize.HasValue)
            {
                context.ConfirmedMaxEngineSize = parameters.MaxEngineSize;
            }

            // Update horsepower range
            if (parameters.MinHorsePower.HasValue)
            {
                context.ConfirmedMinHorsePower = parameters.MinHorsePower;
            }

            if (parameters.MaxHorsePower.HasValue)
            {
                context.ConfirmedMaxHorsePower = parameters.MaxHorsePower;
            }

            // Process explicitly negated makes
            if (parameters.ExplicitlyNegatedMakes?.Any() == true)
            {
                foreach (var make in parameters.ExplicitlyNegatedMakes)
                {
                    // Remove from confirmed makes if present (case-insensitive)
                    context.ConfirmedMakes.RemoveAll(m =>
                        string.Equals(m, make, StringComparison.OrdinalIgnoreCase));

                    // Add to rejected makes if not already present (case-insensitive)
                    bool alreadyRejected = context.RejectedMakes.Any(m =>
                        string.Equals(m, make, StringComparison.OrdinalIgnoreCase));

                    if (!alreadyRejected)
                    {
                        context.RejectedMakes.Add(make);
                        this.logger.LogDebug("Added explicitly negated make '{Make}' to rejected context.", make);
                    }
                }
            }

            // Process explicitly negated vehicle types - ensure proper enum parsing and usage
            if (parameters.ExplicitlyNegatedVehicleTypes?.Any() == true)
            {
                foreach (var type in parameters.ExplicitlyNegatedVehicleTypes)
                {
                    // Remove from confirmed if present (direct enum equality)
                    if (context.ConfirmedVehicleTypes.Contains(type))
                    {
                        context.ConfirmedVehicleTypes.Remove(type);
                    }

                    // Add to rejected if not already present (direct enum equality)
                    if (!context.RejectedVehicleTypes.Contains(type))
                    {
                        context.RejectedVehicleTypes.Add(type);
                        this.logger.LogDebug("Added explicitly negated vehicle type '{Type}' to rejected context.", type);
                    }
                }
            }

            // Process explicitly negated fuel types - ensure proper enum parsing and usage
            if (parameters.ExplicitlyNegatedFuelTypes?.Any() == true)
            {
                foreach (var fuel in parameters.ExplicitlyNegatedFuelTypes)
                {
                    // Remove from confirmed if present (direct enum equality)
                    if (context.ConfirmedFuelTypes.Contains(fuel))
                    {
                        context.ConfirmedFuelTypes.Remove(fuel);
                    }

                    // Add to rejected if not already present (direct enum equality)
                    if (!context.RejectedFuelTypes.Contains(fuel))
                    {
                        context.RejectedFuelTypes.Add(fuel);
                        this.logger.LogDebug("Added explicitly negated fuel type '{Fuel}' to rejected context.", fuel);
                    }
                }
            }
        }

        private void SynchronizeCurrentParametersWithConfirmedValues(ConversationContext context)
        {
            // Ensure CurrentParameters is initialized
            if (context.CurrentParameters == null)
            {
                context.CurrentParameters = new RecommendationParameters();
            }

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

            // Sync vehicle types, excluding rejected ones using direct enum equality
            context.CurrentParameters.PreferredVehicleTypes = context.ConfirmedVehicleTypes
                .Where(t => !context.RejectedVehicleTypes.Contains(t))
                .ToList();

            // Sync fuel types, excluding rejected ones using direct enum equality
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

            // Copy rejected lists directly to CurrentParameters for filtering
            context.CurrentParameters.RejectedMakes = context.RejectedMakes.ToList();
            context.CurrentParameters.RejectedVehicleTypes = context.RejectedVehicleTypes.ToList();
            context.CurrentParameters.RejectedFuelTypes = context.RejectedFuelTypes.ToList();
            context.CurrentParameters.RejectedFeatures = context.RejectedFeatures.ToList();
            context.CurrentParameters.RejectedTransmission = context.RejectedTransmission;

            // Update MentionedVehicleFeatures for backward compatibility (if still needed)
            // Consider removing if MentionedVehicleFeatures is deprecated
            foreach (var feature in context.ConfirmedFeatures)
            {
                if (!context.MentionedVehicleFeatures.Contains(feature))
                {
                    context.MentionedVehicleFeatures.Add(feature);
                }
            }
        }

        private Expression<Func<Vehicle, bool>> BuildFilterExpression(RecommendationParameters parameters)
        {
            // Start with a predicate that always returns true
            var expression = PredicateBuilder.True<Vehicle>();

            if (parameters.MinPrice.HasValue)
            {
                expression = CombineExpressions(expression, v => v.Price >= parameters.MinPrice.Value);
            }

            if (parameters.MaxPrice.HasValue)
            {
                expression = CombineExpressions(expression, v => v.Price <= parameters.MaxPrice.Value);
            }

            if (parameters.MinYear.HasValue)
            {
                expression = CombineExpressions(expression, v => v.Year >= parameters.MinYear.Value);
            }

            if (parameters.MaxYear.HasValue)
            {
                expression = CombineExpressions(expression, v => v.Year <= parameters.MaxYear.Value);
            }

            if (parameters.MaxMileage.HasValue)
            {
                expression = CombineExpressions(expression, v => v.Mileage <= parameters.MaxMileage.Value);
            }

            if (parameters.PreferredMakes?.Any() == true)
            {
                expression = CombineExpressions(expression, v => parameters.PreferredMakes.Contains(v.Make));
            }

            if (parameters.RejectedMakes?.Any() == true)
            {
                expression = CombineExpressions(expression, v => !parameters.RejectedMakes.Contains(v.Make));
            }

            // Correct enum comparisons for vehicle types
            if (parameters.PreferredVehicleTypes?.Any() == true)
            {
                expression = CombineExpressions(expression, v => parameters.PreferredVehicleTypes.Contains(v.VehicleType));
            }

            if (parameters.RejectedVehicleTypes?.Any() == true)
            {
                expression = CombineExpressions(expression, v => !parameters.RejectedVehicleTypes.Contains(v.VehicleType));
            }

            // Correct enum comparisons for fuel types
            if (parameters.PreferredFuelTypes?.Any() == true)
            {
                expression = CombineExpressions(expression, v => parameters.PreferredFuelTypes.Contains(v.FuelType));
            }

            if (parameters.RejectedFuelTypes?.Any() == true)
            {
                expression = CombineExpressions(expression, v => !parameters.RejectedFuelTypes.Contains(v.FuelType));
            }

            if (parameters.Transmission.HasValue)
            {
                expression = CombineExpressions(expression, v => v.Transmission == parameters.Transmission.Value);
            }

            if (parameters.RejectedTransmission.HasValue)
            {
                expression = CombineExpressions(expression, v => v.Transmission != parameters.RejectedTransmission.Value);
            }

            if (parameters.MinEngineSize.HasValue)
            {
                expression = CombineExpressions(expression, v => v.EngineSize >= parameters.MinEngineSize.Value);
            }

            if (parameters.MaxEngineSize.HasValue)
            {
                expression = CombineExpressions(expression, v => v.EngineSize <= parameters.MaxEngineSize.Value);
            }

            if (parameters.MinHorsePower.HasValue)
            {
                expression = CombineExpressions(expression, v => v.HorsePower >= parameters.MinHorsePower.Value);
            }

            if (parameters.MaxHorsePower.HasValue)
            {
                expression = CombineExpressions(expression, v => v.HorsePower <= parameters.MaxHorsePower.Value);
            }

            // Feature filters (case-insensitive)
            if (parameters.DesiredFeatures?.Any() == true)
            {
                foreach (string feature in parameters.DesiredFeatures)
                {
                    expression = CombineExpressions(expression, v =>
                        v.Features != null && v.Features.Any(f => f.Name.Equals(feature, StringComparison.OrdinalIgnoreCase)));
                }
            }

            if (parameters.RejectedFeatures?.Any() == true)
            {
                foreach (string feature in parameters.RejectedFeatures)
                {
                    expression = CombineExpressions(expression, v =>
                        v.Features == null || !v.Features.Any(f => f.Name.Equals(feature, StringComparison.OrdinalIgnoreCase)));
                }
            }

            return expression;
        }

        // Helper to combine expressions
        private static Expression<Func<T, bool>> CombineExpressions<T>(
            Expression<Func<T, bool>> expr1,
            Expression<Func<T, bool>> expr2)
        {
            return PredicateBuilder.And(expr1, expr2);
        }
    }
}