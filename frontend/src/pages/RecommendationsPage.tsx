/**
 * @file RecommendationsPage.tsx
 * @summary Provides the `RecommendationsPage` component, which displays personalized vehicle recommendations and an AI assistant chat interface.
 *
 * @description The `RecommendationsPage` component allows authenticated users to view personalized vehicle recommendations based on their preferences.
 * It includes two main tabs: one for viewing recommendations and another for interacting with an AI assistant via a chat interface. The component fetches
 * recommendations from the backend and updates the UI dynamically based on user interactions. It also includes a floating chat widget for quick access
 * to the AI assistant.
 *
 * @remarks
 * - The component uses Material-UI for layout and styling, including components such as `Container`, `Paper`, `Tabs`, and `Grid`.
 * - React Router is used for navigation, enabling redirection for unauthenticated users.
 * - The `AuthContext` is used to check the user's authentication status.
 * - The `VehicleRecommendations` and `ChatInterface` components are used to display recommendations and provide AI assistant functionality, respectively.
 * - Error handling is implemented to gracefully handle API failures and display fallback content.
 *
 * @dependencies
 * - React: `useState`, `useContext` for managing state and accessing the authentication context.
 * - Material-UI: Components for layout, styling, and animations.
 * - React Router: `Navigate` for redirection.
 * - `AuthContext`: For managing user authentication and access control.
 * - `VehicleRecommendations`: A reusable component for displaying vehicle recommendations.
 * - `ChatInterface`: A reusable component for interacting with the AI assistant.
 *
 * @example
 * <RecommendationsPage />
 */

import { JSX, useContext, useState } from 'react';
import { Navigate } from 'react-router-dom';
import { AuthContext } from '../contexts/AuthContext';
import VehicleRecommendations from '../components/vehicles/VehicleRecommendations';
import ChatInterface from '../components/chat/ChatInterface';
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
  Grid,
  Stack,
} from '@mui/material';
import MinimizeIcon from '@mui/icons-material/Minimize';
import ChatIcon from '@mui/icons-material/Chat';
import DirectionsCarIcon from '@mui/icons-material/DirectionsCar';
import { Vehicle, RecommendationParameters } from '../types/models';

/**
 * @function RecommendationsPage
 * @summary Renders the recommendations page, providing personalized vehicle recommendations and an AI assistant chat interface.
 *
 * @returns {JSX.Element} The rendered recommendations page component.
 *
 * @remarks
 * - The component includes two main tabs: "Recommendations" for viewing vehicle recommendations and "AI Assistant" for interacting with the chat assistant.
 * - It dynamically updates the recommendations based on user interactions with the AI assistant.
 * - A floating chat widget provides quick access to the AI assistant, with a badge indicating new recommendations.
 * - If the user is not authenticated, they are redirected to the login page.
 *
 * @example
 * <RecommendationsPage />
 */
const RecommendationsPage = (): JSX.Element => {
  const theme = useTheme();
  const { isAuthenticated, loading } = useContext(AuthContext);
  const [activeTab, setActiveTab] = useState<'recommendations' | 'assistant'>(
    'recommendations'
  );
  const [recommendedVehicles, setRecommendedVehicles] = useState<Vehicle[]>([]);
  const [parameters, setParameters] = useState<RecommendationParameters>({});
  const [isChatMinimized, setIsChatMinimized] = useState(true);
  const [newRecommendationsFlag, setNewRecommendationsFlag] = useState(false);
  const [showChatBadge, setShowChatBadge] = useState(false);
  const [isLoadingRecommendations] = useState(false);

  if (loading) {
    return (
      <Container maxWidth="lg" sx={{ py: 8, textAlign: 'center' }}>
        <Typography variant="h5" color="text.secondary">
          Loading...
        </Typography>
      </Container>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: '/recommendations' }} />;
  }

  /**
   * @function handleRecommendationsUpdate
   * @summary Updates the recommendations and parameters based on AI assistant interactions.
   *
   * @param {Vehicle[]} vehicles - The updated list of recommended vehicles.
   * @param {RecommendationParameters} newParams - The updated recommendation parameters.
   *
   * @remarks
   * - This function is triggered when the AI assistant provides new recommendations.
   * - It sets a flag to trigger a visual animation and updates the badge on the chat widget.
   */
  const handleRecommendationsUpdate = (
    vehicles: Vehicle[],
    newParams: RecommendationParameters
  ) => {
    console.log('Received recommendations:', vehicles.length, 'vehicles');
    console.log('Received parameters:', newParams);

    setRecommendedVehicles(vehicles);
    setParameters(newParams);

    setNewRecommendationsFlag(true);

    if (isChatMinimized) {
      setShowChatBadge(true);
    }

    setTimeout(() => {
      setNewRecommendationsFlag(false);
    }, 2000);
  };

  /**
   * @function toggleChat
   * @summary Toggles the minimized state of the chat widget.
   *
   * @remarks
   * - When the chat widget is minimized, the badge indicating new recommendations is cleared.
   */
  const toggleChat = () => {
    setIsChatMinimized(!isChatMinimized);
    if (!isChatMinimized) {
      setShowChatBadge(false);
    }
  };

  return (
    <Container maxWidth="lg" sx={{ py: 4, pb: { xs: 24, md: 20 } }}>
      <Grid container spacing={3}>
        {/* Page Header */}
        <Grid item xs={12}>
          <Stack spacing={2}>
            <Typography variant="h3" component="h1" fontWeight="bold">
              Personalized Recommendations
            </Typography>
            <Typography variant="subtitle1" color="text.secondary">
              Our AI analyzes your preferences to recommend vehicles you might
              like. Chat with our assistant for personalized suggestions.
            </Typography>
            <Divider sx={{ mt: 1 }} />
          </Stack>
        </Grid>

        {/* Tabs Navigation */}
        <Grid item xs={12}>
          <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
            <Tabs
              value={activeTab}
              onChange={(_e, newValue) => setActiveTab(newValue)}
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
        </Grid>

        {/* Main Content Area */}
        <Grid item xs={12}>
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
              p: 0,
              borderRadius: 2,
              transition: 'all 0.5s ease',
              backgroundColor: newRecommendationsFlag
                ? 'rgba(25, 118, 210, 0.05)'
                : 'transparent',
            }}
          >
            {activeTab === 'recommendations' ? (
              <Grid item xs={12}>
                <Fade in={!isLoadingRecommendations} timeout={500}>
                  <div>
                    <VehicleRecommendations
                      recommendedVehicles={recommendedVehicles}
                      parameters={parameters}
                    />
                  </div>
                </Fade>
              </Grid>
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
        </Grid>

        {/* Assistant Promo */}
        {activeTab === 'recommendations' &&
          recommendedVehicles.length === 0 && (
            <Grid item xs={12}>
              <Alert
                severity="info"
                variant="outlined"
                sx={{
                  borderRadius: 2,
                  backgroundColor: 'rgba(25, 118, 210, 0.05)',
                }}
              >
                <AlertTitle sx={{ fontWeight: 'bold' }}>
                  Need help finding your perfect car?
                </AlertTitle>
                Our AI assistant can help you discover vehicles based on your
                specific requirements and preferences. Use the chat in the
                bottom right corner or switch to the AI Assistant tab!
              </Alert>
            </Grid>
          )}
      </Grid>

      {/* Chat Widget */}
      {activeTab === 'recommendations' && (
        <Paper
          elevation={4}
          sx={{
            position: 'fixed',
            bottom: 0,
            right: { xs: '16px', sm: '24px' },
            width: { xs: '90%', sm: '350px' },
            maxWidth: { xs: 'calc(100% - 32px)', sm: '350px' },
            height: isChatMinimized ? 'auto' : { xs: '80vh', sm: '500px' },
            maxHeight: { xs: '80vh', sm: '500px' },
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
                sx={{
                  fontWeight: 600,
                  color: 'white',
                  fontSize: '0.95rem',
                  letterSpacing: '0.2px',
                  textShadow: '0px 1px 2px rgba(0,0,0,0.2)',
                }}
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
                e.stopPropagation();
                toggleChat();
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
  );
};

export default RecommendationsPage;
