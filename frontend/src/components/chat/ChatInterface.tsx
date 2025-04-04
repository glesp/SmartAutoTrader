import { useState, useRef, useEffect, useContext } from 'react'
import axios from 'axios'
import { AuthContext } from '../../contexts/AuthContext'
// Import Material-UI components
import Box from '@mui/material/Box'
import TextField from '@mui/material/TextField'
import Typography from '@mui/material/Typography'
import Paper from '@mui/material/Paper'
import CircularProgress from '@mui/material/CircularProgress'
import SendIcon from '@mui/icons-material/Send'
import InputAdornment from '@mui/material/InputAdornment'
import IconButton from '@mui/material/IconButton'
import Tooltip from '@mui/material/Tooltip'
import Chip from '@mui/material/Chip'
import Button from '@mui/material/Button'
import Menu from '@mui/material/Menu'
import MenuItem from '@mui/material/MenuItem'
import ListItemText from '@mui/material/ListItemText'
import ListItemIcon from '@mui/material/ListItemIcon'
import HistoryIcon from '@mui/icons-material/History'
import ChatIcon from '@mui/icons-material/Chat'
import AddIcon from '@mui/icons-material/Add'
import Zoom from '@mui/material/Zoom'

// Type definitions
interface ChatInterfaceProps {
  onRecommendationsUpdated: (
    vehicles: Vehicle[],
    parameters: RecommendationParameters
  ) => void
}

interface Message {
  id: string | number
  content: string
  sender: 'user' | 'ai'
  timestamp: Date
  vehicles?: Vehicle[]
  parameters?: RecommendationParameters
  clarificationNeeded?: boolean
  originalUserInput?: string
  conversationId?: string
}

interface Vehicle {
  id: number
  make: string
  model: string
  year: number
  price: number
  mileage: number
  fuelType: number
  vehicleType: number
  images?: VehicleImage[] | { $values: VehicleImage[] }
}

interface VehicleImage {
  id: number
  imageUrl: string
  isPrimary: boolean
}

interface RecommendationParameters {
  minPrice?: number
  maxPrice?: number
  minYear?: number
  maxYear?: number
  maxMileage?: number
  preferredMakes?: string[]
  preferredVehicleTypes?: number[] | string[]
  preferredFuelTypes?: number[] | string[]
  desiredFeatures?: string[]
}

interface ChatHistoryItem {
  id: number
  userMessage: string
  aiResponse: string
  timestamp: string
  conversationId?: string
}

interface ChatResponseDTO {
  message: string
  recommendedVehicles?: Vehicle[]
  parameters?: RecommendationParameters
  clarificationNeeded?: boolean
  originalUserInput?: string
  conversationId?: string
  matchedCategory?: string
}

interface Conversation {
  id: number
  createdAt: string
  lastInteractionAt: string
  messageCount: number
}

// Helper function to extract arrays from ASP.NET response format
const extractArray = <T,>(data: T[] | { $values: T[] } | undefined): T[] => {
  if (!data) return []
  if (Array.isArray(data)) return data
  if (data && '$values' in data) return data.$values
  return []
}

// Helper function for date formatting
const formatDate = (dateString: string): string => {
  try {
    const date = new Date(dateString)
    return date.toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
      hour12: true,
    })
  } catch (error) {
    return dateString
  }
}

const ChatInterface = ({ onRecommendationsUpdated }: ChatInterfaceProps) => {
  const [messages, setMessages] = useState<Message[]>([])
  const [input, setInput] = useState<string>('')
  const [isLoading, setIsLoading] = useState<boolean>(false)
  const [updatingRecommendations, setUpdatingRecommendations] =
    useState<boolean>(false)
  const [clarificationState, setClarificationState] = useState<{
    awaiting: boolean
    originalUserInput: string
  }>({ awaiting: false, originalUserInput: '' })
  const [currentConversationId, setCurrentConversationId] = useState<
    string | undefined
  >(undefined)
  const [conversations, setConversations] = useState<Conversation[]>([])
  const [conversationsMenuAnchor, setConversationsMenuAnchor] =
    useState<null | HTMLElement>(null)
  const messagesEndRef = useRef<HTMLDivElement>(null)
  const { token } = useContext(AuthContext)

  // Suggested prompts
  const suggestedPrompts = [
    'I need an SUV under €30,000',
    'Show me electric cars with good range',
    'Family cars with low mileage',
  ]

  // Load conversations on component mount
  useEffect(() => {
    if (token) {
      loadConversations()
    }
  }, [token])

  // Load messages when conversation changes
  useEffect(() => {
    if (token && currentConversationId) {
      loadChatHistory(currentConversationId)
    } else {
      // Clear messages if no conversation is selected
      setMessages([])
    }
  }, [token, currentConversationId])

  // Scroll to bottom of messages when messages update
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages])

  // Load available conversations
  const loadConversations = async () => {
    try {
      const response = await axios.get('/api/chat/conversations', {
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
      })

      if (response.data) {
        const conversationsData = Array.isArray(response.data)
          ? response.data
          : response.data.$values || []

        setConversations(conversationsData)

        // If we have conversations but none selected, select the most recent one
        if (conversationsData.length > 0 && !currentConversationId) {
          setCurrentConversationId(conversationsData[0].id.toString())
        }
      }
    } catch (error) {
      console.error('Failed to load conversations:', error)
    }
  }

  // Load chat history for a specific conversation
  const loadChatHistory = async (conversationId: string) => {
    try {
      const response = await axios.get(
        `/api/chat/history?conversationId=${conversationId}`,
        {
          headers: {
            Authorization: `Bearer ${token}`,
            'Content-Type': 'application/json',
          },
        }
      )

      if (response.data) {
        const historyData = Array.isArray(response.data)
          ? response.data
          : response.data.$values || []

        const formattedHistory = historyData
          .map((msg: ChatHistoryItem) => ({
            id: msg.id,
            content: msg.userMessage,
            sender: 'user' as const,
            timestamp: new Date(msg.timestamp),
            conversationId: msg.conversationId,
          }))
          .concat(
            historyData.map((msg: ChatHistoryItem) => ({
              id: `ai-${msg.id}`,
              content: msg.aiResponse,
              sender: 'ai' as const,
              timestamp: new Date(msg.timestamp),
              conversationId: msg.conversationId,
            }))
          )
          .sort((a, b) => a.timestamp.getTime() - b.timestamp.getTime())

        setMessages(formattedHistory)
      }
    } catch (error) {
      console.error('Failed to load chat history:', error)
    }
  }

  // Start a new conversation
  const startNewConversation = async () => {
    try {
      const response = await axios.post(
        '/api/chat/conversation/new',
        {},
        {
          headers: {
            Authorization: `Bearer ${token}`,
            'Content-Type': 'application/json',
          },
        }
      )

      if (response.data?.conversationId) {
        setCurrentConversationId(response.data.conversationId.toString())
        setMessages([])

        // Reset any ongoing clarification
        setClarificationState({
          awaiting: false,
          originalUserInput: '',
        })

        // Refresh the conversations list
        loadConversations()
      }
    } catch (error) {
      console.error('Failed to start new conversation:', error)
    }
  }

  // Open conversations menu
  const handleConversationsMenuOpen = (
    event: React.MouseEvent<HTMLElement>
  ) => {
    setConversationsMenuAnchor(event.currentTarget)
  }

  // Close conversations menu
  const handleConversationsMenuClose = () => {
    setConversationsMenuAnchor(null)
  }

  // Select a conversation
  const selectConversation = (conversationId: string) => {
    setCurrentConversationId(conversationId)
    handleConversationsMenuClose()
  }

  const handleSendMessage = async (
    e: React.FormEvent | null = null,
    promptText: string | null = null
  ) => {
    if (e) e.preventDefault()

    const messageText = promptText || input
    if (!messageText.trim()) return

    // If no conversation is selected, start a new one first
    if (!currentConversationId) {
      try {
        const response = await axios.post(
          '/api/chat/conversation/new',
          {},
          {
            headers: {
              Authorization: `Bearer ${token}`,
              'Content-Type': 'application/json',
            },
          }
        )

        if (response.data?.conversationId) {
          setCurrentConversationId(response.data.conversationId.toString())
        }
      } catch (error) {
        console.error('Failed to start new conversation:', error)
        return
      }
    }

    const userMessage: Message = {
      id: Date.now(),
      content: messageText,
      sender: 'user',
      timestamp: new Date(),
      conversationId: currentConversationId,
    }

    setMessages((prev) => [...prev, userMessage])
    setInput('')
    setIsLoading(true)

    try {
      // Prepare the message payload with conversation context
      const payload = {
        content: messageText,
        originalUserInput: clarificationState.awaiting
          ? clarificationState.originalUserInput
          : undefined,
        isClarification: clarificationState.awaiting,
        isFollowUp: messages.length > 0,
        conversationId: currentConversationId,
      }

      const response = await axios.post('/api/chat/message', payload, {
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
      })

      if (response.data) {
        const responseData: ChatResponseDTO = response.data

        // Create AI message with response data
        const aiMessage: Message = {
          id: `ai-${Date.now()}`,
          content: responseData.message,
          sender: 'ai',
          timestamp: new Date(),
          vehicles: responseData.recommendedVehicles,
          parameters: responseData.parameters,
          clarificationNeeded: responseData.clarificationNeeded,
          conversationId: responseData.conversationId || currentConversationId,
        }

        setMessages((prev) => [...prev, aiMessage])

        // If we got a new conversation ID, update it
        if (
          responseData.conversationId &&
          responseData.conversationId !== currentConversationId
        ) {
          setCurrentConversationId(responseData.conversationId)
          loadConversations() // Refresh the list
        }

        // If clarification is needed, update the clarification state
        if (responseData.clarificationNeeded) {
          setClarificationState({
            awaiting: true,
            originalUserInput: clarificationState.awaiting
              ? clarificationState.originalUserInput
              : messageText,
          })
        } else {
          // If we get a final answer, reset clarification state
          setClarificationState({
            awaiting: false,
            originalUserInput: '',
          })

          // Update recommendations in parent component if available
          if (onRecommendationsUpdated && responseData.recommendedVehicles) {
            // Show updating indicator
            setUpdatingRecommendations(true)

            // Update recommendations
            onRecommendationsUpdated(
              responseData.recommendedVehicles,
              responseData.parameters || {}
            )

            // Hide indicator after a delay
            setTimeout(() => {
              setUpdatingRecommendations(false)
            }, 1500)
          }
        }
      }
    } catch (error) {
      console.error('Failed to send message:', error)

      const errorMessage: Message = {
        id: `error-${Date.now()}`,
        content:
          'Sorry, I encountered an error processing your request. Please try again.',
        sender: 'ai',
        timestamp: new Date(),
        conversationId: currentConversationId,
      }

      setMessages((prev) => [...prev, errorMessage])

      // Reset clarification state on error
      setClarificationState({
        awaiting: false,
        originalUserInput: '',
      })
    } finally {
      setIsLoading(false)
    }
  }

  // Handle clicking a suggested prompt
  const handleSuggestedPrompt = (prompt: string) => {
    setInput(prompt)
    handleSendMessage(null, prompt)
  }

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
            onClick={startNewConversation}
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
            <MenuItem onClick={startNewConversation}>
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
          '&::-webkit-scrollbar': {
            width: '6px',
          },
          '&::-webkit-scrollbar-track': {
            backgroundColor: '#f1f1f1',
            borderRadius: '10px',
          },
          '&::-webkit-scrollbar-thumb': {
            backgroundColor: '#c1c1c1',
            borderRadius: '10px',
          },
          '&::-webkit-scrollbar-thumb:hover': {
            backgroundColor: '#a8a8a8',
          },
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
            <Typography variant="subtitle2" sx={{ fontWeight: 500, mb: 1 }}>
              {currentConversationId
                ? 'This conversation is empty. Start chatting!'
                : 'Welcome to Smart Auto Assistant!'}
            </Typography>

            <Typography variant="caption" sx={{ mb: 2 }}>
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
          <Box
            key={message.id}
            sx={{
              display: 'flex',
              justifyContent:
                message.sender === 'user' ? 'flex-end' : 'flex-start',
              mb: 0.5,
              animation: 'fadeIn 0.3s ease-out forwards',
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
              {/* show vehicles in the chat interface */}
              {/* {message.vehicles && message.vehicles.length > 0 && (
                <Box sx={{ mt: 1, display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                  {message.vehicles.slice(0, 3).map((vehicle) => {
                    const vehicleImages = extractArray(vehicle.images)
                    const primaryImage = vehicleImages.find(
                      (img) => img.isPrimary
                    )

                    return (
                      <Box
                        key={vehicle.id}
                        sx={{
                          display: 'flex',
                          flexDirection: 'column',
                          borderRadius: 1,
                          overflow: 'hidden',
                          border: '1px solid #eee',
                          width: 80,
                        }}
                      >
                        {primaryImage ? (
                          <Box
                            component="img"
                            src={primaryImage.imageUrl}
                            alt={`${vehicle.make} ${vehicle.model}`}
                            sx={{
                              width: '100%',
                              height: 50,
                              objectFit: 'cover',
                            }}
                          />
                        ) : (
                          <Box
                            sx={{
                              width: '100%',
                              height: 50,
                              bgcolor: '#f0f0f0',
                              display: 'flex',
                              alignItems: 'center',
                              justifyContent: 'center',
                            }}
                          >
                            <Typography
                              variant="caption"
                              sx={{ color: '#999' }}
                            >
                              No image
                            </Typography>
                          </Box>
                        )}
                        <Typography
                          variant="caption"
                          sx={{
                            fontSize: '0.7rem',
                            p: 0.5,
                            textAlign: 'center',
                            whiteSpace: 'nowrap',
                            overflow: 'hidden',
                            textOverflow: 'ellipsis',
                          }}
                        >
                          {vehicle.make} {vehicle.model}
                        </Typography>
                      </Box>
                    )
                  })}
                </Box>
              )} */}

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
        sx={{
          p: 1.5,
          borderTop: 1,
          borderColor: 'divider',
        }}
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
            sx={{
              display: 'block',
              color: 'primary.main',
              mt: 0.5,
            }}
          >
            I'm asking follow-up questions to better understand your needs
          </Typography>
        )}
      </Box>
    </Box>
  )
}

export default ChatInterface
