// src/pages/ProfilePage.tsx
import { useState, useEffect, useContext } from 'react'
import { Link, Navigate } from 'react-router-dom'
import { AuthContext } from '../contexts/AuthContext'
import { favoriteService, inquiryService } from '../services/api'
import VehicleCard from '../components/vehicles/VehicleCard'

// Define VehicleImage interface
interface VehicleImage {
  id: number
  imageUrl: string
  isPrimary: boolean
}

// Define ReferenceWrapper for ASP.NET serialization
interface ReferenceWrapper<T> {
  $id?: string
  $values: T[]
}

// Define Vehicle interface
interface Vehicle {
  id: number
  make: string
  model: string
  year: number
  price: number
  mileage: number
  images: VehicleImage[] | ReferenceWrapper<VehicleImage> | any
}

// Define Inquiry interface
interface Inquiry {
  id: number
  vehicleId: number
  subject: string
  message: string
  response?: string
  dateSent: string
  dateReplied?: string
  status: string
  vehicle?: Vehicle
}

// Define what the arrays might look like with ASP.NET serialization
type SerializedData<T> = T[] | ReferenceWrapper<T> | any

// Helper function to extract arrays from ASP.NET reference format
const extractArray = <T,>(data: SerializedData<T>): T[] => {
  if (!data) return []

  if (Array.isArray(data)) {
    return data
  } else if (typeof data === 'object' && data !== null && '$values' in data) {
    return (data as ReferenceWrapper<T>).$values
  }

  return []
}

const ProfilePage = () => {
  const {
    user,
    isAuthenticated,
    loading: authLoading,
  } = useContext(AuthContext)
  const [favoriteVehicles, setFavoriteVehicles] = useState<
    SerializedData<Vehicle>
  >([])
  const [inquiries, setInquiries] = useState<SerializedData<Inquiry>>([])
  const [activeTab, setActiveTab] = useState('favorites')
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const fetchUserData = async () => {
      if (!isAuthenticated) return

      setLoading(true)

      try {
        if (activeTab === 'favorites') {
          const favorites = await favoriteService.getFavorites()
          setFavoriteVehicles(favorites)
        } else if (activeTab === 'inquiries') {
          const userInquiries = await inquiryService.getInquiries()
          setInquiries(userInquiries)
        }
      } catch (error) {
        console.error(`Error fetching ${activeTab}:`, error)
      } finally {
        setLoading(false)
      }
    }

    fetchUserData()
  }, [isAuthenticated, activeTab])

  // Redirect if not authenticated
  if (!authLoading && !isAuthenticated) {
    return <Navigate to="/login" state={{ from: '/profile' }} />
  }

  if (authLoading) {
    return <div className="text-center py-12">Loading profile...</div>
  }

  // Extract arrays from potentially reference-wrapped data
  const favoritesArray = extractArray<Vehicle>(favoriteVehicles)
  const inquiriesArray = extractArray<Inquiry>(inquiries)

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="bg-white rounded-lg shadow-md overflow-hidden">
        {/* Profile header */}
        <div className="bg-blue-600 px-6 py-8 text-white">
          <h1 className="text-2xl font-bold mb-2">My Profile</h1>
          <p>
            {user?.firstName} {user?.lastName} ({user?.username})
          </p>
          <p className="text-blue-100">{user?.email}</p>
        </div>

        {/* Tabs */}
        <div className="border-b">
          <nav className="flex">
            <button
              onClick={() => setActiveTab('favorites')}
              className={`px-6 py-4 font-medium ${
                activeTab === 'favorites'
                  ? 'border-b-2 border-blue-600 text-blue-600'
                  : 'text-gray-500 hover:text-gray-700'
              }`}
            >
              My Favorites
            </button>
            <button
              onClick={() => setActiveTab('inquiries')}
              className={`px-6 py-4 font-medium ${
                activeTab === 'inquiries'
                  ? 'border-b-2 border-blue-600 text-blue-600'
                  : 'text-gray-500 hover:text-gray-700'
              }`}
            >
              My Inquiries
            </button>
          </nav>
        </div>

        {/* Tab content */}
        <div className="p-6">
          {/* Favorites tab */}
          {activeTab === 'favorites' && (
            <>
              {loading ? (
                <div className="text-center py-8">
                  Loading your favorites...
                </div>
              ) : favoritesArray.length === 0 ? (
                <div className="text-center py-8">
                  <p className="text-gray-600 mb-4">
                    You haven't added any vehicles to your favorites yet.
                  </p>
                  <Link
                    to="/vehicles"
                    className="inline-block bg-blue-600 text-white px-6 py-2 rounded-md hover:bg-blue-700"
                  >
                    Browse Vehicles
                  </Link>
                </div>
              ) : (
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                  {favoritesArray.map((vehicle: Vehicle) => (
                    <VehicleCard key={vehicle.id} vehicle={vehicle} />
                  ))}
                </div>
              )}
            </>
          )}

          {/* Inquiries tab */}
          {activeTab === 'inquiries' && (
            <>
              {loading ? (
                <div className="text-center py-8">
                  Loading your inquiries...
                </div>
              ) : inquiriesArray.length === 0 ? (
                <div className="text-center py-8">
                  <p className="text-gray-600 mb-4">
                    You haven't sent any inquiries yet.
                  </p>
                  <Link
                    to="/vehicles"
                    className="inline-block bg-blue-600 text-white px-6 py-2 rounded-md hover:bg-blue-700"
                  >
                    Browse Vehicles
                  </Link>
                </div>
              ) : (
                <div className="space-y-4">
                  {inquiriesArray.map((inquiry: Inquiry) => (
                    <div
                      key={inquiry.id}
                      className="border rounded-lg overflow-hidden"
                    >
                      <div className="bg-gray-50 px-4 py-3 border-b">
                        <div className="flex justify-between items-center">
                          <h3 className="font-semibold">{inquiry.subject}</h3>
                          <span
                            className={`px-2 py-1 rounded-full text-xs font-medium ${
                              inquiry.status === 'New'
                                ? 'bg-blue-100 text-blue-800'
                                : inquiry.status === 'Read'
                                  ? 'bg-yellow-100 text-yellow-800'
                                  : inquiry.status === 'Replied'
                                    ? 'bg-green-100 text-green-800'
                                    : 'bg-gray-100 text-gray-800'
                            }`}
                          >
                            {inquiry.status}
                          </span>
                        </div>
                        <p className="text-sm text-gray-500">
                          {new Date(inquiry.dateSent).toLocaleDateString()} •
                          {inquiry.vehicle &&
                            ` regarding ${inquiry.vehicle.year} ${inquiry.vehicle.make} ${inquiry.vehicle.model}`}
                        </p>
                      </div>
                      <div className="p-4">
                        <div className="mb-4">
                          <h4 className="text-sm font-medium text-gray-500 mb-1">
                            Your message:
                          </h4>
                          <p className="text-gray-700">{inquiry.message}</p>
                        </div>

                        {inquiry.response && (
                          <div className="bg-blue-50 p-4 rounded-md">
                            <h4 className="text-sm font-medium text-blue-700 mb-1">
                              Response:
                            </h4>
                            <p className="text-gray-700">{inquiry.response}</p>
                            <p className="text-xs text-gray-500 mt-2">
                              Replied on{' '}
                              {inquiry.dateReplied &&
                                new Date(
                                  inquiry.dateReplied
                                ).toLocaleDateString()}
                            </p>
                          </div>
                        )}

                        {inquiry.status !== 'Closed' && (
                          <div className="mt-4 text-right">
                            <button
                              onClick={async () => {
                                try {
                                  await inquiryService.closeInquiry(inquiry.id)
                                  setInquiries((prev: any) => {
                                    const prevArray =
                                      extractArray<Inquiry>(prev)
                                    return prevArray.map((i) =>
                                      i.id === inquiry.id
                                        ? { ...i, status: 'Closed' }
                                        : i
                                    )
                                  })
                                } catch (error) {
                                  console.error('Error closing inquiry:', error)
                                }
                              }}
                              className="text-sm text-gray-600 hover:text-gray-800"
                            >
                              Mark as Closed
                            </button>
                          </div>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </>
          )}
        </div>
      </div>
    </div>
  )
}

export default ProfilePage
