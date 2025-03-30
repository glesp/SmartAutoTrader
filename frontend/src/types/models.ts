export interface VehicleImage {
  id: number
  imageUrl: string
  isPrimary: boolean
}

export interface Vehicle {
  id: number
  make: string
  model: string
  year: number
  price: number
  mileage: number
  fuelType: number | string
  vehicleType: number | string
  images?: VehicleImage[] | { $values: VehicleImage[] }
}

// Interface for component props
export interface VehicleRecommendationsProps {
  recommendedVehicles?: Vehicle[] | { $values: Vehicle[] }
  parameters?: any
}

export interface VehicleProps {
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
