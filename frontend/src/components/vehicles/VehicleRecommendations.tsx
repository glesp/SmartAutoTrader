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
  recommendedVehicles?: Vehicle[]
  parameters?: {
    minPrice?: number
    maxPrice?: number
    minYear?: number
    maxYear?: number
    preferredMakes?: string[]
    preferredVehicleTypes?: string[]
    preferredFuelTypes?: string[]
    desiredFeatures?: string[]
  }
}

// Helper function to extract arrays from ASP.NET response format
const extractArray = <T,>(data: T[] | { $values: T[] } | undefined): T[] => {
  if (!data) return []
  if (Array.isArray(data)) return data
  if (data && '$values' in data) return data.$values
  return []
}

const VehicleRecommendations = ({ 
  recommendedVehicles = [], 
  parameters = {}
}: VehicleRecommendationsProps) => {
  const [vehicles, setVehicles] = useState<Vehicle[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const { user } = useContext(AuthContext)
  const [prompt, setPrompt] = useState<string>('')
  const [showFilters, setShowFilters] = useState(false)

  // Fix for infinite loop - only update if vehicles actually changed
  useEffect(() => {
    if (recommendedVehicles && recommendedVehicles.length > 0) {
      // Use JSON.stringify for deep comparison to prevent unnecessary updates
      const currentVehiclesJson = JSON.stringify(vehicles.map(v => v.id));
      const newVehiclesJson = JSON.stringify(recommendedVehicles.map(v => v.id));
      
      if (currentVehiclesJson !== newVehiclesJson) {
        setVehicles(recommendedVehicles)
        setLoading(false)
        setError(null)
      }
    } else if (vehicles.length > 0) {
      // Only clear vehicles if there's something to clear
      setVehicles([])
      setLoading(false)
    }
  }, [recommendedVehicles, vehicles])

  // Helper function to get image URL
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

  // Map fuelType to string
  const getFuelTypeName = (fuelType: number | string): string => {
    // If it's already a string, return it directly
    if (typeof fuelType === 'string') {
      return fuelType
    }
    
    // If it's a number, map it
    const fuelTypes: Record<number, string> = {
      0: 'Petrol',
      1: 'Diesel',
      2: 'Electric',
      3: 'Hybrid',
      4: 'Plugin Hybrid',
    }
    return fuelTypes[fuelType] || 'Unknown'
  }

  // Map vehicleType to string
  const getVehicleTypeName = (vehicleType: number | string): string => {
    // If it's already a string, return it directly
    if (typeof vehicleType === 'string') {
      return vehicleType
    }
    
    // If it's a number, map it
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
    return (
      <div style={{ padding: '20px' }}>
        <h2>AI-Powered Recommendations</h2>
        <p>Please sign in to view personalized recommendations.</p>
      </div>
    )
  }

  return (
    <div style={{ padding: '20px' }}>
      <h2>AI-Powered Recommendations</h2>

      <div style={{ marginBottom: '20px' }}>
        <div style={{ display: 'flex', gap: '10px', marginBottom: '10px' }}>
          <input
            type="text"
            value={prompt}
            onChange={(e) => setPrompt(e.target.value)}
            placeholder="Ask the assistant for recommendations in the chat above"
            style={{
              flex: 1,
              padding: '8px 12px',
              borderRadius: '4px',
              border: '1px solid #ccc',
            }}
            disabled={true}
          />
          <button
            style={{
              backgroundColor: '#1976d2',
              color: 'white',
              border: 'none',
              padding: '8px 16px',
              borderRadius: '4px',
              cursor: 'not-allowed',
              opacity: 0.7,
            }}
            disabled={true}
          >
            Search
          </button>
        </div>

        <button
          onClick={() => setShowFilters(!showFilters)}
          style={{
            background: 'transparent',
            border: 'none',
            color: '#1976d2',
            cursor: 'pointer',
            display: 'flex',
            alignItems: 'center',
            padding: 0,
          }}
        >
          {showFilters ? 'Hide Filters' : 'Show Active Filters'}
        </button>

        {showFilters && parameters && (
          <div
            style={{
              marginTop: '10px',
              padding: '15px',
              backgroundColor: '#f5f5f5',
              borderRadius: '4px',
            }}
          >
            <h3 style={{ marginTop: 0 }}>Current Active Filters</h3>
            <ul style={{ margin: 0, padding: '0 0 0 20px' }}>
              {parameters.minPrice !== undefined && <li>Min Price: €{parameters.minPrice.toLocaleString()}</li>}
              {parameters.maxPrice !== undefined && <li>Max Price: €{parameters.maxPrice.toLocaleString()}</li>}
              {parameters.minYear !== undefined && <li>Min Year: {parameters.minYear}</li>}
              {parameters.maxYear !== undefined && <li>Max Year: {parameters.maxYear}</li>}
              {parameters.preferredMakes && parameters.preferredMakes.length > 0 && (
                <li>Makes: {parameters.preferredMakes.join(', ')}</li>
              )}
              {parameters.preferredFuelTypes && parameters.preferredFuelTypes.length > 0 && (
                <li>Fuel Types: {parameters.preferredFuelTypes.join(', ')}</li>
              )}
              {parameters.preferredVehicleTypes && parameters.preferredVehicleTypes.length > 0 && (
                <li>Vehicle Types: {parameters.preferredVehicleTypes.join(', ')}</li>
              )}
              {parameters.desiredFeatures && parameters.desiredFeatures.length > 0 && (
                <li>Features: {parameters.desiredFeatures.join(', ')}</li>
              )}
            </ul>
            <p style={{ marginTop: '10px', fontSize: '0.9em', fontStyle: 'italic' }}>
              Use the chat assistant above to update your search parameters.
            </p>
          </div>
        )}
      </div>

      {loading ? (
        <p>Loading recommendations...</p>
      ) : error ? (
        <p style={{ color: 'red' }}>{error}</p>
      ) : vehicles.length === 0 ? (
        <div style={{ textAlign: 'center', padding: '30px 0' }}>
          <p>
            No recommendations available yet. Chat with our AI assistant above to get personalized vehicle suggestions.
          </p>
          <p style={{ fontSize: '0.9em', marginTop: '10px' }}>
            Try asking: "Show me electric SUVs" or "I need a family car under €25,000"
          </p>
        </div>
      ) : (
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fill, minmax(250px, 1fr))',
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
                style={{ width: '100%', height: '150px', objectFit: 'cover' }}
              />
              <div style={{ padding: '12px' }}>
                <h3 style={{ margin: '0 0 8px' }}>
                  {vehicle.year} {vehicle.make} {vehicle.model}
                </h3>
                <p
                  style={{
                    margin: '0 0 8px',
                    fontWeight: 'bold',
                    color: '#1976d2',
                  }}
                >
                  ${vehicle.price.toLocaleString()}
                </p>
                <p
                  style={{
                    margin: '0 0 8px',
                    fontSize: '0.9em',
                    color: '#666',
                  }}
                >
                  {vehicle.mileage.toLocaleString()} km •{' '}
                  {getFuelTypeName(vehicle.fuelType)}
                </p>
                <a
                  href={`/vehicles/${vehicle.id}`}
                  style={{
                    display: 'inline-block',
                    backgroundColor: '#1976d2',
                    color: 'white',
                    padding: '6px 12px',
                    borderRadius: '4px',
                    textDecoration: 'none',
                  }}
                >
                  View Details
                </a>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

export default VehicleRecommendations