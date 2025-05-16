import axios from 'axios';

// Basic types that match backend models
export interface Vehicle {
  id: number;
  make: string;
  model: string;
  year: number;
  price: number;
  mileage: number;
  fuelType: string; // Assuming this is already string from your frontend enum
  transmission: string; // Assuming this is already string
  vehicleType: string; // Assuming this is already string
  description: string;
  images: Array<{ id: number; imageUrl: string; isPrimary: boolean }>;
  engineSize?: number;
  horsePower?: number;
  country?: string;
  features?: Array<{ name: string }>; // Add if not present
  // ... any other properties
}

export interface VehicleFeaturePayload {
  name: string;
}

export interface VehicleCreatePayload {
  make: string;
  model: string;
  year: number;
  price: number;
  mileage?: number;
  fuelType: string;
  transmission: string;
  vehicleType: string;
  engineSize?: number;
  horsePower?: number;
  country?: string;
  description: string;
  features: VehicleFeaturePayload[];
}

export interface UpdateVehiclePayload {
  make: string;
  model: string;
  year: number;
  price: number;
  mileage?: number;
  fuelType: string;
  transmission: string;
  vehicleType: string;
  engineSize?: number;
  horsePower?: number;
  country?: string;
  description: string;
  features: VehicleFeaturePayload[];
}

// Simple params type for filtering
type VehicleParams = Record<
  string,
  string | number | boolean | undefined | string[]
>;

const API_URL = import.meta.env.VITE_API_URL || '/api';

// Create axios instance
const api = axios.create({
  baseURL: API_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Add a request interceptor to attach the JWT token
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Authentication service
export const authService = {
  login: async (email: string, password: string) => {
    const response = await api.post('/api/auth/login', { email, password });
    return response.data;
  },
  register: async (userData: {
    username: string;
    email: string;
    password: string;
    firstName?: string;
    lastName?: string;
    phoneNumber?: string;
  }) => {
    const response = await api.post('/api/auth/register', userData);
    return response.data;
  },
};

//vehicle service
export const vehicleService = {
  getVehicles: async (params?: VehicleParams): Promise<Vehicle[]> => {
    try {
      const response = await api.get('/api/vehicles', { params });

      if (response.data?.$values && Array.isArray(response.data.$values)) {
        return response.data.$values;
      } else if (Array.isArray(response.data)) {
        return response.data;
      } else {
        console.error('API did not return an array:', response.data);
        return [];
      }
    } catch (error) {
      console.error('Error fetching vehicles:', error);
      return [];
    }
  },

  getVehicle: async (id: number): Promise<Vehicle> => {
    const response = await api.get<Vehicle>(`/api/vehicles/${id}`);
    return response.data;
  },

  createVehicle: async (
    vehicleData: VehicleCreatePayload
  ): Promise<Vehicle> => {
    const response = await api.post<Vehicle>('/api/vehicles', vehicleData);
    return response.data;
  },

  updateVehicle: async (
    id: number,
    vehicleData: UpdateVehiclePayload
  ): Promise<void> => {
    // Or Promise<Vehicle> if your backend returns the updated vehicle
    await api.put(`/api/vehicles/${id}`, vehicleData);
  },

  uploadVehicleImage: async (
    vehicleId: number,
    imageFile: File
  ): Promise<{ id: number; imageUrl: string; isPrimary: boolean }> => {
    const formData = new FormData();
    formData.append('imageFile', imageFile);
    // The backend endpoint is /api/Vehicles/{vehicleId}/images
    const response = await api.post<{
      id: number;
      imageUrl: string;
      isPrimary: boolean;
    }>(`/api/vehicles/${vehicleId}/images`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },

  deleteVehicleImage: async (
    vehicleId: number,
    imageId: number
  ): Promise<void> => {
    await api.delete(`/api/vehicles/${vehicleId}/images/${imageId}`);
  },

  setPrimaryVehicleImage: async (
    vehicleId: number,
    imageId: number
  ): Promise<void> => {
    await api.put(`/api/vehicles/${vehicleId}/images/${imageId}/primary`);
  },

  getAvailableMakes: async (): Promise<string[]> => {
    try {
      const response = await api.get('/api/vehicles/available-makes');
      return response.data;
    } catch (error) {
      console.error('Error fetching makes:', error);
      return [];
    }
  },

  getAvailableModels: async (make: string): Promise<string[]> => {
    try {
      const response = await api.get('/api/vehicles/available-models', {
        params: { make },
      });
      return response.data;
    } catch (error) {
      console.error('Error fetching models:', error);
      return [];
    }
  },

  getYearRange: async (): Promise<{ min: number; max: number }> => {
    try {
      const response = await api.get('/api/vehicles/year-range');
      return response.data;
    } catch (error) {
      console.error('Error fetching year range:', error);
      return { min: 1990, max: new Date().getFullYear() };
    }
  },

  getEngineSizeRange: async (): Promise<{ min: number; max: number }> => {
    try {
      const response = await api.get('/api/vehicles/engine-size-range');
      return response.data;
    } catch (error) {
      console.error('Error fetching engine size range:', error);
      return { min: 0, max: 8 }; // Default fallback values
    }
  },

  getHorsepowerRange: async (): Promise<{ min: number; max: number }> => {
    try {
      const response = await api.get('/api/vehicles/horsepower-range');
      return response.data;
    } catch (error) {
      console.error('Error fetching horsepower range:', error);
      return { min: 0, max: 800 }; // Default fallback values
    }
  },
};

// Favorites service
export const favoriteService = {
  getFavorites: async () => {
    const response = await api.get<Vehicle[]>('/api/favorites');
    return response.data;
  },
  addFavorite: async (vehicleId: number) => {
    const response = await api.post(`/api/favorites/${vehicleId}`);
    return response.data;
  },
  removeFavorite: async (vehicleId: number) => {
    const response = await api.delete(`/api/favorites/${vehicleId}`);
    return response.data;
  },
  checkFavorite: async (vehicleId: number) => {
    const response = await api.get<boolean>(
      `/api/favorites/check/${vehicleId}`
    );
    return response.data;
  },
  getFavoritesCount: async () => {
    const response = await api.get<number>('/api/favorites/count');
    return response.data;
  },
};

// Inquiry service
interface InquiryReplyData {
  response: string;
}

export const inquiryService = {
  getInquiries: async () => {
    const response = await api.get('/api/inquiries');
    return response.data;
  },
  getInquiry: async (id: number) => {
    const response = await api.get(`/api/inquiries/${id}`);
    return response.data;
  },
  createInquiry: async (inquiryData: {
    vehicleId: number;
    subject: string;
    message: string;
  }) => {
    const response = await api.post('/api/inquiries', inquiryData);
    return response.data;
  },
  closeInquiry: async (id: number) => {
    await api.put(`/api/inquiries/${id}/close`);
  },
  getAllInquiries: async (status = '') => {
    const response = await api.get(
      `/api/inquiries/admin${status ? `?status=${status}` : ''}`
    );
    return response.data;
  },
  markInquiryAsRead: async (id: number) => {
    await api.put(`/api/inquiries/${id}/MarkAsRead`);
  },
  replyToInquiry: async (id: number, replyData: InquiryReplyData) => {
    await api.put(`/api/inquiries/${id}/Reply`, replyData);
  },
};

export default api;
