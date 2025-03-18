import { useState, useEffect, useContext, useCallback } from 'react'
import axios from 'axios'
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
  fuelType: number
  vehicleType: number
  images?: VehicleImage[] | { $values: VehicleImage[] }
}

// New interface for recommendation parameters
interface RecommendationParams {
  userId: string
  textPrompt?: string
  minPrice?: number
  maxPrice?: number
  fuelType?: number
  vehicleType?: number
  year?: number
  minYear?: number
  maxYear?: number
  mileageMax?: number
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
  const { user, token } = useContext(AuthContext)
  const [prompt, setPrompt] = useState<string>('')
  const [filters, setFilters] = useState<Partial<RecommendationParams>>({})
  const [showFilters, setShowFilters] = useState(false)

  const fetchRecommendations = useCallback(
    async (params: RecommendationParams) => {
      if (!user || !token) {
        setLoading(false)
        return
      }

      try {
        setLoading(true)
        console.log(`Fetching recommendations with params:`, params)

        const response = await axios.get('/api/recommendations', {
          params,
          headers: {
            Authorization: `Bearer ${token}`,
            'Content-Type': 'application/json',
          },
        })

        console.log('API Response:', response.data)

        const vehiclesData: Vehicle[] = extractArray(response.data)
        setVehicles(vehiclesData)
        setError(null)
      } catch (err: any) {
        console.error('Error fetching recommendations:', err)
        setError('Failed to load recommendations')
      } finally {
        setLoading(false)
      }
    },
    [user, token]
  )

  useEffect(() => {
    if (user) {
      fetchRecommendations({ userId: user.id.toString() })
    } else {
      setLoading(false)
    }
  }, [user, fetchRecommendations])

  const handlePromptSearch = () => {
    if (!user) return

    fetchRecommendations({
      userId: user.id.toString(),
      textPrompt: prompt,
      ...filters,
    })
  }

  const handleFilterChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>
  ) => {
    const { name, value } = e.target as HTMLInputElement
    const numberFields = [
      'minPrice',
      'maxPrice',
      'minYear',
      'maxYear',
      'mileageMax',
      'fuelType',
      'vehicleType',
    ]

    let parsedValue: string | number = value
    if (numberFields.includes(name) && value) {
      parsedValue = parseFloat(value)
    }

    setFilters((prev) => ({
      ...prev,
      [name]: parsedValue === '' ? undefined : parsedValue,
    }))
  }

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

  // Map fuelType number to string
  const getFuelTypeName = (fuelType: number): string => {
    const fuelTypes: Record<number, string> = {
      0: 'Petrol',
      1: 'Diesel',
      2: 'Electric',
      3: 'Hybrid',
      4: 'Plugin Hybrid',
    }
    return fuelTypes[fuelType] || 'Unknown'
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
            placeholder="Enter your needs (e.g., 'cheap cars only', 'family SUV with good mileage')"
            style={{
              flex: 1,
              padding: '8px 12px',
              borderRadius: '4px',
              border: '1px solid #ccc',
            }}
          />
          <button
            onClick={handlePromptSearch}
            style={{
              backgroundColor: '#1976d2',
              color: 'white',
              border: 'none',
              padding: '8px 16px',
              borderRadius: '4px',
              cursor: 'pointer',
            }}
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
          {showFilters ? 'Hide Filters' : 'Show Additional Filters'}
        </button>

        {showFilters && (
          <div
            style={{
              marginTop: '10px',
              padding: '15px',
              backgroundColor: '#f5f5f5',
              borderRadius: '4px',
              display: 'grid',
              gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))',
              gap: '10px',
            }}
          >
            <div>
              <label style={{ display: 'block', marginBottom: '5px' }}>
                Price Range
              </label>
              <div style={{ display: 'flex', gap: '5px' }}>
                <input
                  type="number"
                  name="minPrice"
                  placeholder="Min"
                  onChange={handleFilterChange}
                  style={{ width: '100%', padding: '5px' }}
                />
                <input
                  type="number"
                  name="maxPrice"
                  placeholder="Max"
                  onChange={handleFilterChange}
                  style={{ width: '100%', padding: '5px' }}
                />
              </div>
            </div>

            <div>
              <label style={{ display: 'block', marginBottom: '5px' }}>
                Year Range
              </label>
              <div style={{ display: 'flex', gap: '5px' }}>
                <input
                  type="number"
                  name="minYear"
                  placeholder="Min"
                  onChange={handleFilterChange}
                  style={{ width: '100%', padding: '5px' }}
                />
                <input
                  type="number"
                  name="maxYear"
                  placeholder="Max"
                  onChange={handleFilterChange}
                  style={{ width: '100%', padding: '5px' }}
                />
              </div>
            </div>

            <div>
              <label style={{ display: 'block', marginBottom: '5px' }}>
                Max Mileage
              </label>
              <input
                type="number"
                name="mileageMax"
                placeholder="Maximum mileage"
                onChange={handleFilterChange}
                style={{ width: '100%', padding: '5px' }}
              />
            </div>

            <div>
              <label style={{ display: 'block', marginBottom: '5px' }}>
                Fuel Type
              </label>
              <select
                name="fuelType"
                onChange={handleFilterChange}
                style={{ width: '100%', padding: '5px' }}
              >
                <option value="">Any</option>
                <option value="0">Petrol</option>
                <option value="1">Diesel</option>
                <option value="2">Electric</option>
                <option value="3">Hybrid</option>
                <option value="4">Plugin Hybrid</option>
              </select>
            </div>

            <div>
              <label style={{ display: 'block', marginBottom: '5px' }}>
                Vehicle Type
              </label>
              <select
                name="vehicleType"
                onChange={handleFilterChange}
                style={{ width: '100%', padding: '5px' }}
              >
                <option value="">Any</option>
                <option value="0">Sedan</option>
                <option value="1">SUV</option>
                <option value="2">Hatchback</option>
                <option value="3">Coupe</option>
                <option value="4">Convertible</option>
                <option value="5">Van</option>
                <option value="6">Truck</option>
              </select>
            </div>
          </div>
        )}
      </div>

      {loading ? (
        <p>Loading recommendations...</p>
      ) : error ? (
        <p style={{ color: 'red' }}>{error}</p>
      ) : vehicles.length === 0 ? (
        <p>
          No recommendations found. Try adjusting your filters or prompt, or
          browse more vehicles to get personalized suggestions.
        </p>
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
