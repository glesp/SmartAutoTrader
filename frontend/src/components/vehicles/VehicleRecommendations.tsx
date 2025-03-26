import { useState, useEffect, useContext } from 'react'
import { AuthContext } from '../../contexts/AuthContext'

// Simple interfaces for the component
interface VehicleImage {
  id: number
  imageUrl: string
  isPrimary: boolean
}

interface Vehicle {
  id: number
  make: string
  model: string
  year: number
  price: number
  mileage: number
  fuelType: number | string
  vehicleType: number | string
  images?: VehicleImage[] | { $values: VehicleImage[] }
}

// Interface for component props
interface VehicleRecommendationsProps {
  recommendedVehicles?: Vehicle[] | { $values: Vehicle[] }
  parameters?: any
}

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
    if (images.length === 0) return 'https://via.placeholder.com/200x150'

    // Find primary image
    const primaryImage = images.find((img) => img.isPrimary)
    return (
      primaryImage?.imageUrl ||
      images[0]?.imageUrl ||
      'https://via.placeholder.com/200x150'
    )
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
    return <div>Please sign in to view recommendations.</div>
  }

  return (
    <div style={{ padding: '20px' }}>
      <h2>AI-Powered Recommendations</h2>

      {vehicles.length === 0 ? (
        <div style={{ textAlign: 'center', padding: '30px 0' }}>
          <p>
            No recommendations available yet. Chat with our AI assistant to get
            personalized vehicle suggestions.
          </p>
          <p style={{ fontSize: '0.9em', marginTop: '10px' }}>
            Try asking: "Show me electric SUVs" or "I need a family car under
            €25,000"
          </p>
        </div>
      ) : (
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))',
            gap: '20px',
          }}
        >
          {vehicles.map((vehicle) => (
            <div
              key={vehicle.id}
              style={{
                border: '1px solid #eee',
                borderRadius: '8px',
                overflow: 'hidden',
                boxShadow: '0 2px 4px rgba(0,0,0,0.1)',
              }}
            >
              <img
                src={getImageUrl(vehicle)}
                alt={`${vehicle.make} ${vehicle.model}`}
                style={{ width: '100%', height: '200px', objectFit: 'cover' }}
                onError={(e) => {
                  // Fallback if image fails to load
                  e.currentTarget.src =
                    'https://via.placeholder.com/300x200?text=No+Image'
                }}
              />
              <div style={{ padding: '16px' }}>
                <h3 style={{ margin: '0 0 8px', fontSize: '1.2rem' }}>
                  {vehicle.year} {vehicle.make} {vehicle.model}
                </h3>
                <p
                  style={{
                    margin: '0 0 8px',
                    fontWeight: 'bold',
                    fontSize: '1.1rem',
                    color: '#1976d2',
                  }}
                >
                  €{vehicle.price.toLocaleString()}
                </p>
                <div
                  style={{
                    display: 'flex',
                    justifyContent: 'space-between',
                    margin: '8px 0',
                    color: '#666',
                  }}
                >
                  <span>{vehicle.mileage.toLocaleString()} km</span>
                  <span>{getFuelTypeName(vehicle.fuelType)}</span>
                </div>
                <div style={{ marginTop: '12px' }}>
                  <a
                    href={`/vehicles/${vehicle.id}`}
                    style={{
                      display: 'inline-block',
                      backgroundColor: '#1976d2',
                      color: 'white',
                      padding: '8px 16px',
                      borderRadius: '4px',
                      textDecoration: 'none',
                      textAlign: 'center',
                      width: '100%',
                    }}
                  >
                    View Details
                  </a>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

export default VehicleRecommendations
