// src/pages/VehicleListingPage.tsx
import { useState, useEffect } from 'react'
import { vehicleService } from '../services/api'
import VehicleCard from '../components/vehicles/VehicleCard'
import VehicleFilters from '../components/vehicles/VehicleFilters'
import { Vehicle } from '../types/models'

interface FilterState {
  make?: string
  model?: string
  minYear?: number
  maxYear?: number
  minPrice?: number
  maxPrice?: number
  fuelType?: string
  transmission?: string
  vehicleType?: string
  sortBy: string
  ascending: boolean
}

const VehicleListingPage = () => {
  const [vehicles, setVehicles] = useState<Vehicle[]>([])
  const [loading, setLoading] = useState(true)
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [filters, setFilters] = useState<FilterState>({
    sortBy: 'DateListed',
    ascending: false,
  })

  useEffect(() => {
    const loadVehicles = async () => {
      setLoading(true)
      try {
        const response = await vehicleService.getVehicles({
          ...filters,
          pageNumber: page,
          pageSize: 8,
        })

        setVehicles(response)

        // Axios response headers need to be accessed differently
        // If your API isn't returning headers correctly, you can adjust this:
        const totalCount = 20 // Default value or calculate from total vehicles
        const calculatedTotalPages = Math.ceil(totalCount / 8)
        setTotalPages(calculatedTotalPages || 1)
      } catch (error) {
        console.error('Error loading vehicles:', error)
      } finally {
        setLoading(false)
      }
    }

    loadVehicles()
  }, [filters, page])

  const handleFilterChange = (newFilters: Partial<FilterState>) => {
    setFilters((prev) => ({ ...prev, ...newFilters }))
    setPage(1) // Reset to first page when filters change
  }

  const handlePageChange = (newPage: number) => {
    setPage(newPage)
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }

  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-3xl font-bold mb-8">Available Vehicles</h1>

      <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
        {/* Filters sidebar */}
        <div className="lg:col-span-1">
          <VehicleFilters
            filters={filters}
            onFilterChange={handleFilterChange}
          />
        </div>

        {/* Vehicle grid */}
        <div className="lg:col-span-3">
          {loading ? (
            <div className="flex justify-center items-center h-64">
              <p>Loading vehicles...</p>
            </div>
          ) : vehicles.length === 0 ? (
            <div className="bg-gray-50 rounded-lg p-8 text-center">
              <h3 className="text-xl font-semibold mb-2">No vehicles found</h3>
              <p className="text-gray-600">
                Try adjusting your filters to see more results.
              </p>
            </div>
          ) : (
            <>
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                {vehicles.map((vehicle) => (
                  <VehicleCard key={vehicle.id} vehicle={vehicle} />
                ))}
              </div>

              {/* Pagination */}
              <div className="flex justify-center mt-8">
                {Array.from({ length: totalPages }, (_, i) => i + 1).map(
                  (pageNum) => (
                    <button
                      key={pageNum}
                      onClick={() => handlePageChange(pageNum)}
                      className={`mx-1 px-4 py-2 rounded ${
                        pageNum === page
                          ? 'bg-blue-600 text-white'
                          : 'bg-gray-200 hover:bg-gray-300'
                      }`}
                    >
                      {pageNum}
                    </button>
                  )
                )}
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  )
}

export default VehicleListingPage
