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
    images: VehicleImage[] | any // added 'any' to handle potential non-array format
  }
}

const VehicleCard: React.FC<VehicleProps> = ({ vehicle }) => {
  // Debug logging
  console.log('Vehicle ID:', vehicle.id)
  console.log('Images property:', vehicle.images)
  console.log('Images type:', typeof vehicle.images)
  console.log('Is images an array?', Array.isArray(vehicle.images))

  // Handle different possible formats of the images property
  let primaryImage = 'https://via.placeholder.com/300x200?text=No+Image'

  if (vehicle.images) {
    // If images is already an array
    if (Array.isArray(vehicle.images)) {
      console.log('Images array content:', vehicle.images)
      // Use a more defensive approach
      try {
        const primaryImg = vehicle.images.find(
          (img) => img && img.isPrimary === true
        )
        primaryImage =
          primaryImg?.imageUrl ||
          (vehicle.images[0] && vehicle.images[0].imageUrl) ||
          primaryImage
      } catch (error) {
        console.error('Error processing images array:', error)
      }
    }
    // If images has $values property (ASP.NET reference handling format)
    else if (vehicle.images.$values && Array.isArray(vehicle.images.$values)) {
      console.log('Images $values content:', vehicle.images.$values)
      // Use a more defensive approach
      try {
        // Log first item to see its structure
        if (vehicle.images.$values.length > 0) {
          console.log('First image item:', vehicle.images.$values[0])
        }

        // Check each item before using find
        let primaryImg = null
        for (const img of vehicle.images.$values) {
          if (
            img &&
            typeof img === 'object' &&
            'isPrimary' in img &&
            img.isPrimary === true
          ) {
            primaryImg = img
            break
          }
        }

        primaryImage =
          primaryImg?.imageUrl ||
          (vehicle.images.$values[0] && vehicle.images.$values[0].imageUrl) ||
          primaryImage
      } catch (error) {
        console.error('Error processing images.$values array:', error)
      }
    }
    // Log unexpected format for debugging
    else {
      console.error('Unexpected images format:', vehicle.images)
    }
  }
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
