import { useState, useRef, useEffect, useContext, useCallback } from 'react';
// Removed: import axios from 'axios'; // No longer needed for direct calls
import { AuthContext } from '../../contexts/AuthContext';
import { Vehicle, RecommendationParameters } from '../../types/models';
import Box from '@mui/material/Box';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';
import Paper from '@mui/material/Paper';
import CircularProgress from '@mui/material/CircularProgress';
import SendIcon from '@mui/icons-material/Send';
import InputAdornment from '@mui/material/InputAdornment';
import IconButton from '@mui/material/IconButton';
import Tooltip from '@mui/material/Tooltip';
import Chip from '@mui/material/Chip';
import Button from '@mui/material/Button';
import Menu from '@mui/material/Menu';
import MenuItem from '@mui/material/MenuItem';
import ListItemText from '@mui/material/ListItemText';
import ListItemIcon from '@mui/material/ListItemIcon';
import HistoryIcon from '@mui/icons-material/History';
import ChatIcon from '@mui/icons-material/Chat';
import AddIcon from '@mui/icons-material/Add';
import Zoom from '@mui/material/Zoom';
import Fade from '@mui/material/Fade';

import chatService from '../../services/chatService'; // Adjust path if necessary
import {
  ChatResponse as ChatServiceResponse, // Alias to avoid naming conflict if necessary
  ConversationInfo as ServiceConversationInfo,
  ChatHistoryItem as ServiceChatHistoryItem,
  ConversationResponse as ServiceNewConversationResponse,
} from '../../services/chatService'; // Adjust path and import types if they are exported from service

// Type definitions (keeping component-specific types for clarity if they differ subtly)
interface ChatInterfaceProps {
  onRecommendationsUpdated: (
    vehicles: Vehicle[],
    parameters: RecommendationParameters
  ) => void;
}

interface Message {
  id: string | number;
  content: string;
  sender: 'user' | 'ai';
  timestamp: Date;
  vehicles?: Vehicle[];
  parameters?: RecommendationParameters;
  clarificationNeeded?: boolean;
  originalUserInput?: string;
  conversationId?: string;
}

// This DTO seems to be what the component expects the service to ultimately return or be mapped to for the AI's turn.
// It aligns well with ChatServiceResponse.
interface ChatResponseDTO {
  message: string;
  recommendedVehicles?: Vehicle[];
  parameters?: RecommendationParameters;
  clarificationNeeded?: boolean;
  originalUserInput?: string;
  conversationId?: string;
  matchedCategory?: string;
}

interface Conversation {
  // Component's internal state for conversations
  id: string; // Ensure ID is string for consistency if service returns number
  createdAt: string;
  lastInteractionAt: string;
  messageCount: number;
}

// Helper function for date formatting
const formatDate = (dateString: string): string => {
  try {
    const date = new Date(dateString);
    return date.toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
      hour12: true,
    });
  } catch {
    return dateString;
  }
};

const ChatInterface = ({ onRecommendationsUpdated }: ChatInterfaceProps) => {
  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState<string>('');
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [updatingRecommendations, setUpdatingRecommendations] =
    useState<boolean>(false);
  const [clarificationState, setClarificationState] = useState<{
    awaiting: boolean;
    originalUserInput: string;
  }>({ awaiting: false, originalUserInput: '' });
  const [currentConversationId, setCurrentConversationId] = useState<
    string | undefined
  >(undefined);
  const [conversations, setConversations] = useState<Conversation[]>([]);
  const [conversationsMenuAnchor, setConversationsMenuAnchor] =
    useState<null | HTMLElement>(null);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const { token } = useContext(AuthContext); // Token is used by the Axios interceptor in the chatService

  const suggestedPrompts = [
    'I need an SUV under €30,000',
    'Show me electric cars with good range',
    'Family cars with low mileage',
  ];

  const loadConversations = useCallback(async () => {
    if (!token) return; // Ensure token exists before calling service
    try {
      // Use chatService
      const serviceConversations: ServiceConversationInfo[] =
        await chatService.getConversations();

      const conversationsData: Conversation[] = serviceConversations.map(
        (sc) => ({
          id: sc.id.toString(), // Assuming service ID is number, component uses string
          createdAt: sc.createdAt,
          lastInteractionAt: sc.lastInteractionAt,
          messageCount: sc.messageCount,
        })
      );

      setConversations(conversationsData);

      if (conversationsData.length > 0 && !currentConversationId) {
        // Automatically select the most recent existing conversation
        // Assuming serviceConversations are sorted most recent first or component sorts them
        const sortedConversations = [...conversationsData].sort(
          (a, b) =>
            new Date(b.lastInteractionAt).getTime() -
            new Date(a.lastInteractionAt).getTime()
        );
        if (sortedConversations.length > 0) {
          setCurrentConversationId(sortedConversations[0].id);
        }
      }
    } catch (error) {
      console.error('Failed to load conversations:', error);
    }
  }, [token, currentConversationId]); // currentConversationId dependency might be re-evaluated

  const loadChatHistory = useCallback(
    async (conversationId: string) => {
      if (!token || !conversationId) return;
      try {
        // Use chatService
        const historyData: ServiceChatHistoryItem[] =
          await chatService.getChatHistory(conversationId);

        const formattedHistory: Message[] = historyData
          .flatMap((msg: ServiceChatHistoryItem) => [
            // Use flatMap for cleaner structure
            {
              id: `user-${msg.id}`, // Ensure unique IDs
              content: msg.userMessage,
              sender: 'user' as const,
              timestamp: new Date(msg.timestamp),
              conversationId: msg.conversationId,
            },
            {
              id: `ai-${msg.id}`, // Ensure unique IDs
              content: msg.aiResponse,
              sender: 'ai' as const,
              timestamp: new Date(msg.timestamp), // AI response might have slightly different timestamp in reality
              conversationId: msg.conversationId,
              // Potentially map other fields from aiResponse if they exist in ServiceChatHistoryItem
            },
          ])
          .sort(
            (a: Message, b: Message) =>
              a.timestamp.getTime() - b.timestamp.getTime()
          );
        setMessages(formattedHistory);
      } catch (error) {
        console.error('Failed to load chat history:', error);
      }
    },
    [token]
  );

  useEffect(() => {
    if (token) {
      loadConversations();
    }
  }, [token, loadConversations]); // loadConversations is stable due to useCallback

  useEffect(() => {
    if (token && currentConversationId) {
      loadChatHistory(currentConversationId);
    } else {
      setMessages([]);
    }
  }, [token, currentConversationId, loadChatHistory]); // loadChatHistory is stable

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const startNewConversationLocal = async () => {
    // Renamed to avoid conflict if service has same name
    if (!token) return;
    try {
      // Use chatService
      const responseData: ServiceNewConversationResponse =
        await chatService.startNewConversation();

      if (responseData?.conversationId) {
        // Assuming welcome message is part of the first AI response or handled differently
        const newConversationId = responseData.conversationId.toString();

        setMessages([]);
        setCurrentConversationId(newConversationId);

        // Optionally, add a welcome message from the service if provided
        if (responseData.welcomeMessage) {
          const welcomeAiMessage: Message = {
            id: `welcome-${newConversationId}`,
            content: responseData.welcomeMessage,
            sender: 'ai',
            timestamp: new Date(),
            conversationId: newConversationId,
          };
          setMessages([welcomeAiMessage]);
        }

        setClarificationState({ awaiting: false, originalUserInput: '' });
        setInput('');
        loadConversations(); // Refresh the conversations list
      }
    } catch (error) {
      console.error('Failed to start new conversation:', error);
    }
  };

  const handleConversationsMenuOpen = (
    event: React.MouseEvent<HTMLElement>
  ) => {
    setConversationsMenuAnchor(event.currentTarget);
  };

  const handleConversationsMenuClose = () => {
    setConversationsMenuAnchor(null);
  };

  const selectConversation = (conversationId: string) => {
    setCurrentConversationId(conversationId);
    handleConversationsMenuClose();
  };

  const handleSendMessage = async (
    e: React.FormEvent | null = null,
    promptText: string | null = null
  ) => {
    if (e) e.preventDefault();
    if (!token) return;

    const messageText = promptText || input;
    if (!messageText.trim()) return;

    let messageConversationId = currentConversationId;

    if (!messageConversationId) {
      try {
        // Use chatService
        const newConvoResponse: ServiceNewConversationResponse =
          await chatService.startNewConversation();
        if (newConvoResponse?.conversationId) {
          const newConversationId = newConvoResponse.conversationId.toString();
          setCurrentConversationId(newConversationId);
          messageConversationId = newConversationId;
          // If there's a welcome message, display it
          if (newConvoResponse.welcomeMessage) {
            const welcomeAiMessage: Message = {
              id: `welcome-${newConversationId}`,
              content: newConvoResponse.welcomeMessage,
              sender: 'ai',
              timestamp: new Date(),
              conversationId: newConversationId,
            };
            setMessages([welcomeAiMessage]); // Start with welcome message
          } else {
            setMessages([]); // Clear messages for new conversation if no welcome message
          }
          loadConversations(); // Refresh list
        } else {
          console.error('Failed to get new conversation ID');
          return;
        }
      } catch (error) {
        console.error('Failed to start new conversation:', error);
        setIsLoading(false); // Ensure loading is reset
        return;
      }
    }

    const userMessage: Message = {
      id: Date.now(), // Consider more robust ID generation (e.g., uuid)
      content: messageText,
      sender: 'user',
      timestamp: new Date(),
      conversationId: messageConversationId,
    };

    setMessages((prev) => [...prev, userMessage]);
    setInput('');
    setIsLoading(true);

    try {
      // Use chatService
      // The parameters for chatService.sendMessage are:
      // message, originalUserInput, isClarification, isFollowUp, conversationId
      const serviceResponse: ChatServiceResponse =
        await chatService.sendMessage(
          messageText,
          clarificationState.awaiting
            ? clarificationState.originalUserInput
            : undefined,
          clarificationState.awaiting,
          messages.length > 1, // isFollowUp if more than just the user message exists
          messageConversationId
        );

      // Map serviceResponse to component's ChatResponseDTO expectations
      const responseData: ChatResponseDTO = {
        message: serviceResponse.message,
        recommendedVehicles: serviceResponse.recommendedVehicles,
        parameters: serviceResponse.parameters,
        clarificationNeeded: serviceResponse.clarificationNeeded,
        conversationId: serviceResponse.conversationId || messageConversationId,
        // originalUserInput and matchedCategory might need to be part of ChatServiceResponse if they are expected
      };

      const isConfused =
        responseData.parameters?.intent === 'CONFUSED_FALLBACK';
      const messageContent = isConfused
        ? responseData.message
        : responseData.message;

      if (isConfused)
        console.log('CONFUSED_FALLBACK detected, displaying fallback message.');

      const aiMessage: Message = {
        id: `ai-${Date.now()}`, // Consider more robust ID
        content: messageContent,
        sender: 'ai',
        timestamp: new Date(),
        vehicles: !isConfused ? responseData.recommendedVehicles : undefined,
        parameters: !isConfused ? responseData.parameters : undefined,
        clarificationNeeded: responseData.clarificationNeeded,
        conversationId: responseData.conversationId || messageConversationId,
      };

      setMessages((prev) => [...prev, aiMessage]);

      if (
        responseData.conversationId &&
        responseData.conversationId !== messageConversationId
      ) {
        setCurrentConversationId(responseData.conversationId);
        if (messageConversationId) loadConversations(); // Refresh if ID changed from existing
      }

      if (isConfused || !responseData.clarificationNeeded) {
        setClarificationState({ awaiting: false, originalUserInput: '' });
      } else {
        setClarificationState({
          awaiting: true,
          originalUserInput: clarificationState.awaiting
            ? clarificationState.originalUserInput
            : messageText,
        });
      }

      if (
        !isConfused &&
        onRecommendationsUpdated &&
        responseData.recommendedVehicles
      ) {
        setUpdatingRecommendations(true);
        console.log(
          'Parameters received from backend before calling onRecommendationsUpdated:',
          JSON.stringify(responseData.parameters, null, 2)
        );
        onRecommendationsUpdated(
          responseData.recommendedVehicles,
          responseData.parameters || {}
        );
        setTimeout(() => {
          setUpdatingRecommendations(false);
        }, 1500);
      }
    } catch (error) {
      console.error('Failed to send message:', error);
      const errorMessage: Message = {
        id: `error-${Date.now()}`,
        content:
          'Sorry, I encountered an error processing your request. Please try again.',
        sender: 'ai',
        timestamp: new Date(),
        conversationId: messageConversationId,
      };
      setMessages((prev) => [...prev, errorMessage]);
      setClarificationState({ awaiting: false, originalUserInput: '' });
    } finally {
      setIsLoading(false);
    }
  };

  const handleSuggestedPrompt = (prompt: string) => {
    setInput(prompt); // Set input for visual feedback, though promptText is used
    handleSendMessage(null, prompt);
  };

  // JSX remains largely the same, only the data fetching logic changes
  return (
    <Box
      sx={{
        display: 'flex',
        flexDirection: 'column',
        height: '100%',
        bgcolor: 'background.paper',
      }}
    >
      {/* Recommendations updating indicator */}
      {updatingRecommendations && (
        <Zoom in={updatingRecommendations}>
          <Box
            sx={{
              position: 'absolute',
              top: 0,
              left: 0,
              right: 0,
              zIndex: 10,
              textAlign: 'center',
              py: 0.5,
              bgcolor: 'primary.main',
              color: 'white',
              fontSize: '0.75rem',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              gap: 1,
            }}
          >
            <CircularProgress size={14} thickness={4} sx={{ color: 'white' }} />
            <Typography variant="caption">
              Updating recommendations...
            </Typography>
          </Box>
        </Zoom>
      )}

      {/* Chat header with conversation controls */}
      <Box
        sx={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          p: 1.5,
          borderBottom: 1,
          borderColor: 'divider',
        }}
      >
        <Typography variant="subtitle2" sx={{ fontWeight: 500 }}>
          {currentConversationId
            ? 'Current Conversation'
            : 'Start a Conversation'}
        </Typography>

        <Box sx={{ display: 'flex', gap: 1 }}>
          <Button
            size="small"
            variant="outlined"
            startIcon={<ChatIcon />}
            onClick={handleConversationsMenuOpen}
            sx={{
              fontSize: '0.75rem',
              textTransform: 'none',
              '&:hover': { backgroundColor: 'rgba(25, 118, 210, 0.04)' },
            }}
          >
            {conversations.length} Conversation
            {conversations.length !== 1 ? 's' : ''}
          </Button>

          <Button
            size="small"
            variant="contained"
            startIcon={<AddIcon />}
            onClick={startNewConversationLocal} // Use the renamed local function
            sx={{
              fontSize: '0.75rem',
              textTransform: 'none',
            }}
          >
            New
          </Button>

          {/* Conversations menu */}
          <Menu
            anchorEl={conversationsMenuAnchor}
            open={Boolean(conversationsMenuAnchor)}
            onClose={handleConversationsMenuClose}
            sx={{
              '& .MuiPaper-root': {
                width: 280,
                maxHeight: 400,
                overflow: 'auto',
              },
            }}
          >
            {conversations.map((convo) => (
              <MenuItem
                key={convo.id}
                onClick={() => selectConversation(convo.id.toString())}
                selected={currentConversationId === convo.id.toString()}
                sx={{ py: 1 }}
              >
                <ListItemIcon>
                  <HistoryIcon fontSize="small" />
                </ListItemIcon>
                <ListItemText
                  primary={formatDate(convo.lastInteractionAt)}
                  secondary={`${convo.messageCount} message${convo.messageCount !== 1 ? 's' : ''}`}
                  primaryTypographyProps={{ fontSize: '0.875rem' }}
                  secondaryTypographyProps={{ fontSize: '0.75rem' }}
                />
              </MenuItem>
            ))}
            {conversations.length === 0 && (
              <MenuItem disabled>
                <ListItemText
                  primary="No conversations yet"
                  primaryTypographyProps={{
                    fontSize: '0.875rem',
                    textAlign: 'center',
                  }}
                />
              </MenuItem>
            )}
            <MenuItem onClick={startNewConversationLocal}>
              {' '}
              {/* Use the renamed local function */}
              <ListItemIcon>
                <AddIcon fontSize="small" />
              </ListItemIcon>
              <ListItemText
                primary="Start a new conversation"
                primaryTypographyProps={{ fontSize: '0.875rem' }}
              />
            </MenuItem>
          </Menu>
        </Box>
      </Box>

      <Box
        sx={{
          flexGrow: 1,
          overflowY: 'auto',
          p: 2,
          display: 'flex',
          flexDirection: 'column',
          gap: 1.5,
          '&::-webkit-scrollbar': { width: '6px' },
          '&::-webkit-scrollbar-track': {
            backgroundColor: '#f1f1f1',
            borderRadius: '10px',
          },
          '&::-webkit-scrollbar-thumb': {
            backgroundColor: '#c1c1c1',
            borderRadius: '10px',
          },
          '&::-webkit-scrollbar-thumb:hover': { backgroundColor: '#a8a8a8' },
        }}
      >
        {messages.length === 0 && (
          <Box
            sx={{
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              justifyContent: 'center',
              height: '100%',
              textAlign: 'center',
              color: 'text.secondary',
              py: 2,
            }}
          >
            <Typography
              variant="subtitle2"
              sx={{
                fontWeight: 500,
                mb: 1,
                color: 'primary.main',
                fontSize: '0.95rem',
              }}
            >
              {currentConversationId
                ? 'This conversation is empty. Start chatting!'
                : 'Welcome to Smart Auto Assistant!'}
            </Typography>
            <Typography
              variant="caption"
              sx={{ mb: 2, color: 'text.primary', fontSize: '0.8rem' }}
            >
              Ask me questions or try these:
            </Typography>
            <Box
              sx={{
                display: 'flex',
                flexDirection: 'column',
                gap: 1,
                width: '100%',
              }}
            >
              {suggestedPrompts.map((prompt, index) => (
                <Chip
                  key={index}
                  label={prompt}
                  variant="outlined"
                  color="primary"
                  size="small"
                  onClick={() => handleSuggestedPrompt(prompt)}
                  sx={{ cursor: 'pointer' }}
                />
              ))}
            </Box>
          </Box>
        )}

        {messages.map((message) => (
          <Fade in={true} timeout={300} key={message.id}>
            <Box
              sx={{
                display: 'flex',
                justifyContent:
                  message.sender === 'user' ? 'flex-end' : 'flex-start',
                mb: 0.5,
              }}
            >
              <Paper
                elevation={0}
                sx={{
                  maxWidth: '85%',
                  p: 1.5,
                  backgroundColor:
                    message.sender === 'user' ? '#e3f2fd' : '#f5f5f5',
                  borderRadius: 2,
                }}
              >
                <Typography
                  variant="body2"
                  sx={{ whiteSpace: 'pre-wrap', fontSize: '0.875rem' }}
                >
                  {message.content}
                </Typography>
                {message.vehicles && message.vehicles.length > 0 && (
                  <Box sx={{ mt: 1, display: 'flex', alignItems: 'center' }}>
                    <Typography
                      variant="caption"
                      sx={{ fontWeight: 500, color: 'primary.main' }}
                    >
                      Found {message.vehicles.length} matching vehicles
                    </Typography>
                    <Tooltip title="Results shown in main content area">
                      <Box
                        component="span"
                        sx={{
                          display: 'inline-flex',
                          alignItems: 'center',
                          ml: 0.5,
                          color: 'primary.main',
                          cursor: 'help',
                          fontSize: '0.75rem',
                        }}
                      >
                        ⓘ
                      </Box>
                    </Tooltip>
                  </Box>
                )}
                {message.clarificationNeeded && message.sender === 'ai' && (
                  <>
                    {message.parameters?.matchedCategory && (
                      <Typography
                        variant="caption"
                        sx={{
                          display: 'block',
                          color: 'primary.main',
                          fontWeight: 600,
                          mt: 1,
                        }}
                      >
                        Sounds like you might be interested in:{' '}
                        {message.parameters.matchedCategory}
                      </Typography>
                    )}
                    <Typography
                      variant="caption"
                      sx={{ display: 'block', color: 'primary.main', mt: 0.5 }}
                    >
                      Could you let me know your budget, preferred fuel type, or
                      how new you'd like the car to be?
                    </Typography>
                  </>
                )}
                <Typography
                  variant="caption"
                  sx={{
                    display: 'block',
                    mt: 0.5,
                    color: 'text.secondary',
                    fontSize: '0.7rem',
                  }}
                >
                  {message.timestamp.toLocaleTimeString([], {
                    hour: '2-digit',
                    minute: '2-digit',
                  })}
                </Typography>
              </Paper>
            </Box>
          </Fade>
        ))}

        {isLoading && (
          <Box sx={{ display: 'flex', justifyContent: 'flex-start', mb: 1 }}>
            <Paper
              elevation={0}
              sx={{
                p: 1.5,
                backgroundColor: '#f5f5f5',
                borderRadius: 2,
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'center',
              }}
            >
              <Box sx={{ display: 'flex', justifyContent: 'center', gap: 0.5 }}>
                <CircularProgress size={8} sx={{ color: 'primary.main' }} />
                <CircularProgress size={8} sx={{ color: 'primary.main' }} />
                <CircularProgress size={8} sx={{ color: 'primary.main' }} />
              </Box>
              <Typography
                variant="caption"
                sx={{ color: 'text.secondary', mt: 0.5, fontSize: '0.75rem' }}
              >
                Thinking...
              </Typography>
            </Paper>
          </Box>
        )}
        <div ref={messagesEndRef} />
      </Box>

      <Box
        component="form"
        onSubmit={handleSendMessage}
        sx={{ p: 1.5, borderTop: 1, borderColor: 'divider' }}
      >
        <TextField
          fullWidth
          value={input}
          onChange={(e) => setInput(e.target.value)}
          placeholder={
            clarificationState.awaiting
              ? 'Please provide more details...'
              : 'Ask about cars...'
          }
          disabled={isLoading}
          size="small"
          variant="outlined"
          InputProps={{
            endAdornment: (
              <InputAdornment position="end">
                <IconButton
                  edge="end"
                  color="primary"
                  disabled={isLoading || !input.trim()}
                  type="submit"
                  aria-label="Send message" // Add aria-label here
                >
                  <SendIcon />
                </IconButton>
              </InputAdornment>
            ),
          }}
          sx={{ mb: clarificationState.awaiting ? 0.5 : 0 }}
        />
        {clarificationState.awaiting && (
          <Typography
            variant="caption"
            sx={{ display: 'block', color: 'primary.main', mt: 0.5 }}
          >
            I'm asking follow-up questions to better understand your needs
          </Typography>
        )}
      </Box>
    </Box>
  );
};

export default ChatInterface;
