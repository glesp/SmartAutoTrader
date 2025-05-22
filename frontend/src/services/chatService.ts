/**
 * @file chatService.ts
 * @summary Provides services for interacting with the AI chat assistant and managing conversations.
 *
 * @description This module defines interfaces for chat-related data structures and exports a `chatService` object.
 * The `chatService` encapsulates methods for sending messages to the AI, retrieving conversation history,
 * starting new conversations, and fetching a list of existing conversations. It uses the configured Axios instance
 * from `./api` to communicate with the backend chat API endpoints.
 *
 * @remarks
 * - Handles responses that might be wrapped in an ASP.NET Core `$values` structure.
 * - Defines clear TypeScript interfaces for request payloads and response data to ensure type safety.
 * - All interactions with the chat API are centralized within this service.
 *
 * @dependencies
 * - api (./api): The configured Axios instance for making HTTP requests.
 * - Vehicle (../types/models): Type definition for vehicle objects.
 * - RecommendationParameters (../types/models): Type definition for recommendation parameters.
 */
import api from './api'; // Your configured axios instance
import { Vehicle, RecommendationParameters } from '../types/models';

/**
 * @interface ChatResponse
 * @summary Represents the response received from the chat service after sending a message.
 *
 * @property {string} message - The AI's textual response.
 * @property {Vehicle[]} [recommendedVehicles] - Optional array of vehicles recommended by the AI.
 * @property {RecommendationParameters} [parameters] - Optional parameters used by the AI to generate recommendations.
 * @property {boolean} [clarificationNeeded] - Optional flag indicating if the AI needs further clarification from the user.
 * @property {string} [conversationId] - Optional ID of the current conversation, useful if a new conversation was implicitly started.
 */
export interface ChatResponse {
  message: string;
  recommendedVehicles?: Vehicle[];
  parameters?: RecommendationParameters;
  clarificationNeeded?: boolean;
  conversationId?: string;
}

/**
 * @interface ConversationResponse
 * @summary Represents the response received when starting a new conversation.
 *
 * @property {string} conversationId - The unique identifier for the newly created conversation.
 * @property {string} welcomeMessage - A welcome message from the AI for the new conversation.
 *                                    Note: The UI might not use this directly if it has its own welcome flow.
 */
export interface ConversationResponse {
  // For starting a new conversation
  conversationId: string;
  welcomeMessage: string; // Though your UI might not use this directly
}

/**
 * @interface ConversationInfo
 * @summary Represents metadata for a single conversation.
 *
 * @property {number} id - The unique identifier for the conversation (backend might send a number).
 * @property {string} createdAt - ISO date string representing when the conversation was created.
 * @property {string} lastInteractionAt - ISO date string representing the time of the last interaction in the conversation.
 * @property {number} messageCount - The total number of messages (user and AI) in the conversation.
 */
export interface ConversationInfo {
  id: number; // Assuming backend sends number, will be mapped to string if needed by component
  createdAt: string;
  lastInteractionAt: string;
  messageCount: number;
}

/**
 * @interface ChatHistoryItem
 * @summary Represents a single item (a user message and AI response pair) in a conversation's history.
 *
 * @property {number} id - The unique identifier for this chat history entry.
 * @property {string} userMessage - The message sent by the user.
 * @property {string} aiResponse - The corresponding response from the AI.
 * @property {string} timestamp - ISO date string representing when this interaction occurred.
 * @property {string} conversationId - The identifier of the conversation this item belongs to.
 */
export interface ChatHistoryItem {
  id: number;
  userMessage: string;
  aiResponse: string;
  timestamp: string;
  conversationId: string; // Or number, ensure consistency
}

/**
 * @summary Service object for AI chat-related API calls.
 */
export const chatService = {
  /**
   * @summary Sends a message to the AI chat assistant.
   * @param {string} message - The content of the user's message.
   * @param {string} [originalUserInput] - The original user input if the current message is a clarification or follow-up.
   * @param {boolean} [isClarification] - Flag indicating if this message is a clarification to a previous AI question.
   * @param {boolean} [isFollowUp] - Flag indicating if this message is a follow-up to a previous interaction.
   * @param {string} [conversationId] - The ID of the ongoing conversation. If not provided, the backend might start a new one or use a default.
   * @returns {Promise<ChatResponse>} A promise that resolves with the AI's response, including any recommended vehicles or parameters.
   * @throws {import('axios').AxiosError} If the API request fails.
   * @example
   * chatService.sendMessage("I'm looking for an SUV", undefined, false, false, "conv123")
   *   .then(response => console.log("AI says:", response.message))
   *   .catch(error => console.error("Chat error:", error));
   */
  sendMessage: async (
    message: string,
    originalUserInput?: string,
    isClarification?: boolean,
    isFollowUp?: boolean,
    conversationId?: string
  ): Promise<ChatResponse> => {
    const payload = {
      content: message,
      originalUserInput,
      isClarification,
      isFollowUp,
      conversationId,
    };
    // Corrected URL for sending a message
    const response = await api.post('/api/chat/message', payload);
    return response.data;
  },

  /**
   * @summary Retrieves a list of all conversations for the current user.
   * @returns {Promise<ConversationInfo[]>} A promise that resolves with an array of conversation metadata objects.
   * @remarks Handles potential ASP.NET Core `$values` wrapper in the response. Returns an empty array if data is not in the expected format or on error.
   * @throws {import('axios').AxiosError} If the API request fails (though the current implementation catches and logs, returning empty array).
   * @example
   * chatService.getConversations()
   *   .then(conversations => console.log("User conversations:", conversations))
   *   .catch(error => console.error("Failed to get conversations:", error));
   */
  getConversations: async (): Promise<ConversationInfo[]> => {
    const response = await api.get('/api/chat/conversations');
    // Handle potential $values wrapper from .NET
    if (Array.isArray(response.data)) {
      return response.data;
    } else if (
      response.data &&
      '$values' in response.data &&
      Array.isArray(response.data.$values)
    ) {
      return response.data.$values;
    }
    return []; // Return empty array if data is not in expected format
  },

  /**
   * @summary Starts a new conversation with the AI chat assistant.
   * @returns {Promise<ConversationResponse>} A promise that resolves with the new conversation ID and a welcome message.
   * @throws {import('axios').AxiosError} If the API request fails.
   * @example
   * chatService.startNewConversation()
   *   .then(data => console.log("New conversation started:", data.conversationId))
   *   .catch(error => console.error("Failed to start conversation:", error));
   */
  startNewConversation: async (): Promise<ConversationResponse> => {
    // Corrected URL for starting a new conversation
    const response = await api.post('/api/chat/conversation/new', {});
    return response.data;
  },

  /**
   * @summary Retrieves the chat history for a specific conversation.
   * @param {string} conversationId - The unique identifier of the conversation.
   * @returns {Promise<ChatHistoryItem[]>} A promise that resolves with an array of chat history items (user messages and AI responses).
   * @remarks Handles potential ASP.NET Core `$values` wrapper in the response. Returns an empty array if data is not in the expected format or on error.
   * @throws {import('axios').AxiosError} If the API request fails (though the current implementation catches and logs, returning empty array).
   * @example
   * chatService.getChatHistory("conv123")
   *   .then(history => console.log("Chat history for conv123:", history))
   *   .catch(error => console.error("Failed to get chat history:", error));
   */
  getChatHistory: async (
    conversationId: string
  ): Promise<ChatHistoryItem[]> => {
    const response = await api.get(
      `/api/chat/history?conversationId=${conversationId}`
    );
    // Handle potential $values wrapper from .NET
    if (Array.isArray(response.data)) {
      return response.data;
    } else if (
      response.data &&
      '$values' in response.data &&
      Array.isArray(response.data.$values)
    ) {
      return response.data.$values;
    }
    return []; // Return empty array if data is not in expected format
  },
};

/**
 * @summary Default export of the chatService.
 * @description Provides a convenient way to import the entire service.
 * @example import chatService from './chatService';
 */
export default chatService;
