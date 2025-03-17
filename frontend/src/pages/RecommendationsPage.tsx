import { useContext, useState } from 'react'
import { Navigate } from 'react-router-dom'
import { AuthContext } from '../contexts/AuthContext'
import VehicleRecommendations from '../components/vehicles/VehicleRecommendations'
import ChatInterface from '../components/chat/ChatInterface'

// Types for the chat component integration
interface Vehicle {
  id: number
  make: string
  model: string
  year: number
  price: number
  mileage: number
  fuelType: number
  vehicleType: number
  images?: any
}

interface RecommendationParameters {
  minPrice?: number
  maxPrice?: number
  minYear?: number
  maxYear?: number
  preferredMakes?: string[]
  preferredVehicleTypes?: number[]
  preferredFuelTypes?: number[]
  desiredFeatures?: string[]
}

const RecommendationsPage = () => {
  const { isAuthenticated, loading } = useContext(AuthContext)
  const [activeTab, setActiveTab] = useState<'recommendations' | 'assistant'>(
    'recommendations'
  )
  const [recommendedVehicles, setRecommendedVehicles] = useState<Vehicle[]>([])
  const [parameters, setParameters] = useState<RecommendationParameters>({})

  // Show loading state while checking authentication
  if (loading) {
    return <div className="text-center py-8">Loading...</div>
  }

  // Redirect to login if not authenticated
  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: '/recommendations' }} />
  }

  // Handle recommendations update from chat
  const handleRecommendationsUpdate = (
    vehicles: Vehicle[],
    newParams: RecommendationParameters
  ) => {
    setRecommendedVehicles(vehicles)
    setParameters(newParams)
    setActiveTab('recommendations')
  }

  return (
    <div className="container mx-auto px-4">
      <div className="mb-6">
        <h1 className="text-3xl font-bold">Personalized Recommendations</h1>
        <p className="text-gray-600 mt-2">
          Our AI analyzes your browsing history and preferences to recommend
          vehicles you might like.
        </p>
      </div>

      {/* Tabs Navigation */}
      <div className="mb-6 border-b border-gray-200">
        <ul className="flex flex-wrap -mb-px">
          <li className="mr-2">
            <button
              className={`inline-block py-4 px-4 text-sm font-medium ${
                activeTab === 'recommendations'
                  ? 'text-blue-600 border-b-2 border-blue-600'
                  : 'text-gray-500 hover:text-gray-700'
              }`}
              onClick={() => setActiveTab('recommendations')}
            >
              Recommendations
            </button>
          </li>
          <li className="mr-2">
            <button
              className={`inline-block py-4 px-4 text-sm font-medium ${
                activeTab === 'assistant'
                  ? 'text-blue-600 border-b-2 border-blue-600'
                  : 'text-gray-500 hover:text-gray-700'
              }`}
              onClick={() => setActiveTab('assistant')}
            >
              AI Assistant
            </button>
          </li>
        </ul>
      </div>

      {/* Tab Content */}
      <div className="mb-8">
        {activeTab === 'recommendations' ? (
          recommendedVehicles.length > 0 ? (
            // If we have chat-recommended vehicles, show those
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {recommendedVehicles.map((vehicle) => (
                <div
                  key={vehicle.id}
                  className="bg-white rounded-lg overflow-hidden shadow-md hover:shadow-lg transition-shadow"
                >
                  {/* Add your vehicle card content here */}
                  <div className="p-4">
                    <h3 className="font-semibold text-lg">
                      {vehicle.year} {vehicle.make} {vehicle.model}
                    </h3>
                    <p className="font-bold text-blue-600 mt-1">
                      €{vehicle.price.toLocaleString()}
                    </p>
                    <a
                      href={`/vehicles/${vehicle.id}`}
                      className="mt-3 inline-block bg-blue-600 text-white px-4 py-2 rounded-md text-sm hover:bg-blue-700"
                    >
                      View Details
                    </a>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            // Otherwise show the standard recommendations component
            <VehicleRecommendations />
          )
        ) : (
          // Show chat interface when on assistant tab
          <div
            className="bg-gray-50 p-4 rounded-lg shadow-sm"
            style={{ height: '600px' }}
          >
            <ChatInterface
              onRecommendationsUpdated={handleRecommendationsUpdate}
            />
          </div>
        )}
      </div>

      {/* If on recommendations tab, show assistant promo */}
      {activeTab === 'recommendations' && (
        <div className="bg-blue-50 p-5 rounded-lg shadow-sm">
          <h2 className="text-xl font-semibold text-blue-800">
            Need help finding your perfect car?
          </h2>
          <p className="mt-2 text-blue-700">
            Our AI assistant can help you discover vehicles based on your
            specific requirements and preferences.
          </p>
          <button
            onClick={() => setActiveTab('assistant')}
            className="mt-4 bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700 transition-colors"
          >
            Chat with AI Assistant
          </button>
        </div>
      )}
    </div>
  )
}

export default RecommendationsPage
