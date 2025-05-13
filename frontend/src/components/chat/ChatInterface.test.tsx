// frontend/src/components/chat/ChatInterface.test.tsx
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { vi } from 'vitest';
import ChatInterface from './ChatInterface';
import { AuthContext } from '../../contexts/AuthContext';
import {
  ChatResponse as ChatServiceResponse,
  ConversationResponse as ServiceNewConversationResponse,
} from '../../services/chatService';

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

import actualChatServiceInstance from '../../services/chatService';

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
  beforeEach(() => {
    vi.resetAllMocks();
    mockGetConversationsFn.mockResolvedValue([]);
    mockGetChatHistoryFn.mockResolvedValue([]);
    // Default for any mount-time conversation request, can be overridden in tests
    mockStartNewConversationFn.mockResolvedValue({
      conversationId: 'default-conv-id',
      welcomeMessage: 'Default Welcome!',
    });
  });

  test('sends message and displays user and AI messages, including initial welcome', async () => {
    const welcomeMessageText = 'New convo started for send';
    const conversationIdForTest = 'convSend123';
    const userMessageText = 'Hello AI';
    const aiResponseMessageText = 'Hello from AI!';

    mockStartNewConversationFn.mockResolvedValueOnce({
      conversationId: conversationIdForTest,
      welcomeMessage: welcomeMessageText,
    } as ServiceNewConversationResponse);

    mockSendMessageFn.mockResolvedValueOnce({
      message: aiResponseMessageText,
      conversationId: conversationIdForTest,
      recommendedVehicles: [],
      parameters: {},
      clarificationNeeded: false,
    } as ChatServiceResponse);

    renderChatInterface();

    expect(await screen.findByText(welcomeMessageText)).toBeInTheDocument();

    const inputField = screen.getByPlaceholderText(/Ask about cars.../i);
    const sendButton = screen.getByRole('button', { name: /Send message/i });

    fireEvent.change(inputField, { target: { value: userMessageText } });
    fireEvent.click(sendButton);

    expect(await screen.findByText(userMessageText)).toBeInTheDocument();
    expect(await screen.findByText(aiResponseMessageText)).toBeInTheDocument();

    expect(mockStartNewConversationFn).toHaveBeenCalledTimes(1);
    expect(mockSendMessageFn).toHaveBeenCalledWith(
      userMessageText,
      undefined,
      false,
      true,
      conversationIdForTest
    );
  });

  test('displays an error message if sending a message (after initial welcome) fails', async () => {
    const welcomeMessageText = 'Welcome for error test';
    const conversationIdForTest = 'convError123';
    const userMessageText = 'Test error message';

    mockStartNewConversationFn.mockResolvedValueOnce({
      conversationId: conversationIdForTest,
      welcomeMessage: welcomeMessageText,
    });
    mockSendMessageFn.mockRejectedValueOnce(new Error('Network Error'));

    renderChatInterface();

    expect(await screen.findByText(welcomeMessageText)).toBeInTheDocument();

    fireEvent.change(screen.getByPlaceholderText(/Ask about cars.../i), {
      target: { value: userMessageText },
    });
    fireEvent.click(screen.getByRole('button', { name: /Send message/i }));

    expect(await screen.findByText(userMessageText)).toBeInTheDocument();
    expect(
      await screen.findByText(
        /Sorry, I encountered an error processing your request. Please try again./i
      )
    ).toBeInTheDocument();
  });

  test('calls onRecommendationsUpdated when API response includes recommendations', async () => {
    const welcomeMessageText = 'Welcome for recs test';
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
      welcomeMessage: welcomeMessageText,
    } as ServiceNewConversationResponse);

    mockSendMessageFn.mockResolvedValueOnce({
      message: 'AI says here are some recs!',
      recommendedVehicles: recommendedVehiclesData,
      parameters: parametersForRecs,
      conversationId: conversationIdForTest,
      clarificationNeeded: false,
    } as ChatServiceResponse);

    renderChatInterface();

    expect(await screen.findByText(welcomeMessageText)).toBeInTheDocument();

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
    const welcomeMessageText = 'Welcome for clarify test';
    const conversationIdForTest = 'clarify-convo-id';
    const clarificationMessageText =
      'I need more details. What is your budget?';

    mockStartNewConversationFn.mockResolvedValueOnce({
      conversationId: conversationIdForTest,
      welcomeMessage: welcomeMessageText,
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

    expect(await screen.findByText(welcomeMessageText)).toBeInTheDocument();

    fireEvent.change(screen.getByPlaceholderText(/Ask about cars.../i), {
      target: { value: 'Find a car' },
    });
    fireEvent.click(screen.getByRole('button', { name: /Send message/i }));

    expect(await screen.findByText('Find a car')).toBeInTheDocument();
    expect(
      await screen.findByText(clarificationMessageText)
    ).toBeInTheDocument();
    expect(
      screen.getByPlaceholderText(/Please provide more details.../i)
    ).toBeInTheDocument();
  });

  test('displays an error message if sending a message (starting a new conversation) fails', async () => {
    mockStartNewConversationFn.mockRejectedValueOnce(
      new Error('Simulated API Error for new convo')
    );

    renderChatInterface();

    const inputField = screen.getByPlaceholderText('Ask about cars...');
    fireEvent.change(inputField, { target: { value: 'Hello' } });
    const sendButton = screen.getByRole('button', { name: /Send message/i });
    fireEvent.click(sendButton);

    expect(
      await screen.findByText(
        /Sorry, I encountered an error processing your request. Please try again./i
      )
    ).toBeInTheDocument();
  });

  test('displays an error message if sending a follow-up message fails', async () => {
    const testNumericId = 1;
    const testStringConversationId = testNumericId.toString();

    mockGetConversationsFn.mockResolvedValueOnce([
      {
        id: testNumericId,
        createdAt: new Date().toISOString(),
        lastInteractionAt: new Date().toISOString(),
        messageCount: 2,
      },
    ]);

    mockGetChatHistoryFn.mockResolvedValueOnce([
      {
        id: 1,
        userMessage: 'Old message',
        aiResponse: 'Old response',
        timestamp: new Date().toISOString(),
        conversationId: testStringConversationId,
      },
    ]);

    mockSendMessageFn.mockRejectedValueOnce(
      new Error('Simulated Send Error for follow-up')
    );

    renderChatInterface();

    await screen.findByText('Old message');

    const input = screen.getByPlaceholderText('Ask about cars...');
    const sendButton = screen.getByRole('button', { name: /Send message/i });

    fireEvent.change(input, { target: { value: 'Another message' } });
    fireEvent.click(sendButton);

    expect(
      await screen.findByText(
        /Sorry, I encountered an error processing your request. Please try again./i
      )
    ).toBeInTheDocument();

    expect(mockSendMessageFn).toHaveBeenCalledWith(
      'Another message',
      undefined,
      false,
      true,
      testStringConversationId
    );
  });

  test('sends first message and shows specific welcome', async () => {
    mockStartNewConversationFn.mockResolvedValueOnce({
      conversationId: 'mount-id',
      welcomeMessage: 'Mount-time welcome',
    });
    mockStartNewConversationFn.mockResolvedValueOnce({
      conversationId: 'test-id',
      welcomeMessage: 'EXPECTED_WELCOME',
    });
    mockSendMessageFn.mockResolvedValueOnce({
      message: 'AI response',
      conversationId: 'test-id',
    });

    renderChatInterface();

    await screen.findByText('Mount-time welcome');

    fireEvent.change(screen.getByPlaceholderText(/Ask about cars/i), {
      target: { value: 'User message' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Send message' }));

    expect(await screen.findByText('EXPECTED_WELCOME')).toBeInTheDocument();
    expect(await screen.findByText('AI response')).toBeInTheDocument();
  });

  test('displays error when new conversation fails', async () => {
    mockStartNewConversationFn.mockRejectedValueOnce(new Error('Network Fail'));

    renderChatInterface();

    fireEvent.change(screen.getByPlaceholderText(/Ask about cars/i), {
      target: { value: 'Trigger error' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Send message' }));

    expect(
      await screen.findByText(/Sorry, I encountered an error/i)
    ).toBeInTheDocument();
  });
});
