import { useContext, useState } from 'react'
import { Navigate } from 'react-router-dom'
import { AuthContext } from '../contexts/AuthContext'
import VehicleRecommendations from '../components/vehicles/VehicleRecommendations'
import ChatInterface from '../components/chat/ChatInterface'
// Import Material-UI components
import Fab from '@mui/material/Fab';
import ChatIcon from '@mui/icons-material/Chat';
import CloseIcon from '@mui/icons-material/Close';
import Paper from '@mui/material/Paper';
import Typography from '@mui/material/Typography';
import Box from '@mui/material/Box';
import IconButton from '@mui/material/IconButton';

// Types for the chat component integration
interface Vehicle {
  id: number
  make: string
  model: string
  year: number
  price: number
  mileage: number
  fuelType: number | string
  vehicleType: number | string
  images?: any
}

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
  const [showChatButton, setShowChatButton] = useState(true)
  const [showChatInterface, setShowChatInterface] = useState(false)

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
    console.log("Received recommendations:", vehicles.length, "vehicles");
    setRecommendedVehicles(vehicles)
    setParameters(newParams)
    setActiveTab('recommendations')
  }

  // Open chat
  const openChat = () => {
    setShowChatButton(false)
    setShowChatInterface(true)
  }

  // Close chat
  const closeChat = () => {
    setShowChatInterface(false)
    setShowChatButton(true)
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

      {/* Tab Content */}
      <div className="mb-8">
        {activeTab === 'recommendations' ? (
          // Always use VehicleRecommendations and pass the vehicles as props
          <VehicleRecommendations 
            recommendedVehicles={recommendedVehicles} 
            parameters={parameters} 
          />
        ) : (
          // Show chat interface when on assistant tab
          <div className="h-[600px] max-w-full">
           <div className="relative w-full h-full bg-white border rounded-lg shadow-md overflow-hidden">
             <ChatInterface
              onRecommendationsUpdated={handleRecommendationsUpdate}
           />
            </div>
          </div>
        )}
      </div>

      {/* If on recommendations tab, show assistant promo */}
      {activeTab === 'recommendations' && (
        <div className="bg-blue-50 p-5 rounded-lg shadow-sm">
          <h2 className="text-xl font-semibold text-blue-800">
            Need help finding your perfect car?
          </h2>
          <p className="mt-2 text-blue-700">
            Our AI assistant can help you discover vehicles based on your
            specific requirements and preferences.
          </p>
          <button
            onClick={() => setActiveTab('assistant')}
            className="mt-4 bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700 transition-colors"
          >
            Chat with AI Assistant
          </button>
        </div>
      )}

      {/* Material-UI Messenger-style Chat Button */}
      {activeTab === 'recommendations' && showChatButton && (
        <Box sx={{ 
          position: 'fixed', 
          bottom: 24, 
          right: 24, 
          zIndex: 1050 
        }}>
          <Fab 
            color="primary" 
            aria-label="chat"
            onClick={openChat}
            className="pulse-animation"
          >
            <ChatIcon />
          </Fab>
        </Box>
      )}

      {/* Material-UI Messenger-style Chat Interface */}
      {activeTab === 'recommendations' && showChatInterface && (
        <Paper 
          elevation={8}
          sx={{
            position: 'fixed',
            bottom: 0,
            right: 24,
            width: { xs: '320px', sm: '380px' },
            height: '400px',
            display: 'flex',
            flexDirection: 'column',
            overflow: 'hidden',
            zIndex: 1050,
            borderTopLeftRadius: 8,
            borderTopRightRadius: 8,
            borderBottomLeftRadius: 0,
            borderBottomRightRadius: 0
          }}
        >
          <Box 
            sx={{ 
              display: 'flex', 
              alignItems: 'center', 
              justifyContent: 'space-between',
              backgroundColor: 'primary.main',
              color: 'white',
              p: 1.5
            }}
          >
            <Typography variant="subtitle1" component="h3" sx={{ fontWeight: 500 }}>
              Smart Auto Assistant
            </Typography>
            <IconButton 
              size="small" 
              onClick={closeChat}
              sx={{ color: 'white' }}
              aria-label="close chat"
            >
              <CloseIcon fontSize="small" />
            </IconButton>
          </Box>
          <Box sx={{ flexGrow: 1, overflow: 'hidden' }}>
            <ChatInterface 
              onRecommendationsUpdated={handleRecommendationsUpdate}
            />
          </Box>
        </Paper>
      )}
    </div>
  )
}

export default RecommendationsPage