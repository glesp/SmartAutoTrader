import { Link } from 'react-router-dom'

interface VehicleImage {
  id: number
  imageUrl: string
  isPrimary: boolean
}

interface VehicleProps {
  vehicle: {
    id: number
    make: string
    model: string
    year: number
    price: number
    mileage: number
    fuelType?: number | string
    vehicleType?: number | string
    images: VehicleImage[] | { $values: VehicleImage[] } | any
  }
}

// Helper function to extract arrays from ASP.NET response format
const extractArray = <T,>(
  data: T[] | { $values: T[] } | undefined | any
): T[] => {
  if (!data) return []
  if (Array.isArray(data)) return data
  if (data && '$values' in data) return data.$values
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
    const images = extractArray(vehicle.images)
    if (images.length === 0)
      return 'https://via.placeholder.com/300x200?text=No+Image'

    const primaryImage = images.find((img) => img && img.isPrimary === true)
    return (
      primaryImage?.imageUrl ||
      images[0]?.imageUrl ||
      'https://via.placeholder.com/300x200?text=No+Image'
    )
  }

  return (
    <Link
      to={`/vehicles/${vehicle.id}`}
      style={{
        textDecoration: 'none',
        color: 'inherit',
        display: 'block',
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
              e.currentTarget.src =
                'https://via.placeholder.com/300x200?text=No+Image'
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
