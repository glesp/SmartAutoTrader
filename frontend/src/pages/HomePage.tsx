import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { vehicleService } from '../services/api'
import VehicleCard from '../components/vehicles/VehicleCard'

interface Vehicle {
  id: number
  make: string
  model: string
  year: number
  price: number
  mileage: number
  images: Array<{ id: number; imageUrl: string; isPrimary: boolean }>
}

const HomePage = () => {
  const [featuredVehicles, setFeaturedVehicles] = useState<Vehicle[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const loadFeaturedVehicles = async () => {
      try {
        // Get the latest 4 vehicles
        console.log('Fetching vehicles...');
        const response = await vehicleService.getVehicles({
          pageSize: 4,
          sortBy: 'DateListed',
          ascending: false,
        });
        
        console.log('API response type:', typeof response);
        console.log('Is array?', Array.isArray(response));
        console.log('Raw response:', response);
        
        // Safe check before setting state
        if (Array.isArray(response)) {
          setFeaturedVehicles(response);
        } else {
          console.error('Response is not an array:', response);
          setFeaturedVehicles([]); // Use empty array as fallback
        }
      } catch (error) {
        console.error('Error loading featured vehicles:', error);
        setFeaturedVehicles([]);
      } finally {
        setLoading(false);
      }
    };
  
    loadFeaturedVehicles();
  }, []);

  return (
    <div>
      <section className="hero bg-blue-600 text-white py-16">
        <div className="container mx-auto px-4">
          <h1 className="text-4xl font-bold mb-4">
            Welcome to Smart Auto Trader
          </h1>
          <p className="text-xl mb-8">
            Find your perfect vehicle with our AI-powered recommendations
          </p>
          <Link
            to="/vehicles"
            className="bg-white text-blue-600 px-6 py-3 rounded-lg font-semibold hover:bg-blue-50 transition-colors"
          >
            Browse Vehicles
          </Link>
        </div>
      </section>

      <section className="py-12">
        <div className="container mx-auto px-4">
          <h2 className="text-3xl font-bold mb-8 text-center">
            Featured Vehicles
          </h2>

          {loading ? (
            <p className="text-center">Loading featured vehicles...</p>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
              {featuredVehicles.map((vehicle) => (
                <VehicleCard key={vehicle.id} vehicle={vehicle} />
              ))}
            </div>
          )}

          <div className="text-center mt-8">
            <Link
              to="/vehicles"
              className="inline-block border border-blue-600 text-blue-600 px-6 py-3 rounded-lg font-semibold hover:bg-blue-50 transition-colors"
            >
              View All Vehicles
            </Link>
          </div>
        </div>
      </section>
    </div>
  )
}

export default HomePage
