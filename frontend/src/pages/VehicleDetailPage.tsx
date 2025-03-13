// src/pages/VehicleDetailPage.tsx
import { useState, useEffect, useContext } from 'react'
import { useParams, Link } from 'react-router-dom'
import { vehicleService, favoriteService } from '../services/api'
import { AuthContext } from '../contexts/AuthContext'

// Define what your API actually returns
interface VehicleImage {
  id: number
  imageUrl: string
  isPrimary: boolean
}

// Define a type for the ASP.NET Core reference format
interface ReferenceWrapper {
  $id?: string
  $values: VehicleImage[]
}

interface ApiVehicle {
  id: number
  make: string
  model: string
  year: number
  price: number
  mileage: number
  fuelType: string
  transmission: string
  vehicleType: string
  description: string
  // Define more precisely to help TypeScript
  images: VehicleImage[] | ReferenceWrapper | any
}

const VehicleDetailPage = () => {
  const { id } = useParams<{ id: string }>()
  const [vehicle, setVehicle] = useState<ApiVehicle | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [activeImageIndex, setActiveImageIndex] = useState(0)
  const [isFavorite, setIsFavorite] = useState(false)
  const [checkingFavorite, setCheckingFavorite] = useState(false)
  const { isAuthenticated } = useContext(AuthContext)

  useEffect(() => {
    const fetchVehicle = async () => {
      if (!id) return

      setLoading(true)
      setError(null)

      try {
        const data = await vehicleService.getVehicle(parseInt(id))
        setVehicle(data)

        // Handle both array formats for images
        let imageArray: VehicleImage[] = []

        // Check if images exists
        if (data.images) {
          // Check if images is an array
          if (Array.isArray(data.images)) {
            imageArray = data.images
          }
          // Check if images has $values property using a type guard
          else if (
            typeof data.images === 'object' &&
            data.images !== null &&
            '$values' in data.images
          ) {
            // Use a safe type assertion
            const imagesWithValues = data.images as { $values: VehicleImage[] }
            imageArray = imagesWithValues.$values
          }
        }

        // Set primary image as active
        if (imageArray.length > 0) {
          const primaryIndex = imageArray.findIndex((img) => img.isPrimary)
          setActiveImageIndex(primaryIndex >= 0 ? primaryIndex : 0)
        }
      } catch (err) {
        console.error('Error fetching vehicle:', err)
        setError('Failed to load vehicle details')
      } finally {
        setLoading(false)
      }
    }

    fetchVehicle()
  }, [id])

  useEffect(() => {
    // Check if vehicle is in favorites
    const checkFavorite = async () => {
      if (!isAuthenticated || !id) return

      setCheckingFavorite(true)
      try {
        const favoriteStatus = await favoriteService.checkFavorite(parseInt(id))
        setIsFavorite(favoriteStatus)
      } catch (err) {
        console.error('Error checking favorite status:', err)
      } finally {
        setCheckingFavorite(false)
      }
    }

    checkFavorite()
  }, [id, isAuthenticated])

  const handleToggleFavorite = async () => {
    if (!isAuthenticated || !id) return

    try {
      if (isFavorite) {
        await favoriteService.removeFavorite(parseInt(id))
      } else {
        await favoriteService.addFavorite(parseInt(id))
      }
      setIsFavorite(!isFavorite)
    } catch (err) {
      console.error('Error toggling favorite:', err)
    }
  }

  // Helper function to get image array regardless of format
  const getImageArray = (): VehicleImage[] => {
    if (!vehicle) return []

    if (Array.isArray(vehicle.images)) {
      return vehicle.images
    } else if (
      typeof vehicle.images === 'object' &&
      vehicle.images !== null &&
      '$values' in vehicle.images
    ) {
      // Use a safe type assertion
      const imagesWithValues = vehicle.images as { $values: VehicleImage[] }
      return imagesWithValues.$values
    }
    return []
  }

  // Helper function to get image URL with fallback
  const getImageUrl = (image: VehicleImage | undefined) => {
    if (!image || !image.imageUrl) {
      return `https://via.placeholder.com/800x450/3498db/ffffff?text=No+Image`
    }

    const isPlaceholderUrl = image.imageUrl.includes('placeholder.com/vehicles')
    if (isPlaceholderUrl && vehicle) {
      return `https://via.placeholder.com/800x450/3498db/ffffff?text=${vehicle.make}+${vehicle.model}`
    }

    return image.imageUrl
  }

  if (loading) {
    return (
      <div className="container mx-auto px-4 py-8">
        <div className="text-center py-12">Loading vehicle details...</div>
      </div>
    )
  }

  if (error || !vehicle) {
    return (
      <div className="container mx-auto px-4 py-8">
        <div className="bg-red-50 border border-red-200 text-red-800 rounded-lg p-4 text-center">
          {error || 'Vehicle not found'}
          <div className="mt-4">
            <Link
              to="/vehicles"
              className="inline-block bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700"
            >
              Back to Vehicles
            </Link>
          </div>
        </div>
      </div>
    )
  }

  const imageArray = getImageArray()

  return (
    <div className="container mx-auto px-4 py-8">
      {/* Breadcrumb */}
      <div className="text-sm text-gray-500 mb-6">
        <Link to="/" className="hover:text-blue-600">
          Home
        </Link>{' '}
        &gt;
        <Link to="/vehicles" className="hover:text-blue-600 mx-1">
          Vehicles
        </Link>{' '}
        &gt;
        <span className="text-gray-700">
          {vehicle.year} {vehicle.make} {vehicle.model}
        </span>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-5 gap-8">
        {/* Images and details */}
        <div className="lg:col-span-3">
          {/* Main image */}
          <div className="relative mb-4 bg-gray-100 rounded-lg overflow-hidden aspect-w-16 aspect-h-9">
            {imageArray.length > 0 ? (
              <img
                src={getImageUrl(imageArray[activeImageIndex])}
                alt={`${vehicle.make} ${vehicle.model}`}
                className="object-cover w-full h-full"
              />
            ) : (
              <div className="flex items-center justify-center h-full">
                <span className="text-gray-400">No image available</span>
              </div>
            )}
          </div>

          {/* Thumbnail images */}
          {imageArray.length > 1 && (
            <div className="grid grid-cols-5 gap-2">
              {imageArray.map((image, index) => (
                <button
                  key={image.id}
                  onClick={() => setActiveImageIndex(index)}
                  className={`border-2 rounded overflow-hidden aspect-w-1 aspect-h-1 ${
                    index === activeImageIndex
                      ? 'border-blue-500'
                      : 'border-transparent hover:border-gray-300'
                  }`}
                >
                  <img
                    src={getImageUrl(image)}
                    alt={`${vehicle.make} ${vehicle.model} thumbnail ${index + 1}`}
                    className="object-cover w-full h-full"
                  />
                </button>
              ))}
            </div>
          )}

          {/* Description */}
          <div className="mt-8">
            <h2 className="text-xl font-semibold mb-4">Description</h2>
            <div className="bg-white rounded-lg shadow p-4">
              <p className="text-gray-700 whitespace-pre-line">
                {vehicle.description}
              </p>
            </div>
          </div>
        </div>

        {/* Sidebar with summary and actions */}
        <div className="lg:col-span-2">
          <div className="bg-white rounded-lg shadow p-6 sticky top-4">
            <h1 className="text-2xl font-bold mb-2">
              {vehicle.year} {vehicle.make} {vehicle.model}
            </h1>

            <div className="text-3xl font-bold text-blue-600 mb-4">
              ${vehicle.price.toLocaleString()}
            </div>

            <div className="mb-6 space-y-2">
              <div className="flex justify-between text-gray-700">
                <span>Mileage:</span>
                <span className="font-semibold">
                  {vehicle.mileage.toLocaleString()} miles
                </span>
              </div>
              <div className="flex justify-between text-gray-700">
                <span>Fuel Type:</span>
                <span className="font-semibold">{vehicle.fuelType}</span>
              </div>
              <div className="flex justify-between text-gray-700">
                <span>Transmission:</span>
                <span className="font-semibold">{vehicle.transmission}</span>
              </div>
              <div className="flex justify-between text-gray-700">
                <span>Body Type:</span>
                <span className="font-semibold">{vehicle.vehicleType}</span>
              </div>
            </div>

            {/* Actions */}
            <div className="space-y-3">
              <Link
                to={`/inquiries/new?vehicleId=${vehicle.id}`}
                className="block w-full bg-blue-600 text-white text-center py-3 px-4 rounded-lg font-semibold hover:bg-blue-700"
              >
                Send Inquiry
              </Link>

              {isAuthenticated ? (
                <button
                  onClick={handleToggleFavorite}
                  disabled={checkingFavorite}
                  className={`block w-full py-3 px-4 rounded-lg font-semibold border ${
                    isFavorite
                      ? 'bg-red-50 text-red-600 border-red-200 hover:bg-red-100'
                      : 'bg-gray-50 text-gray-700 border-gray-200 hover:bg-gray-100'
                  }`}
                >
                  {checkingFavorite
                    ? 'Loading...'
                    : isFavorite
                      ? 'Remove from Favorites'
                      : 'Add to Favorites'}
                </button>
              ) : (
                <Link
                  to="/login"
                  className="block w-full bg-gray-50 text-gray-700 text-center py-3 px-4 rounded-lg font-semibold border border-gray-200 hover:bg-gray-100"
                >
                  Login to Save to Favorites
                </Link>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}

export default VehicleDetailPage
