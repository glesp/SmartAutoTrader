export interface VehicleImage {
  id: number;
  imageUrl: string;
  isPrimary: boolean;
}

// Define a reusable type for the ASP.NET Core reference format
export interface ReferenceWrapper<T> {
  $id?: string;
  $values: T[];
}

export interface Vehicle {
  id: number;
  make: string;
  model: string;
  year: number;
  price: number;
  mileage: number;
  fuelType: number | string;
  vehicleType: number | string;
  transmission?: string;
  description?: string;
  images?: VehicleImage[] | ReferenceWrapper<VehicleImage>;
}

// Add the RecommendationParameters interface here
export interface RecommendationParameters {
  minPrice?: number;
  maxPrice?: number;
  minYear?: number;
  maxYear?: number;
  maxMileage?: number;
  preferredMakes?: string[];
  preferredVehicleTypes?: string[] | number[];
  preferredFuelTypes?: string[] | number[];
  desiredFeatures?: string[];
  matchedCategory?: string;
}

// Interface for component props
export interface VehicleRecommendationsProps {
  recommendedVehicles?: Vehicle[] | ReferenceWrapper<Vehicle>;
  parameters?: RecommendationParameters;
}

export interface VehicleProps {
  vehicle: Vehicle;
}
