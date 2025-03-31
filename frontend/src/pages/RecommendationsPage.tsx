import { useContext, useState, useEffect } from 'react'
import { Navigate } from 'react-router-dom'
import { AuthContext } from '../contexts/AuthContext'
import VehicleRecommendations from '../components/vehicles/VehicleRecommendations'
import ChatInterface from '../components/chat/ChatInterface'
// Import Material-UI components
import {
  Paper,
  Typography,
  Box,
  IconButton,
  Slide,
  Badge,
  Container,
  Tabs,
  Tab,
  Divider,
  Alert,
  AlertTitle,
  Fade,
  useTheme,
} from '@mui/material'
import MinimizeIcon from '@mui/icons-material/Minimize'
import ChatIcon from '@mui/icons-material/Chat'
import DirectionsCarIcon from '@mui/icons-material/DirectionsCar'
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
  const theme = useTheme()
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
    return (
      <Container maxWidth="lg" sx={{ py: 8, textAlign: 'center' }}>
        <Typography variant="h5" color="text.secondary">
          Loading...
        </Typography>
      </Container>
    )
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
    <Container maxWidth="lg" sx={{ py: 4, pb: 20 }}>
      {/* Page Header */}
      <Box sx={{ mb: 4 }}>
        <Typography variant="h3" component="h1" fontWeight="bold" gutterBottom>
          Personalized Recommendations
        </Typography>
        <Typography variant="subtitle1" color="text.secondary">
          Our AI analyzes your preferences to recommend vehicles you might like.
          Chat with our assistant for personalized suggestions.
        </Typography>
        <Divider sx={{ mt: 3 }} />
      </Box>

      {/* Tabs Navigation */}
      <Box sx={{ mb: 3, borderBottom: 1, borderColor: 'divider' }}>
        <Tabs
          value={activeTab}
          onChange={(e, newValue) => setActiveTab(newValue)}
          indicatorColor="primary"
          textColor="primary"
        >
          <Tab
            icon={<DirectionsCarIcon sx={{ mr: 1 }} />}
            iconPosition="start"
            label="Recommendations"
            value="recommendations"
            sx={{ fontWeight: 500, textTransform: 'none' }}
          />
          <Tab
            icon={<ChatIcon sx={{ mr: 1 }} />}
            iconPosition="start"
            label="AI Assistant"
            value="assistant"
            sx={{ fontWeight: 500, textTransform: 'none' }}
          />
        </Tabs>
      </Box>

      {/* Main Content Area - with recommendation highlight effect */}
      <Fade in={newRecommendationsFlag}>
        <Box
          sx={{
            position: 'absolute',
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            backgroundColor: 'rgba(25, 118, 210, 0.05)',
            zIndex: -1,
            pointerEvents: 'none',
          }}
        />
      </Fade>

      <Paper
        elevation={0}
        sx={{
          mb: 4,
          p: 0,
          borderRadius: 2,
          transition: 'all 0.5s ease',
          backgroundColor: newRecommendationsFlag
            ? 'rgba(25, 118, 210, 0.05)'
            : 'transparent',
        }}
      >
        {activeTab === 'recommendations' ? (
          <VehicleRecommendations
            recommendedVehicles={recommendedVehicles}
            parameters={parameters}
          />
        ) : (
          <Paper
            elevation={2}
            sx={{
              height: { xs: '500px', md: '600px' },
              borderRadius: 2,
              overflow: 'hidden',
            }}
          >
            <ChatInterface
              onRecommendationsUpdated={handleRecommendationsUpdate}
            />
          </Paper>
        )}
      </Paper>

      {/* Show assistant promo only if there are no recommendations yet */}
      {activeTab === 'recommendations' && recommendedVehicles.length === 0 && (
        <Alert
          severity="info"
          variant="outlined"
          sx={{
            borderRadius: 2,
            mb: 4,
            backgroundColor: 'rgba(25, 118, 210, 0.05)',
          }}
        >
          <AlertTitle sx={{ fontWeight: 'bold' }}>
            Need help finding your perfect car?
          </AlertTitle>
          Our AI assistant can help you discover vehicles based on your specific
          requirements and preferences. Use the chat in the bottom right corner
          or switch to the AI Assistant tab!
        </Alert>
      )}

      {/* Facebook Messenger Style Chat - Header Always Visible */}
      {activeTab === 'recommendations' && (
        <Paper
          elevation={4}
          sx={{
            position: 'fixed',
            bottom: 0,
            right: 24,
            width: { xs: '300px', sm: '350px' },
            height: isChatMinimized ? 'auto' : '500px',
            display: 'flex',
            flexDirection: 'column',
            zIndex: 1050,
            borderTopLeftRadius: 12,
            borderTopRightRadius: 12,
            overflow: 'hidden',
            transition: theme.transitions.create(['height', 'box-shadow'], {
              duration: theme.transitions.duration.standard,
            }),
            boxShadow: isChatMinimized ? 4 : 8,
          }}
        >
          <Box
            sx={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
              backgroundColor: 'primary.main',
              color: 'white',
              p: 2,
              cursor: 'pointer',
              transition: theme.transitions.create('background-color', {
                duration: theme.transitions.duration.short,
              }),
              '&:hover': {
                backgroundColor: 'primary.dark',
              },
            }}
            onClick={toggleChat}
          >
            <Box sx={{ display: 'flex', alignItems: 'center' }}>
              <Badge
                color="error"
                variant="dot"
                invisible={!showChatBadge}
                sx={{ mr: 1.5 }}
              >
                <ChatIcon fontSize="small" />
              </Badge>
              <Typography
                variant="subtitle1"
                component="h3"
                sx={{ fontWeight: 500 }}
              >
                Smart Auto Assistant
              </Typography>
              {showChatBadge && (
                <Typography
                  variant="caption"
                  sx={{
                    ml: 1.5,
                    bgcolor: 'error.main',
                    color: 'white',
                    px: 1,
                    py: 0.3,
                    borderRadius: 5,
                    fontWeight: 'bold',
                  }}
                >
                  New
                </Typography>
              )}
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
            <Box sx={{ flexGrow: 1, overflow: 'hidden', height: '100%' }}>
              <ChatInterface
                onRecommendationsUpdated={handleRecommendationsUpdate}
              />
            </Box>
          </Slide>
        </Paper>
      )}
    </Container>
  )
}

export default RecommendationsPage
