import { useState, useRef, useEffect, useContext } from 'react'
import axios from 'axios'
import { AuthContext } from '../../contexts/AuthContext'
// Import Material-UI components
import Box from '@mui/material/Box'
import TextField from '@mui/material/TextField'
import Button from '@mui/material/Button'
import Typography from '@mui/material/Typography'
import Paper from '@mui/material/Paper'
import CircularProgress from '@mui/material/CircularProgress'
import SendIcon from '@mui/icons-material/Send'
import InputAdornment from '@mui/material/InputAdornment'
import IconButton from '@mui/material/IconButton'

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
  preferredVehicleTypes?: number[]
  preferredFuelTypes?: number[]
  desiredFeatures?: string[]
}

interface ChatHistoryItem {
  id: number
  userMessage: string
  aiResponse: string
  timestamp: string
}

interface ChatResponseDTO {
  message: string
  recommendedVehicles?: Vehicle[]
  parameters?: RecommendationParameters
  clarificationNeeded?: boolean
  originalUserInput?: string
}

// Helper function to extract arrays from ASP.NET response format
const extractArray = <T,>(data: T[] | { $values: T[] } | undefined): T[] => {
  if (!data) return []
  if (Array.isArray(data)) return data
  if (data && '$values' in data) return data.$values
  return []
}

// Helper function to format currency
const formatCurrency = (value: number) => {
  return new Intl.NumberFormat('en-IE', {
    style: 'currency',
    currency: 'EUR',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(value)
}

// Helper function to get the image URL
const getImageUrl = (vehicle: Vehicle) => {
  const images = extractArray(vehicle.images)
  if (images.length === 0) return 'https://via.placeholder.com/200x150'

  const primaryImage = images.find((img) => img.isPrimary)
  return (
    primaryImage?.imageUrl ||
    images[0].imageUrl ||
    'https://via.placeholder.com/200x150'
  )
}

// Helper function to convert enum value to string
const getFuelTypeString = (fuelType: number): string => {
  const fuelTypes = [
    'Petrol',
    'Diesel',
    'Electric',
    'Hybrid',
    'Plugin',
    'Hydrogen',
  ]
  return fuelTypes[fuelType] || 'Unknown'
}

const getVehicleTypeString = (vehicleType: number): string => {
  const vehicleTypes = [
    'Hatchback',
    'Sedan',
    'SUV',
    'Coupe',
    'Convertible',
    'Wagon',
    'Pickup',
    'Minivan',
  ]
  return vehicleTypes[vehicleType] || 'Unknown'
}

const ChatInterface = ({ 
  onRecommendationsUpdated,
}: ChatInterfaceProps) => {
  const [messages, setMessages] = useState<Message[]>([])
  const [input, setInput] = useState<string>('')
  const [isLoading, setIsLoading] = useState<boolean>(false)
  const [clarificationState, setClarificationState] = useState<{
    awaiting: boolean
    originalUserInput: string
  }>({ awaiting: false, originalUserInput: '' })
  const messagesEndRef = useRef<HTMLDivElement>(null)
  const { token } = useContext(AuthContext)

  // Load chat history on component mount
  useEffect(() => {
    const loadChatHistory = async () => {
      try {
        const response = await axios.get('/api/chat/history', {
          headers: {
            Authorization: `Bearer ${token}`,
            'Content-Type': 'application/json',
          },
        })

        if (response.data) {
          const historyData = Array.isArray(response.data)
            ? response.data
            : response.data.$values || []

          // Limit to last 5 history items to prevent UI overflow
          const recentHistory = historyData.slice(-5)

          const formattedHistory = recentHistory
            .map((msg: ChatHistoryItem) => ({
              id: msg.id,
              content: msg.userMessage,
              sender: 'user' as const,
              timestamp: new Date(msg.timestamp),
            }))
            .concat(
              recentHistory.map((msg: ChatHistoryItem) => ({
                id: `ai-${msg.id}`,
                content: msg.aiResponse,
                sender: 'ai' as const,
                timestamp: new Date(msg.timestamp),
              }))
            )
            .sort((a, b) => a.timestamp.getTime() - b.timestamp.getTime())

          setMessages(formattedHistory)
        }
      } catch (error) {
        console.error('Failed to load chat history:', error)
      }
    }

    if (token) {
      loadChatHistory()
    }
  }, [token])

  // Scroll to bottom of messages when messages update
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages])

  const handleSendMessage = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!input.trim()) return

    const userMessage: Message = {
      id: Date.now(),
      content: input,
      sender: 'user',
      timestamp: new Date(),
    }

    setMessages((prev) => [...prev, userMessage])
    setInput('')
    setIsLoading(true)

    try {
      // Prepare the message payload based on whether we're in clarification mode
      const payload = clarificationState.awaiting
        ? {
            content: input,
            originalUserInput: clarificationState.originalUserInput,
            isClarification: true,
          }
        : { content: input }

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
        }

        setMessages((prev) => [...prev, aiMessage])

        console.log('📦 Chat response parameters:', responseData.parameters)


        // If clarification is needed, update the clarification state
        if (responseData.clarificationNeeded) {
          setClarificationState({
            awaiting: true,
            originalUserInput: clarificationState.awaiting
              ? clarificationState.originalUserInput
              : input,
          })
        } else {
          // If we get a final answer, reset clarification state
          setClarificationState({
            awaiting: false,
            originalUserInput: '',
          })

          // Update recommendations in parent component if available
          if (onRecommendationsUpdated && responseData.recommendedVehicles) {
            onRecommendationsUpdated(
              responseData.recommendedVehicles,
              responseData.parameters || {}
            )
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

  return (
    <Box sx={{ 
      display: 'flex', 
      flexDirection: 'column', 
      height: '100%',
      bgcolor: 'background.paper'
    }}>
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
              py: 4
            }}
          >
            <Typography variant="subtitle1" sx={{ fontWeight: 500 }}>
              Welcome to Smart Auto Assistant!
            </Typography>
            <Typography variant="body2" sx={{ mt: 1 }}>
              Ask me questions like:
            </Typography>
            <Box component="ul" sx={{ mt: 0.5, textAlign: 'left', p: 0 }}>
              <Typography component="li" variant="body2" sx={{ mb: 0.5 }}>
                "I'm looking for an SUV under €30,000"
              </Typography>
              <Typography component="li" variant="body2" sx={{ mb: 0.5 }}>
                "Show me electric cars with good range"
              </Typography>
              <Typography component="li" variant="body2" sx={{ mb: 0.5 }}>
                "What family cars would you recommend?"
              </Typography>
            </Box>
          </Box>
        )}

        {messages.map((message) => (
          <Box
            key={message.id}
            sx={{
              display: 'flex',
              justifyContent: message.sender === 'user' ? 'flex-end' : 'flex-start',
              mb: 1,
              animation: 'fadeIn 0.3s ease-out forwards'
            }}
          >
            <Paper
              elevation={0}
              sx={{
                maxWidth: '75%',
                p: 1.5,
                backgroundColor: message.sender === 'user' ? '#e3f2fd' : '#f5f5f5',
                borderRadius: 2
              }}
            >
              <Typography 
                variant="body2" 
                sx={{ whiteSpace: 'pre-wrap' }}
              >
                {message.content}
              </Typography>

              {message.vehicles && message.vehicles.length > 0 && (
                <Box sx={{ mt: 1.5, display: 'flex', flexDirection: 'column', gap: 1 }}>
                  <Typography variant="caption" sx={{ fontWeight: 500, color: 'primary.main' }}>
                    Here are some recommendations:
                  </Typography>
                  <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                    {message.vehicles.slice(0, 2).map((vehicle) => (
                      <Paper
                        key={vehicle.id}
                        elevation={1}
                        sx={{ p: 1, display: 'flex', alignItems: 'center' }}
                      >
                        <Box
                          component="img"
                          src={getImageUrl(vehicle)}
                          alt={`${vehicle.year} ${vehicle.make} ${vehicle.model}`}
                          sx={{ width: 40, height: 40, borderRadius: 1, objectFit: 'cover' }}
                        />
                        <Box sx={{ ml: 1, flex: 1 }}>
                          <Typography variant="caption" sx={{ fontWeight: 500 }}>
                            {vehicle.year} {vehicle.make} {vehicle.model}
                          </Typography>
                          <Typography variant="caption" sx={{ display: 'block', color: 'text.secondary' }}>
                            {formatCurrency(vehicle.price)} · {getFuelTypeString(vehicle.fuelType)} · {getVehicleTypeString(vehicle.vehicleType)}
                          </Typography>
                        </Box>
                      </Paper>
                    ))}
                    {message.vehicles.length > 2 && (
                      <Typography 
                        variant="caption" 
                        sx={{ 
                          textAlign: 'center', 
                          color: 'primary.main'
                        }}
                      >
                        + {message.vehicles.length - 2} more recommendations
                      </Typography>
                    )}
                  </Box>
                </Box>
              )}

              {message.clarificationNeeded && message.sender === 'ai' && (
                <Typography 
                  variant="caption" 
                  sx={{ 
                    display: 'block', 
                    mt: 1,
                    fontWeight: 500,
                    color: 'primary.main'
                  }}
                >
                  I need more information to help you find the perfect vehicle.
                </Typography>
              )}

              <Typography 
                variant="caption" 
                sx={{ 
                  display: 'block', 
                  mt: 0.5,
                  color: 'text.secondary'
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
                p: 2,
                backgroundColor: '#f5f5f5',
                borderRadius: 2,
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'center'
              }}
            >
              <Box sx={{ display: 'flex', justifyContent: 'center', gap: 1 }}>
                <CircularProgress size={8} sx={{ color: 'primary.main' }} />
                <CircularProgress size={8} sx={{ color: 'primary.main' }} />
                <CircularProgress size={8} sx={{ color: 'primary.main' }} />
              </Box>
              <Typography variant="caption" sx={{ color: 'text.secondary', mt: 0.5 }}>
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
          borderColor: 'divider' 
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
              mt: 0.5
            }}
          >
            I'm asking follow-up questions to better understand your needs
          </Typography>
        )}
      </Box>
    </Box>
  );
}

export default ChatInterface