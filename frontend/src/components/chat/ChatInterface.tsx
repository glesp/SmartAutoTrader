import { useState, useRef, useEffect, useContext } from 'react'
import axios from 'axios'
import { AuthContext } from '../../contexts/AuthContext'

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

const ChatInterface = ({ onRecommendationsUpdated }: ChatInterfaceProps) => {
  const [messages, setMessages] = useState<Message[]>([])
  const [input, setInput] = useState<string>('')
  const [isLoading, setIsLoading] = useState<boolean>(false)
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

          const formattedHistory = historyData
            .map((msg: ChatHistoryItem) => ({
              id: msg.id,
              content: msg.userMessage,
              sender: 'user' as const,
              timestamp: new Date(msg.timestamp),
            }))
            .concat(
              historyData.map((msg: ChatHistoryItem) => ({
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
      const response = await axios.post(
        '/api/chat/message',
        { content: input },
        {
          headers: {
            Authorization: `Bearer ${token}`,
            'Content-Type': 'application/json',
          },
        }
      )

      if (response.data) {
        const aiMessage: Message = {
          id: `ai-${Date.now()}`,
          content: response.data.message,
          sender: 'ai',
          timestamp: new Date(),
          vehicles: response.data.recommendedVehicles,
          parameters: response.data.parameters,
        }

        setMessages((prev) => [...prev, aiMessage])

        // Update recommendations in parent component
        if (onRecommendationsUpdated && response.data.recommendedVehicles) {
          onRecommendationsUpdated(
            response.data.recommendedVehicles,
            response.data.parameters
          )
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
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div className="flex flex-col h-full bg-white rounded-lg shadow-lg">
      <div className="p-4 bg-blue-600 text-white rounded-t-lg">
        <h2 className="text-xl font-semibold">Smart Auto Assistant</h2>
        <p className="text-sm text-blue-100">
          Ask me about cars and I'll help you find the perfect match!
        </p>
      </div>

      <div className="flex-1 overflow-y-auto p-4 space-y-4">
        {messages.length === 0 && (
          <div className="flex items-center justify-center h-full">
            <div className="text-center text-gray-500">
              <p className="font-medium">Welcome to Smart Auto Assistant!</p>
              <p className="text-sm mt-2">Ask me questions like:</p>
              <ul className="text-sm mt-1 space-y-1">
                <li>"I'm looking for an SUV under €30,000"</li>
                <li>"Show me electric cars with good range"</li>
                <li>"What family cars would you recommend?"</li>
              </ul>
            </div>
          </div>
        )}

        {messages.map((message) => (
          <div
            key={message.id}
            className={`flex ${message.sender === 'user' ? 'justify-end' : 'justify-start'}`}
          >
            <div
              className={`max-w-3/4 rounded-lg p-3 ${
                message.sender === 'user'
                  ? 'bg-blue-100 text-blue-900'
                  : 'bg-gray-100 text-gray-900'
              }`}
            >
              <p className="whitespace-pre-wrap">{message.content}</p>

              {message.vehicles && message.vehicles.length > 0 && (
                <div className="mt-3 space-y-2">
                  <p className="font-medium text-sm text-blue-700">
                    Here are some recommendations:
                  </p>
                  <div className="grid grid-cols-1 gap-2">
                    {message.vehicles.slice(0, 2).map((vehicle) => (
                      <div
                        key={vehicle.id}
                        className="flex items-center p-2 bg-white rounded-md shadow-sm"
                      >
                        <img
                          src={getImageUrl(vehicle)}
                          alt={`${vehicle.year} ${vehicle.make} ${vehicle.model}`}
                          className="w-12 h-12 object-cover rounded-md"
                        />
                        <div className="ml-2 flex-1">
                          <p className="font-medium text-sm">
                            {vehicle.year} {vehicle.make} {vehicle.model}
                          </p>
                          <p className="text-xs text-gray-600">
                            {formatCurrency(vehicle.price)} ·{' '}
                            {getFuelTypeString(vehicle.fuelType)} ·{' '}
                            {getVehicleTypeString(vehicle.vehicleType)}
                          </p>
                        </div>
                      </div>
                    ))}
                    {message.vehicles.length > 2 && (
                      <p className="text-xs text-blue-600 text-center">
                        + {message.vehicles.length - 2} more recommendations
                      </p>
                    )}
                  </div>
                </div>
              )}

              <p className="text-xs text-gray-500 mt-1">
                {message.timestamp.toLocaleTimeString([], {
                  hour: '2-digit',
                  minute: '2-digit',
                })}
              </p>
            </div>
          </div>
        ))}

        {isLoading && (
          <div className="flex justify-start">
            <div className="bg-gray-100 rounded-lg p-4">
              <div className="animate-pulse flex space-x-2 justify-center">
                <div className="rounded-full bg-blue-400 h-2 w-2"></div>
                <div className="rounded-full bg-blue-400 h-2 w-2"></div>
                <div className="rounded-full bg-blue-400 h-2 w-2"></div>
              </div>
              <p className="text-xs text-gray-500 mt-1">Thinking...</p>
            </div>
          </div>
        )}

        <div ref={messagesEndRef} />
      </div>

      <form onSubmit={handleSendMessage} className="p-4 border-t">
        <div className="flex">
          <input
            type="text"
            value={input}
            onChange={(e) => setInput(e.target.value)}
            placeholder="Ask about cars, features, or preferences..."
            className="flex-1 p-2 border rounded-l-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
            disabled={isLoading}
          />
          <button
            type="submit"
            className="bg-blue-600 text-white px-4 py-2 rounded-r-lg hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:opacity-50"
            disabled={isLoading || !input.trim()}
          >
            Send
          </button>
        </div>
      </form>
    </div>
  )
}

export default ChatInterface
