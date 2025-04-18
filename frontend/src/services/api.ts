import axios from 'axios';

// Basic types that match backend models
export interface Vehicle {
  id: number;
  make: string;
  model: string;
  year: number;
  price: number;
  mileage: number;
  fuelType: string; // This is a string, not a number
  transmission: string; // This is a string, not a number
  vehicleType: string; // This is a string, not a number
  description: string;
  images: Array<{ id: number; imageUrl: string; isPrimary: boolean }>;
}

// Simple params type for filtering
type VehicleParams = Record<string, string | number | boolean | undefined>;

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
    const response = await api.post('/auth/login', { email, password });
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
    const response = await api.post('/auth/register', userData);
    return response.data;
  },
};

//vehicle service
export const vehicleService = {
  getVehicles: async (params?: VehicleParams): Promise<Vehicle[]> => {
    try {
      const response = await api.get('/vehicles', { params });

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

  // Existing method: fetch a single vehicle by ID
  getVehicle: async (id: number) => {
    const response = await api.get<Vehicle>(`/vehicles/${id}`);
    return response.data;
  },

  getAvailableMakes: async (): Promise<string[]> => {
    try {
      const response = await api.get('/vehicles/available-makes');
      return response.data;
    } catch (error) {
      console.error('Error fetching makes:', error);
      return [];
    }
  },

  getAvailableModels: async (make: string): Promise<string[]> => {
    try {
      const response = await api.get('/vehicles/available-models', {
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
      const response = await api.get('/vehicles/year-range');
      return response.data;
    } catch (error) {
      console.error('Error fetching year range:', error);
      return { min: 1990, max: new Date().getFullYear() };
    }
  },
};

// Favorites service
export const favoriteService = {
  getFavorites: async () => {
    const response = await api.get<Vehicle[]>('/favorites');
    return response.data;
  },
  addFavorite: async (vehicleId: number) => {
    const response = await api.post(`/favorites/${vehicleId}`);
    return response.data;
  },
  removeFavorite: async (vehicleId: number) => {
    const response = await api.delete(`/favorites/${vehicleId}`);
    return response.data;
  },
  checkFavorite: async (vehicleId: number) => {
    const response = await api.get<boolean>(`/favorites/check/${vehicleId}`);
    return response.data;
  },
  getFavoritesCount: async () => {
    const response = await api.get<number>('/favorites/count');
    return response.data;
  },
};

// Inquiry service
export const inquiryService = {
  getInquiries: async () => {
    const response = await api.get('/inquiries');
    return response.data;
  },
  getInquiry: async (id: number) => {
    const response = await api.get(`/inquiries/${id}`);
    return response.data;
  },
  createInquiry: async (inquiryData: {
    vehicleId: number;
    subject: string;
    message: string;
  }) => {
    const response = await api.post('/inquiries', inquiryData);
    return response.data;
  },
  closeInquiry: async (id: number) => {
    await api.put(`/inquiries/${id}/close`);
  },
  getAllInquiries: async (status = '') => {
    const response = await api.get(
      `/inquiries/admin${status ? `?status=${status}` : ''}`
    );
    return response.data;
  },
  markInquiryAsRead: async (id) => {
    await api.put(`/inquiries/${id}/MarkAsRead`);
  },
  replyToInquiry: async (id, replyData) => {
    await api.put(`/inquiries/${id}/Reply`, replyData);
  },
};

export default api;
