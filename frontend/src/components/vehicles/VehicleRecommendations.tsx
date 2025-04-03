import { useState, useEffect, useContext } from 'react'
import { AuthContext } from '../../contexts/AuthContext'
import { Vehicle } from '../../types/models.ts'
import { VehicleRecommendationsProps } from '../../types/models.ts'
import {
  Box,
  Typography,
  Button,
  Card,
  CardMedia,
  CardContent,
  CardActions,
} from '@mui/material'
import { Link } from 'react-router-dom'

// Helper function to extract arrays from ASP.NET response format
const extractArray = <T,>(data: T[] | { $values: T[] } | undefined): T[] => {
  if (!data) return []
  if (Array.isArray(data)) return data
  if (data && '$values' in data) return data.$values
  return []
}

const VehicleRecommendations = ({
  recommendedVehicles,
  parameters,
}: VehicleRecommendationsProps) => {
  const { user } = useContext(AuthContext)

  // Direct conversion function
  const getVehicles = () => {
    if (!recommendedVehicles) return []

    // If it's an array, use it directly
    if (Array.isArray(recommendedVehicles)) return recommendedVehicles

    // If it has $values property
    if (recommendedVehicles && '$values' in recommendedVehicles) {
      return recommendedVehicles.$values
    }

    return []
  }

  // Get vehicles directly from props
  const vehicles = getVehicles()

  // Helper function to get primary image URL or fallback
  const getImageUrl = (vehicle: Vehicle) => {
    const images = extractArray(vehicle.images)
    if (images.length === 0)
      return 'https://via.placeholder.com/300x200?text=No+Image'

    // Find primary image
    const primaryImage = images.find((img) => img.isPrimary)
    const path = primaryImage?.imageUrl || images[0]?.imageUrl

    // Important: Add the base URL prefix just like in VehicleDetailPage
    return path
      ? `https://localhost:7001/${path}`
      : 'https://via.placeholder.com/300x200?text=No+Image'
  }

  // Map fuel type numbers to strings
  const getFuelTypeName = (fuelType: number | string): string => {
    if (typeof fuelType === 'string') return fuelType

    const fuelTypes: Record<number, string> = {
      0: 'Petrol',
      1: 'Diesel',
      2: 'Electric',
      3: 'Hybrid',
      4: 'Plugin Hybrid',
    }
    return fuelTypes[fuelType] || 'Unknown'
  }

  // Map vehicle type numbers to strings
  const getVehicleTypeName = (vehicleType: number | string): string => {
    if (typeof vehicleType === 'string') return vehicleType

    const vehicleTypes: Record<number, string> = {
      0: 'Sedan',
      1: 'SUV',
      2: 'Hatchback',
      3: 'Coupe',
      4: 'Convertible',
      5: 'Wagon',
      6: 'Van',
      7: 'Truck',
    }
    return vehicleTypes[vehicleType] || 'Unknown'
  }

  // Alternative content when user is not authenticated
  if (!user) {
    return <Box p={4}>Please sign in to view recommendations.</Box>
  }

  return (
    <Box p={2}>
      <Typography variant="h4" fontWeight="bold" mb={3}>
        AI-Powered Recommendations
      </Typography>

      {vehicles.length === 0 ? (
        <Box
          textAlign="center"
          py={6}
          px={2}
          bgcolor="background.paper"
          borderRadius={2}
        >
          <Typography variant="h6" mb={2}>
            No recommendations available yet
          </Typography>
          <Typography variant="body1" color="text.secondary" mb={2}>
            Chat with our AI assistant to get personalized vehicle suggestions.
          </Typography>
          <Typography variant="body2" color="text.secondary" fontStyle="italic">
            Try asking: "Show me electric SUVs" or "I need a family car under
            €25,000"
          </Typography>
        </Box>
      ) : (
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: {
              xs: '1fr',
              sm: 'repeat(2, 1fr)',
              md: 'repeat(3, 1fr)',
              lg: 'repeat(4, 1fr)',
            },
            gap: 3,
          }}
        >
          {vehicles.map((vehicle) => (
            <Card
              key={vehicle.id}
              sx={{
                height: '100%',
                display: 'flex',
                flexDirection: 'column',
                transition: 'transform 0.2s, box-shadow 0.2s',
                '&:hover': {
                  transform: 'translateY(-4px)',
                  boxShadow: 4,
                },
                borderRadius: 2,
              }}
              elevation={2}
            >
              <CardMedia
                component="img"
                height={200}
                image={getImageUrl(vehicle)}
                alt={`${vehicle.make} ${vehicle.model}`}
                sx={{ objectFit: 'cover' }}
                onError={(e) => {
                  e.currentTarget.src =
                    'https://via.placeholder.com/300x200?text=No+Image'
                }}
              />
              <CardContent sx={{ flexGrow: 1, pb: 1 }}>
                <Typography
                  gutterBottom
                  variant="h6"
                  component="h3"
                  fontWeight={500}
                >
                  {vehicle.year} {vehicle.make} {vehicle.model}
                </Typography>
                <Typography
                  variant="h6"
                  color="primary"
                  fontWeight={600}
                  mb={1}
                >
                  €{vehicle.price.toLocaleString()}
                </Typography>
                <Box
                  display="flex"
                  justifyContent="space-between"
                  color="text.secondary"
                  mt={1}
                >
                  <Typography variant="body2">
                    {vehicle.mileage.toLocaleString()} km
                  </Typography>
                  <Typography variant="body2">
                    {getFuelTypeName(vehicle.fuelType)}
                  </Typography>
                </Box>
              </CardContent>
              <CardActions sx={{ p: 2, pt: 0 }}>
                <Button
                  component={Link}
                  to={`/vehicles/${vehicle.id}`}
                  variant="contained"
                  color="primary"
                  fullWidth
                  sx={{ borderRadius: 1 }}
                >
                  View Details
                </Button>
              </CardActions>
            </Card>
          ))}
        </Box>
      )}
    </Box>
  )
}

export default VehicleRecommendations
