// src/components/vehicles/VehicleFilters.tsx
import { useState, useEffect } from 'react'

interface FilterProps {
  filters: {
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
  onFilterChange: (filters: Partial<FilterProps['filters']>) => void
}

const VehicleFilters = ({ filters, onFilterChange }: FilterProps) => {
  const [localFilters, setLocalFilters] = useState(filters)

  // Update local state when props change
  useEffect(() => {
    setLocalFilters(filters)
  }, [filters])

  const handleInputChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>
  ) => {
    const { name, value, type } = e.target as HTMLInputElement

    let parsedValue: string | number | boolean | undefined = value

    // Convert numeric values
    if (type === 'number' && value) {
      parsedValue = Number(value)
    }

    // Handle empty values
    if (value === '') {
      parsedValue = undefined
    }

    setLocalFilters((prev) => ({
      ...prev,
      [name]: parsedValue,
    }))
  }

  const handleSortChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const { value } = e.target
    setLocalFilters((prev) => ({
      ...prev,
      sortBy: value,
    }))
  }

  const handleSortDirectionChange = (
    e: React.ChangeEvent<HTMLInputElement>
  ) => {
    setLocalFilters((prev) => ({
      ...prev,
      ascending: e.target.checked,
    }))
  }

  const applyFilters = () => {
    onFilterChange(localFilters)
  }

  const resetFilters = () => {
    const resetValues = {
      make: undefined,
      model: undefined,
      minYear: undefined,
      maxYear: undefined,
      minPrice: undefined,
      maxPrice: undefined,
      fuelType: undefined,
      transmission: undefined,
      vehicleType: undefined,
      sortBy: 'DateListed',
      ascending: false,
    }
    setLocalFilters(resetValues)
    onFilterChange(resetValues)
  }

  return (
    <div className="bg-white rounded-lg shadow-md p-4">
      <h2 className="text-xl font-semibold mb-4">Filters</h2>

      <div className="space-y-4">
        {/* Make */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Make
          </label>
          <input
            type="text"
            name="make"
            value={localFilters.make || ''}
            onChange={handleInputChange}
            className="w-full p-2 border border-gray-300 rounded"
            placeholder="Any make"
          />
        </div>

        {/* Model */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Model
          </label>
          <input
            type="text"
            name="model"
            value={localFilters.model || ''}
            onChange={handleInputChange}
            className="w-full p-2 border border-gray-300 rounded"
            placeholder="Any model"
          />
        </div>

        {/* Year Range */}
        <div className="grid grid-cols-2 gap-2">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Min Year
            </label>
            <input
              type="number"
              name="minYear"
              value={localFilters.minYear || ''}
              onChange={handleInputChange}
              className="w-full p-2 border border-gray-300 rounded"
              placeholder="From"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Max Year
            </label>
            <input
              type="number"
              name="maxYear"
              value={localFilters.maxYear || ''}
              onChange={handleInputChange}
              className="w-full p-2 border border-gray-300 rounded"
              placeholder="To"
            />
          </div>
        </div>

        {/* Price Range */}
        <div className="grid grid-cols-2 gap-2">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Min Price
            </label>
            <input
              type="number"
              name="minPrice"
              value={localFilters.minPrice || ''}
              onChange={handleInputChange}
              className="w-full p-2 border border-gray-300 rounded"
              placeholder="From"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Max Price
            </label>
            <input
              type="number"
              name="maxPrice"
              value={localFilters.maxPrice || ''}
              onChange={handleInputChange}
              className="w-full p-2 border border-gray-300 rounded"
              placeholder="To"
            />
          </div>
        </div>

        {/* Fuel Type */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Fuel Type
          </label>
          <select
            name="fuelType"
            value={localFilters.fuelType || ''}
            onChange={handleInputChange}
            className="w-full p-2 border border-gray-300 rounded"
          >
            <option value="">Any fuel type</option>
            <option value="0">Petrol</option>
            <option value="1">Diesel</option>
            <option value="2">Electric</option>
            <option value="3">Hybrid</option>
            <option value="4">Plugin</option>
          </select>
        </div>

        {/* Transmission */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Transmission
          </label>
          <select
            name="transmission"
            value={localFilters.transmission || ''}
            onChange={handleInputChange}
            className="w-full p-2 border border-gray-300 rounded"
          >
            <option value="">Any transmission</option>
            <option value="0">Manual</option>
            <option value="1">Automatic</option>
            <option value="2">Semi-Automatic</option>
          </select>
        </div>

        {/* Vehicle Type */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Vehicle Type
          </label>
          <select
            name="vehicleType"
            value={localFilters.vehicleType || ''}
            onChange={handleInputChange}
            className="w-full p-2 border border-gray-300 rounded"
          >
            <option value="">Any type</option>
            <option value="0">Sedan</option>
            <option value="1">SUV</option>
            <option value="2">Hatchback</option>
            <option value="3">Estate</option>
            <option value="4">Coupe</option>
            <option value="5">Convertible</option>
            <option value="6">Pickup</option>
            <option value="7">Van</option>
          </select>
        </div>

        {/* Sort By */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Sort By
          </label>
          <select
            name="sortBy"
            value={localFilters.sortBy}
            onChange={handleSortChange}
            className="w-full p-2 border border-gray-300 rounded"
          >
            <option value="DateListed">Date Listed</option>
            <option value="Price">Price</option>
            <option value="Year">Year</option>
            <option value="Mileage">Mileage</option>
            <option value="Make">Make</option>
          </select>
        </div>

        {/* Sort Direction */}
        <div className="flex items-center">
          <input
            type="checkbox"
            id="ascending"
            checked={localFilters.ascending}
            onChange={handleSortDirectionChange}
            className="h-4 w-4 text-blue-600 rounded"
          />
          <label htmlFor="ascending" className="ml-2 text-sm text-gray-700">
            Ascending order
          </label>
        </div>

        {/* Action Buttons */}
        <div className="flex space-x-2 pt-2">
          <button
            onClick={applyFilters}
            className="flex-1 bg-blue-600 text-white py-2 px-4 rounded hover:bg-blue-700"
          >
            Apply
          </button>
          <button
            onClick={resetFilters}
            className="flex-1 bg-gray-200 text-gray-800 py-2 px-4 rounded hover:bg-gray-300"
          >
            Reset
          </button>
        </div>
      </div>
    </div>
  )
}

export default VehicleFilters
