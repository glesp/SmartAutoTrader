import { useContext } from 'react'
import { Navigate } from 'react-router-dom'
import { AuthContext } from '../contexts/AuthContext'
import VehicleRecommendations from '../components/vehicles/VehicleRecommendations'

const RecommendationsPage = () => {
  const { isAuthenticated, loading } = useContext(AuthContext)

  // Show loading state while checking authentication
  if (loading) {
    return <div className="text-center py-8">Loading...</div>
  }

  // Redirect to login if not authenticated
  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: '/recommendations' }} />
  }

  return (
    <div>
      <div className="mb-6">
        <h1 className="text-3xl font-bold">Personalized Recommendations</h1>
        <p className="text-gray-600 mt-2">
          Our AI analyzes your browsing history and preferences to recommend
          vehicles you might like.
        </p>
      </div>

      <VehicleRecommendations />
    </div>
  )
}

export default RecommendationsPage
