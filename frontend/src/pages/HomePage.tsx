/**
 * @file HomePage.tsx
 * @summary Provides the `HomePage` component, which serves as the main landing page for the Smart Auto Trader application.
 *
 * @description The `HomePage` component is the main entry point for users visiting the application. It includes a hero section with a welcome message,
 * a featured vehicles section showcasing the latest vehicle listings, and a features section highlighting the application's key benefits.
 * The component fetches featured vehicles from the backend API and displays them in a responsive grid layout.
 *
 * @remarks
 * - The component uses Material-UI for layout and styling, including components such as `Box`, `Container`, `Typography`, `Grid`, and `Paper`.
 * - React Router is used for navigation, enabling seamless routing to other pages such as the vehicle listing and AI recommendations pages.
 * - The `vehicleService` is used to fetch featured vehicles from the backend API.
 * - Error handling is implemented to gracefully handle API failures and display fallback content.
 *
 * @dependencies
 * - React: `useEffect`, `useState` for managing state and side effects.
 * - Material-UI: Components for layout, styling, and buttons.
 * - React Router: `Link` for navigation between pages.
 * - `vehicleService`: For interacting with the backend API to fetch vehicle data.
 * - `VehicleCard`: A reusable component for displaying individual vehicle details.
 *
 * @example
 * <HomePage />
 */

import { JSX, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { vehicleService } from '../services/api';
import VehicleCard from '../components/vehicles/VehicleCard';
import { Vehicle } from '../types/models';
import {
  Box,
  Button,
  Container,
  Typography,
  Grid,
  Paper,
  CircularProgress,
  Divider,
} from '@mui/material';
import DirectionsCarIcon from '@mui/icons-material/DirectionsCar';
import RecommendIcon from '@mui/icons-material/Recommend';

/**
 * @function HomePage
 * @summary Renders the main landing page for the application.
 *
 * @returns {JSX.Element} The rendered home page component.
 *
 * @remarks
 * - The component includes three main sections: a hero section, a featured vehicles section, and a features section.
 * - The featured vehicles section fetches data from the backend API and displays it in a responsive grid layout.
 * - The features section highlights the application's key benefits, such as AI-powered recommendations and expert support.
 * - Error handling ensures that fallback content is displayed if the API request fails.
 *
 * @example
 * <HomePage />
 */
const HomePage = (): JSX.Element => {
  const [featuredVehicles, setFeaturedVehicles] = useState<Vehicle[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    /**
     * @function loadFeaturedVehicles
     * @summary Fetches the latest featured vehicles from the backend API.
     *
     * @returns {Promise<void>} A promise that resolves when the data is fetched and the state is updated.
     *
     * @throws Will log an error to the console if the API request fails.
     *
     * @remarks
     * - The function fetches the latest 4 vehicles sorted by their listing date in descending order.
     * - If the API response is not an array, an error is logged, and the state is set to an empty array.
     */
    const loadFeaturedVehicles = async (): Promise<void> => {
      try {
        const response = await vehicleService.getVehicles({
          pageSize: 4,
          sortBy: 'DateListed',
          ascending: false,
        });

        console.log('API response type:', typeof response);
        console.log('Is array?', Array.isArray(response));
        console.log('Raw response:', response);

        // Safe check before setting state
        if (Array.isArray(response)) {
          setFeaturedVehicles(response);
        } else {
          console.error('Response is not an array:', response);
          setFeaturedVehicles([]);
        }
      } catch (error) {
        console.error('Error loading featured vehicles:', error);
        setFeaturedVehicles([]);
      } finally {
        setLoading(false);
      }
    };

    loadFeaturedVehicles();
  }, []);

  return (
    <Box>
      {/* Hero Section */}
      <Box
        sx={{
          backgroundColor: 'primary.main',
          color: 'white',
          py: 8,
          mb: 4,
        }}
      >
        <Container maxWidth="lg">
          <Box textAlign={{ xs: 'center', md: 'left' }} px={2}>
            <Typography
              variant="h2"
              component="h1"
              gutterBottom
              fontWeight="bold"
              sx={{ mb: 2 }}
            >
              Welcome to Smart Auto Trader
            </Typography>
            <Typography variant="h5" sx={{ mb: 4, fontWeight: 400 }}>
              Find your perfect vehicle with our AI-powered recommendations
            </Typography>
            <Button
              component={Link}
              to="/vehicles"
              variant="contained"
              size="large"
              sx={{
                bgcolor: 'white',
                color: 'primary.main',
                fontWeight: 600,
                '&:hover': {
                  bgcolor: 'rgba(255,255,255,0.9)',
                },
              }}
            >
              <DirectionsCarIcon sx={{ mr: 1 }} />
              Browse Vehicles
            </Button>
            <Button
              component={Link}
              to="/recommendations"
              variant="outlined"
              size="large"
              sx={{
                ml: 2,
                borderColor: 'white',
                color: 'white',
                fontWeight: 600,
                '&:hover': {
                  borderColor: 'white',
                  bgcolor: 'rgba(255,255,255,0.1)',
                },
              }}
            >
              <RecommendIcon sx={{ mr: 1 }} />
              AI Recommendations
            </Button>
          </Box>
        </Container>
      </Box>

      {/* Featured Vehicles Section */}
      <Container maxWidth="lg" sx={{ py: 4 }}>
        <Box textAlign="center" mb={5}>
          <Typography
            variant="h3"
            component="h2"
            fontWeight="bold"
            sx={{ mb: 1 }}
          >
            Featured Vehicles
          </Typography>
          <Divider
            sx={{
              width: '80px',
              mx: 'auto',
              my: 2,
              borderColor: 'primary.main',
              borderWidth: 2,
            }}
          />
          <Typography variant="subtitle1" color="text.secondary">
            Check out our latest arrivals
          </Typography>
        </Box>

        {loading ? (
          <Box
            sx={{
              display: 'flex',
              justifyContent: 'center',
              alignItems: 'center',
              height: '200px',
            }}
          >
            <CircularProgress />
            <Typography sx={{ ml: 2 }}>Loading featured vehicles...</Typography>
          </Box>
        ) : (
          <Grid container spacing={3}>
            {featuredVehicles.map((vehicle) => (
              <Grid item xs={12} sm={6} md={3} key={vehicle.id}>
                <VehicleCard vehicle={vehicle} />
              </Grid>
            ))}
          </Grid>
        )}

        <Box textAlign="center" mt={6}>
          <Button
            component={Link}
            to="/vehicles"
            variant="outlined"
            color="primary"
            size="large"
            sx={{ fontWeight: 500 }}
          >
            View All Vehicles
          </Button>
        </Box>
      </Container>

      {/* Features Section */}
      <Box sx={{ bgcolor: 'background.paper', py: 8, mt: 4 }}>
        <Container maxWidth="lg">
          <Box textAlign="center" mb={5}>
            <Typography
              variant="h3"
              component="h2"
              fontWeight="bold"
              sx={{ mb: 1 }}
            >
              Why Choose Us
            </Typography>
            <Divider
              sx={{
                width: '80px',
                mx: 'auto',
                my: 2,
                borderColor: 'primary.main',
                borderWidth: 2,
              }}
            />
          </Box>

          <Grid container spacing={4}>
            <Grid item xs={12} md={4}>
              <Paper
                elevation={0}
                sx={{
                  p: 4,
                  height: '100%',
                  textAlign: 'center',
                  borderRadius: 2,
                  bgcolor: 'rgba(25, 118, 210, 0.05)',
                }}
              >
                <Box
                  sx={{
                    mb: 2,
                    width: 70,
                    height: 70,
                    bgcolor: 'primary.main',
                    color: 'white',
                    borderRadius: '50%',
                    display: 'flex',
                    justifyContent: 'center',
                    alignItems: 'center',
                    mx: 'auto',
                  }}
                >
                  <RecommendIcon sx={{ fontSize: 32 }} />
                </Box>
                <Typography
                  variant="h5"
                  component="h3"
                  fontWeight="bold"
                  gutterBottom
                >
                  AI-Powered Recommendations
                </Typography>
                <Typography variant="body1">
                  Our smart system learns your preferences and suggests vehicles
                  that match your needs perfectly.
                </Typography>
              </Paper>
            </Grid>

            <Grid item xs={12} md={4}>
              <Paper
                elevation={0}
                sx={{
                  p: 4,
                  height: '100%',
                  textAlign: 'center',
                  borderRadius: 2,
                  bgcolor: 'rgba(25, 118, 210, 0.05)',
                }}
              >
                <Box
                  sx={{
                    mb: 2,
                    width: 70,
                    height: 70,
                    bgcolor: 'primary.main',
                    color: 'white',
                    borderRadius: '50%',
                    display: 'flex',
                    justifyContent: 'center',
                    alignItems: 'center',
                    mx: 'auto',
                  }}
                >
                  <DirectionsCarIcon sx={{ fontSize: 32 }} />
                </Box>
                <Typography
                  variant="h5"
                  component="h3"
                  fontWeight="bold"
                  gutterBottom
                >
                  Quality Vehicles
                </Typography>
                <Typography variant="body1">
                  All our vehicles are thoroughly inspected and come with a
                  comprehensive service history.
                </Typography>
              </Paper>
            </Grid>

            <Grid item xs={12} md={4}>
              <Paper
                elevation={0}
                sx={{
                  p: 4,
                  height: '100%',
                  textAlign: 'center',
                  borderRadius: 2,
                  bgcolor: 'rgba(25, 118, 210, 0.05)',
                }}
              >
                <Box
                  sx={{
                    mb: 2,
                    width: 70,
                    height: 70,
                    bgcolor: 'primary.main',
                    color: 'white',
                    borderRadius: '50%',
                    display: 'flex',
                    justifyContent: 'center',
                    alignItems: 'center',
                    mx: 'auto',
                  }}
                >
                  <span style={{ fontSize: '32px' }}>🛠️</span>
                </Box>
                <Typography
                  variant="h5"
                  component="h3"
                  fontWeight="bold"
                  gutterBottom
                >
                  Expert Support
                </Typography>
                <Typography variant="body1">
                  Our team of automotive experts is available to help you find
                  the perfect vehicle for your needs.
                </Typography>
              </Paper>
            </Grid>
          </Grid>
        </Container>
      </Box>
    </Box>
  );
};

export default HomePage;
