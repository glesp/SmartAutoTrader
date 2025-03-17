import { useState, useEffect } from 'react'
import axios from 'axios'

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
  fuelType: number
  vehicleType: number
  images?: VehicleImage[] | { $values: VehicleImage[] }
}

// Helper function to extract arrays from ASP.NET response format
const extractArray = <T,>(data: T[] | { $values: T[] } | undefined): T[] => {
  if (!data) return []
  if (Array.isArray(data)) return data
  if (data && '$values' in data) return data.$values
  return []
}

const VehicleRecommendations = () => {
  const [vehicles, setVehicles] = useState<Vehicle[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const fetchRecommendations = async () => {
      try {
        setLoading(true)

        // Use a test user ID for demonstration
        const userId = 1
        const response = await axios.get(`/api/recommendations/test/${userId}`)

        // Handle both possible response formats
        let vehiclesData: Vehicle[] = []
        if (Array.isArray(response.data)) {
          vehiclesData = response.data
        } else if (response.data && '$values' in response.data) {
          vehiclesData = response.data.$values
        }

        setVehicles(vehiclesData)
        setError(null)
      } catch (err) {
        console.error('Error fetching recommendations:', err)
        setError('Failed to load recommendations')
      } finally {
        setLoading(false)
      }
    }

    fetchRecommendations()
  }, [])

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

  return (
    <div style={{ padding: '20px' }}>
      <h2>AI-Powered Recommendations</h2>

      {loading ? (
        <p>Loading recommendations...</p>
      ) : error ? (
        <p style={{ color: 'red' }}>{error}</p>
      ) : vehicles.length === 0 ? (
        <p>No recommendations found</p>
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
