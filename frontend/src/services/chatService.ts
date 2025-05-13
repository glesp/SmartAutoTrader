import api from './api'; // Your configured axios instance
import { Vehicle, RecommendationParameters } from '../types/models';

// Response Types from the Service
export interface ChatResponse {
  message: string;
  recommendedVehicles?: Vehicle[];
  parameters?: RecommendationParameters;
  clarificationNeeded?: boolean;
  conversationId?: string;
}

export interface ConversationResponse {
  // For starting a new conversation
  conversationId: string;
  welcomeMessage: string; // Though your UI might not use this directly
}

export interface ConversationInfo {
  id: number; // Assuming backend sends number, will be mapped to string if needed by component
  createdAt: string;
  lastInteractionAt: string;
  messageCount: number;
}

export interface ChatHistoryItem {
  id: number;
  userMessage: string;
  aiResponse: string;
  timestamp: string;
  conversationId: string; // Or number, ensure consistency
}

export const chatService = {
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
    const response = await api.post('/api/chat/send', payload);
    return response.data;
  },

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

  startNewConversation: async (): Promise<ConversationResponse> => {
    // Corrected URL for starting a new conversation
    const response = await api.post('/api/chat/start', {});
    return response.data;
  },

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

export default chatService;
