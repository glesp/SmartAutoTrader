import { useContext, useState, useEffect } from 'react'
import { Navigate } from 'react-router-dom'
import { AuthContext } from '../contexts/AuthContext'
import VehicleRecommendations from '../components/vehicles/VehicleRecommendations'
import ChatInterface from '../components/chat/ChatInterface'
// Import Material-UI components
import MinimizeIcon from '@mui/icons-material/Minimize'
import Paper from '@mui/material/Paper'
import Typography from '@mui/material/Typography'
import Box from '@mui/material/Box'
import IconButton from '@mui/material/IconButton'
import Slide from '@mui/material/Slide'
import Badge from '@mui/material/Badge'
import { Vehicle } from '../types/models'

interface RecommendationParameters {
  minPrice?: number
  maxPrice?: number
  minYear?: number
  maxYear?: number
  preferredMakes?: string[]
  preferredVehicleTypes?: string[] | number[]
  preferredFuelTypes?: string[] | number[]
  desiredFeatures?: string[]
}

const RecommendationsPage = () => {
  const { isAuthenticated, loading } = useContext(AuthContext)
  const [activeTab, setActiveTab] = useState<'recommendations' | 'assistant'>(
    'recommendations'
  )
  const [recommendedVehicles, setRecommendedVehicles] = useState<Vehicle[]>([])
  const [parameters, setParameters] = useState<RecommendationParameters>({})
  const [isChatMinimized, setIsChatMinimized] = useState(true)
  const [newRecommendationsFlag, setNewRecommendationsFlag] = useState(false)
  const [showChatBadge, setShowChatBadge] = useState(false)

  // Show loading state while checking authentication
  if (loading) {
    return <div className="text-center py-8">Loading...</div>
  }

  // Redirect to login if not authenticated
  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: '/recommendations' }} />
  }

  // Handle recommendations update from chat
  const handleRecommendationsUpdate = (
    vehicles: Vehicle[],
    newParams: RecommendationParameters
  ) => {
    console.log('Received recommendations:', vehicles.length, 'vehicles')
    setRecommendedVehicles(vehicles)
    setParameters(newParams)

    // Set flag for animation
    setNewRecommendationsFlag(true)

    // Show badge on minimized chat to indicate new recommendations
    if (isChatMinimized) {
      setShowChatBadge(true)
    }

    // Reset flag after animation completes
    setTimeout(() => {
      setNewRecommendationsFlag(false)
    }, 2000)
  }

  // Toggle chat minimized state
  const toggleChat = () => {
    setIsChatMinimized(!isChatMinimized)
    if (!isChatMinimized) {
      // When minimizing, clear any badge notification
      setShowChatBadge(false)
    }
  }

  return (
    <div className="container mx-auto px-4 pb-20">
      <div className="mb-6">
        <h1 className="text-3xl font-bold">Personalized Recommendations</h1>
        <p className="text-gray-600 mt-2">
          Our AI analyzes your browsing history and preferences to recommend
          vehicles you might like.
        </p>
      </div>

      {/* Tabs Navigation */}
      <div className="mb-6 border-b border-gray-200">
        <ul className="flex flex-wrap -mb-px">
          <li className="mr-2">
            <button
              className={`inline-block py-4 px-4 text-sm font-medium ${
                activeTab === 'recommendations'
                  ? 'text-blue-600 border-b-2 border-blue-600'
                  : 'text-gray-500 hover:text-gray-700'
              }`}
              onClick={() => setActiveTab('recommendations')}
            >
              Recommendations
            </button>
          </li>
          <li className="mr-2">
            <button
              className={`inline-block py-4 px-4 text-sm font-medium ${
                activeTab === 'assistant'
                  ? 'text-blue-600 border-b-2 border-blue-600'
                  : 'text-gray-500 hover:text-gray-700'
              }`}
              onClick={() => setActiveTab('assistant')}
            >
              AI Assistant
            </button>
          </li>
        </ul>
      </div>

      {/* Main Content Area - with recommendation highlight effect */}
      <div
        className={`mb-8 transition-all duration-500 ${newRecommendationsFlag ? 'bg-blue-50 rounded-lg p-4' : ''}`}
      >
        {activeTab === 'recommendations' ? (
          <VehicleRecommendations
            recommendedVehicles={recommendedVehicles}
            parameters={parameters}
          />
        ) : (
          <div className="h-[600px] max-w-full">
            <div className="relative w-full h-full bg-white border rounded-lg shadow-md overflow-hidden">
              <ChatInterface
                onRecommendationsUpdated={handleRecommendationsUpdate}
              />
            </div>
          </div>
        )}
      </div>

      {/* Show assistant promo only if there are no recommendations yet */}
      {activeTab === 'recommendations' && recommendedVehicles.length === 0 && (
        <div className="bg-blue-50 p-5 rounded-lg shadow-sm">
          <h2 className="text-xl font-semibold text-blue-800">
            Need help finding your perfect car?
          </h2>
          <p className="mt-2 text-blue-700">
            Our AI assistant can help you discover vehicles based on your
            specific requirements and preferences. Use the chat in the bottom
            right corner!
          </p>
        </div>
      )}

      {/* Facebook Messenger Style Chat - Header Always Visible */}
      {activeTab === 'recommendations' && (
        <Paper
          elevation={3}
          sx={{
            position: 'fixed',
            bottom: 0,
            right: 24,
            width: { xs: '320px', sm: '350px' },
            height: isChatMinimized ? 'auto' : '400px',
            display: 'flex',
            flexDirection: 'column',
            zIndex: 1050,
            borderTopLeftRadius: 8,
            borderTopRightRadius: 8,
            overflow: 'hidden',
            transition: 'all 0.2s ease',
          }}
        >
          <Box
            sx={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
              backgroundColor: 'primary.main',
              color: 'white',
              p: 1.5,
              cursor: 'pointer',
            }}
            onClick={toggleChat}
          >
            <Box sx={{ display: 'flex', alignItems: 'center' }}>
              <Badge
                color="error"
                variant="dot"
                invisible={!showChatBadge}
                sx={{ mr: 1 }}
              >
                <Box
                  sx={{
                    width: 10,
                    height: 10,
                    borderRadius: '50%',
                    backgroundColor: '#4caf50',
                    marginRight: 1,
                  }}
                />
              </Badge>
              <Typography
                variant="subtitle1"
                component="h3"
                sx={{ fontWeight: 500 }}
              >
                Smart Auto Assistant
              </Typography>
            </Box>
            <IconButton
              size="small"
              sx={{ color: 'white' }}
              aria-label={isChatMinimized ? 'Expand chat' : 'Minimize chat'}
              onClick={(e) => {
                e.stopPropagation()
                toggleChat()
              }}
            >
              <MinimizeIcon fontSize="small" />
            </IconButton>
          </Box>

          <Slide
            direction="up"
            in={!isChatMinimized}
            mountOnEnter
            unmountOnExit
          >
            <Box sx={{ flexGrow: 1, overflow: 'hidden' }}>
              <ChatInterface
                onRecommendationsUpdated={handleRecommendationsUpdate}
              />
            </Box>
          </Slide>
        </Paper>
      )}
    </div>
  )
}

export default RecommendationsPage
