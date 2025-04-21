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

// Updated RecommendationParameters interface with negated criteria
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

  // Added negated criteria fields from backend
  explicitlyNegatedMakes?: string[]; // Matches C# naming
  explicitlyNegatedVehicleTypes?: string[] | number[]; // Matches C# naming
  explicitlyNegatedFuelTypes?: string[] | number[]; // Matches C# naming

  // Additional negated fields that might come from the backend
  rejectedMakes?: string[];
  rejectedVehicleTypes?: string[] | number[];
  rejectedFuelTypes?: string[] | number[];
  rejectedFeatures?: string[];
  rejectedTransmission?: string | number;

  // For clarification flow
  intent?: string;
  clarificationNeeded?: boolean;
  clarificationNeededFor?: string[];
}

// Add/update FilterState interface for components that need it
export interface FilterState {
  make?: string;
  model?: string;
  minYear?: number;
  maxYear?: number;
  minPrice?: number;
  maxPrice?: number;
  fuelType?: string;
  transmission?: string;
  vehicleType?: string;
  minEngineSize?: number;
  maxEngineSize?: number;
  minHorsepower?: number;
  maxHorsepower?: number;
  sortBy: string;
  ascending: boolean;

  // Add negated criteria fields for filtering UI
  rejectedMakes?: string[];
  rejectedVehicleTypes?: string[];
  rejectedFuelTypes?: string[];
  rejectedFeatures?: string[];
}

// Interface for component props
export interface VehicleRecommendationsProps {
  recommendedVehicles?: Vehicle[] | ReferenceWrapper<Vehicle>;
  parameters?: RecommendationParameters;
}

export interface VehicleProps {
  vehicle: Vehicle;
}
