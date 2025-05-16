import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, test, expect, vi, beforeEach } from 'vitest';
import ChatInterface from './ChatInterface';
import { AuthContext } from '../../contexts/AuthContext';
import {
  ChatResponse as ChatServiceResponse,
  ConversationResponse as ServiceNewConversationResponse,
} from '../../services/chatService';

import actualChatServiceInstance from '../../services/chatService';

window.HTMLElement.prototype.scrollIntoView = vi.fn();

vi.mock('../../services/chatService', () => ({
  default: {
    sendMessage: vi.fn(),
    startNewConversation: vi.fn(),
    getConversations: vi.fn(),
    getChatHistory: vi.fn(),
  },
  chatService: {
    sendMessage: vi.fn(),
    startNewConversation: vi.fn(),
    getConversations: vi.fn(),
    getChatHistory: vi.fn(),
  },
}));

const mockedChatService = vi.mocked(actualChatServiceInstance, true);

const mockSendMessageFn = mockedChatService.sendMessage;
const mockStartNewConversationFn = mockedChatService.startNewConversation;
const mockGetConversationsFn = mockedChatService.getConversations;
const mockGetChatHistoryFn = mockedChatService.getChatHistory;

const mockOnRecommendationsUpdated = vi.fn();

const mockAuthContextValue = {
  token: 'test-token',
  user: {
    id: 1,
    username: 'testuser',
    email: 'test@example.com',
    firstName: 'Test',
    lastName: 'User',
    roles: ['User'],
  },
  login: vi.fn(),
  register: vi.fn(),
  logout: vi.fn(),
  loading: false,
  isAuthenticated: true,
};

const renderChatInterface = () => {
  return render(
    <AuthContext.Provider value={mockAuthContextValue}>
      <ChatInterface onRecommendationsUpdated={mockOnRecommendationsUpdated} />
    </AuthContext.Provider>
  );
};

describe('ChatInterface Component', () => {
  test('minimal test case', () => {
    expect(true).toBe(true);
  });

  beforeEach(() => {
    vi.resetAllMocks();
    mockGetConversationsFn.mockResolvedValue([]);
    mockGetChatHistoryFn.mockResolvedValue([]);
    mockStartNewConversationFn.mockResolvedValue({
      conversationId: 'default-conv-id',
      welcomeMessage: '', // <-- Fix: no default welcome
    });
  });

  test('shows empty state and suggested prompts before any message is sent', async () => {
    renderChatInterface();
    expect(await screen.findByText('Start a Conversation')).toBeInTheDocument();
    expect(
      screen.getByText('Welcome to Smart Auto Assistant!')
    ).toBeInTheDocument();
    expect(screen.getByText('I need an SUV under €30,000')).toBeInTheDocument();
    expect(
      screen.getByText('Show me electric cars with good range')
    ).toBeInTheDocument();
    expect(
      screen.getByText('Family cars with low mileage')
    ).toBeInTheDocument();
  });

  test('sends message and displays user and AI messages', async () => {
    const conversationIdForTest = 'convSend123';
    const userMessageText = 'Hello AI';
    const aiResponseMessageText = 'Hello from AI!';

    mockStartNewConversationFn.mockImplementation(() =>
      Promise.resolve({
        conversationId: conversationIdForTest,
        welcomeMessage: '',
      })
    );

    mockSendMessageFn.mockImplementation(() =>
      Promise.resolve({
        message: aiResponseMessageText,
        conversationId: conversationIdForTest,
        recommendedVehicles: [],
        parameters: {},
        clarificationNeeded: false,
      })
    );

    renderChatInterface();

    await screen.findByText('Start a Conversation');
    fireEvent.change(screen.getByPlaceholderText(/Ask about cars.../i), {
      target: { value: userMessageText },
    });
    fireEvent.click(screen.getByRole('button', { name: /Send message/i }));

    await waitFor(() => {
      expect(screen.getByText(userMessageText)).toBeInTheDocument();
      expect(screen.getByText(aiResponseMessageText)).toBeInTheDocument();
    });

    expect(mockStartNewConversationFn).toHaveBeenCalled();
    expect(mockSendMessageFn).toHaveBeenCalledWith(
      userMessageText,
      undefined,
      false,
      false, // <-- not true!
      conversationIdForTest
    );
  });

  test('displays an error message if sending a message (after initial welcome) fails', async () => {
    const conversationIdForTest = 'convError123';
    const userMessageText = 'Test error message';
    const errorMessagePattern = /sorry.*encountered an error/i;

    mockStartNewConversationFn.mockResolvedValueOnce({
      conversationId: conversationIdForTest,
      welcomeMessage: '',
    });
    mockSendMessageFn.mockImplementation(() =>
      Promise.reject(new Error('Network Error'))
    );

    renderChatInterface();

    await screen.findByText('Start a Conversation');
    fireEvent.change(screen.getByPlaceholderText(/Ask about cars.../i), {
      target: { value: userMessageText },
    });
    fireEvent.click(screen.getByRole('button', { name: /Send message/i }));

    await waitFor(() => {
      expect(screen.getByText(userMessageText)).toBeInTheDocument();
      expect(screen.getByText(errorMessagePattern)).toBeInTheDocument();
    });
  });

  test('calls onRecommendationsUpdated when API response includes recommendations', async () => {
    const conversationIdForTest = 'recs-convo-id';
    const recommendedVehiclesData = [
      {
        id: 1,
        make: 'TestRecCar',
        model: 'RecModel',
        year: 2022,
        price: 20000,
        mileage: 1000,
        fuelType: 'Electric',
        transmission: 'Automatic',
        images: [],
        status: 0,
        dateListed: new Date().toISOString(),
        vin: 'testvin1',
        description: 'desc',
        vehicleType: 'SUV',
        engineSize: 0,
        horsePower: 0,
        cityMpg: 0,
        highwayMpg: 0,
        features: [],
        ownerReviews: [],
        expertReviews: [],
        similarVehicles: [],
        inquiryCount: 0,
        favoriteCount: 0,
        viewCount: 0,
      },
    ];
    const parametersForRecs = { intent: 'vehicleSearch' };

    mockStartNewConversationFn.mockResolvedValueOnce({
      conversationId: conversationIdForTest,
      welcomeMessage: '',
    } as ServiceNewConversationResponse);

    mockSendMessageFn.mockResolvedValueOnce({
      message: 'AI says here are some recs!',
      recommendedVehicles: recommendedVehiclesData,
      parameters: parametersForRecs,
      conversationId: conversationIdForTest,
      clarificationNeeded: false,
    } as ChatServiceResponse);

    renderChatInterface();

    await screen.findByText('Start a Conversation');
    fireEvent.change(screen.getByPlaceholderText(/Ask about cars.../i), {
      target: { value: 'Show me cars' },
    });
    fireEvent.click(screen.getByRole('button', { name: /Send message/i }));

    await waitFor(() => {
      expect(mockOnRecommendationsUpdated).toHaveBeenCalledWith(
        recommendedVehiclesData,
        parametersForRecs
      );
    });
  });

  test('handles clarification needed', async () => {
    const conversationIdForTest = 'clarify-convo-id';
    const clarificationMessageText =
      'I need more details. What is your budget?';

    mockStartNewConversationFn.mockResolvedValueOnce({
      conversationId: conversationIdForTest,
      welcomeMessage: '',
    } as ServiceNewConversationResponse);

    mockSendMessageFn.mockResolvedValueOnce({
      message: clarificationMessageText,
      clarificationNeeded: true,
      conversationId: conversationIdForTest,
      parameters: {
        intent: 'clarification',
        clarificationNeededFor: ['budget'],
      },
    } as ChatServiceResponse);

    renderChatInterface();

    await screen.findByText('Start a Conversation');
    fireEvent.change(screen.getByPlaceholderText(/Ask about cars.../i), {
      target: { value: 'Find a car' },
    });
    fireEvent.click(screen.getByRole('button', { name: /Send message/i }));

    await waitFor(() => {
      expect(screen.getByText(clarificationMessageText)).toBeInTheDocument();
      expect(
        screen.getByPlaceholderText(/Please provide more details.../i)
      ).toBeInTheDocument();
    });
  });

  test('sends first message and shows user and AI response (no welcome)', async () => {
    mockStartNewConversationFn.mockResolvedValueOnce({
      conversationId: 'test-id',
      welcomeMessage: '',
    });
    mockSendMessageFn.mockResolvedValueOnce({
      message: 'AI response',
      conversationId: 'test-id',
      clarificationNeeded: false,
      recommendedVehicles: [],
      parameters: {},
    });

    renderChatInterface();

    await screen.findByText('Start a Conversation');
    await screen.findByText('Welcome to Smart Auto Assistant!');

    fireEvent.change(screen.getByPlaceholderText(/Ask about cars/i), {
      target: { value: 'User message' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Send message' }));

    await waitFor(() => {
      expect(screen.getByText('AI response')).toBeInTheDocument();
    });
  });

  test('displays error when new conversation fails', async () => {
    mockStartNewConversationFn.mockRejectedValueOnce(new Error('Network Fail'));

    renderChatInterface();

    await screen.findByText('Start a Conversation');

    fireEvent.change(screen.getByPlaceholderText(/Ask about cars/i), {
      target: { value: 'Trigger error' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Send message' }));

    expect(
      await screen.findByText(
        /Sorry, I encountered an error processing your request/i
      )
    ).toBeInTheDocument();
  });
});
