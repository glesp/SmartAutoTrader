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

// Interface for component props
export interface VehicleRecommendationsProps {
  recommendedVehicles?: Vehicle[] | ReferenceWrapper<Vehicle>;
  parameters?: {
    [key: string]: string | number | boolean;
  };
}

export interface VehicleProps {
  vehicle: Vehicle;
}
