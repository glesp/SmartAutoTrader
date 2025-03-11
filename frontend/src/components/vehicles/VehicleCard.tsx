// src/components/vehicles/VehicleCard.tsx
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
    images: VehicleImage[]
  }
}

const VehicleCard: React.FC<VehicleProps> = ({ vehicle }) => {
  // Find primary image or use first image or fallback
  const primaryImage =
    vehicle.images?.find((img) => img.isPrimary)?.imageUrl ||
    vehicle.images?.[0]?.imageUrl ||
    'https://via.placeholder.com/300x200?text=No+Image'

  return (
    <Link to={`/vehicles/${vehicle.id}`} className="block">
      <div className="bg-white rounded-lg shadow-md overflow-hidden hover:shadow-lg transition-shadow">
        <div className="aspect-w-16 aspect-h-9">
          <img
            src={primaryImage}
            alt={`${vehicle.make} ${vehicle.model}`}
            className="object-cover w-full h-full"
          />
        </div>
        <div className="p-4">
          <h3 className="text-lg font-semibold">
            {vehicle.year} {vehicle.make} {vehicle.model}
          </h3>
          <div className="mt-2 flex justify-between">
            <span className="text-blue-600 font-bold">
              ${vehicle.price.toLocaleString()}
            </span>
            <span className="text-gray-600">
              {vehicle.mileage.toLocaleString()} miles
            </span>
          </div>
        </div>
      </div>
    </Link>
  )
}

export default VehicleCard
