import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { vehicleService } from '../services/api'
import VehicleCard from '../components/vehicles/VehicleCard'
import { Vehicle } from '../types/models'
// Import Material UI components
import {
  Box,
  Button,
  Container,
  Typography,
  Grid,
  Paper,
  CircularProgress,
  Divider,
} from '@mui/material'
import DirectionsCarIcon from '@mui/icons-material/DirectionsCar'
import RecommendIcon from '@mui/icons-material/Recommend'

const HomePage = () => {
  const [featuredVehicles, setFeaturedVehicles] = useState<Vehicle[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const loadFeaturedVehicles = async () => {
      try {
        // Get the latest 4 vehicles
        console.log('Fetching vehicles...')
        const response = await vehicleService.getVehicles({
          pageSize: 4,
          sortBy: 'DateListed',
          ascending: false,
        })

        console.log('API response type:', typeof response)
        console.log('Is array?', Array.isArray(response))
        console.log('Raw response:', response)

        // Safe check before setting state
        if (Array.isArray(response)) {
          setFeaturedVehicles(response)
        } else {
          console.error('Response is not an array:', response)
          setFeaturedVehicles([]) // Use empty array as fallback
        }
      } catch (error) {
        console.error('Error loading featured vehicles:', error)
        setFeaturedVehicles([])
      } finally {
        setLoading(false)
      }
    }

    loadFeaturedVehicles()
  }, [])

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

      {/* Features Section (New) */}
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
                  {/* You can add another icon here */}
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
  )
}

export default HomePage
