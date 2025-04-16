import { Link } from 'react-router-dom'
import { VehicleProps } from '../../types/models'

// Define image type to avoid type errors
interface VehicleImage {
  imageUrl: string
  isPrimary: boolean
}

// Helper function to extract arrays from ASP.NET response format
const extractArray = <T,>(
  data: T[] | { $values: T[] } | undefined | Record<string, unknown>
): T[] => {
  if (!data) return []
  if (Array.isArray(data)) return data
  if (data && '$values' in data && Array.isArray(data.$values))
    return data.$values as T[]
  return []
}

// Map fuel type numbers to strings
const getFuelTypeName = (fuelType?: number | string): string => {
  if (!fuelType) return ''
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

const VehicleCard: React.FC<VehicleProps> = ({ vehicle }) => {
  // Get primary image URL
  const getImageUrl = () => {
    const images = extractArray<VehicleImage>(vehicle.images)
    if (!images || images.length === 0) return ''

    const primary = images.find((img) => img.isPrimary) || images[0]
    const path = primary.imageUrl

    return path ? `https://localhost:7001/${path}` : ''
  }

  return (
    <Link
      to={`/vehicles/${vehicle.id}`}
      style={{
        textDecoration: 'none',
        color: 'inherit',
        display: 'block',
        maxWidth: '300px',
        margin: '0 auto',
        width: '100%',
      }}
    >
      <div
        style={{
          border: '1px solid #eee',
          borderRadius: '8px',
          overflow: 'hidden',
          boxShadow: '0 2px 4px rgba(0,0,0,0.1)',
          transition: 'transform 0.2s ease-in-out, box-shadow 0.2s ease-in-out',
          height: '100%',
          display: 'flex',
          flexDirection: 'column',
        }}
        className="hover:shadow-lg hover:-translate-y-1"
      >
        <div style={{ height: '200px', overflow: 'hidden' }}>
          <img
            src={getImageUrl()}
            alt={`${vehicle.make} ${vehicle.model}`}
            style={{
              width: '100%',
              height: '100%',
              objectFit: 'cover',
            }}
            onError={(e) => {
              e.currentTarget.style.display = 'none' // optionally hide if broken
            }}
          />
        </div>

        <div
          style={{
            padding: '16px',
            display: 'flex',
            flexDirection: 'column',
            flexGrow: 1,
          }}
        >
          <h3
            style={{ margin: '0 0 8px', fontSize: '1.2rem', fontWeight: 600 }}
          >
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
              fontSize: '0.9rem',
            }}
          >
            <span>{vehicle.mileage.toLocaleString()} km</span>
            {vehicle.fuelType && (
              <span>{getFuelTypeName(vehicle.fuelType)}</span>
            )}
          </div>

          <div style={{ marginTop: 'auto', paddingTop: '12px' }}>
            <div
              style={{
                display: 'inline-block',
                backgroundColor: '#1976d2',
                color: 'white',
                padding: '8px 16px',
                borderRadius: '4px',
                textAlign: 'center',
                width: '100%',
                fontWeight: 500,
              }}
            >
              View Details
            </div>
          </div>
        </div>
      </div>
    </Link>
  )
}

export default VehicleCard
