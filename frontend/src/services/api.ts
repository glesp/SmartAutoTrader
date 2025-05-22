/**
 * @file api.ts
 * @summary Centralized API service module for the Smart Auto Trader application.
 *
 * @description This module configures an Axios instance for making HTTP requests to the backend API.
 * It includes request interceptors for attaching JWT tokens to authenticated requests.
 * It also exports various service objects (authService, vehicleService, favoriteService, inquiryService)
 * that encapsulate API endpoints related to different features of the application.
 *
 * @remarks
 * - Uses Axios for HTTP requests.
 * - Implements a request interceptor to automatically add JWT tokens from localStorage to the Authorization header.
 * - Defines TypeScript interfaces for common data structures (Vehicle, Payloads) to ensure type safety.
 * - API_URL is configurable via environment variables (VITE_API_URL).
 *
 * @dependencies
 * - axios: For making HTTP requests.
 */
import axios from 'axios';

/**
 * @interface Vehicle
 * @summary Represents a vehicle object as defined by the backend.
 *
 * @property {number} id - The unique identifier for the vehicle.
 * @property {string} make - The manufacturer of the vehicle (e.g., Toyota).
 * @property {string} model - The model of the vehicle (e.g., Camry).
 * @property {number} year - The manufacturing year of the vehicle.
 * @property {number} price - The price of the vehicle.
 * @property {number} mileage - The mileage of the vehicle in kilometers.
 * @property {string} fuelType - The type of fuel the vehicle uses (e.g., Petrol, Diesel).
 * @property {string} transmission - The type of transmission (e.g., Automatic, Manual).
 * @property {string} vehicleType - The category or body style of the vehicle (e.g., Sedan, SUV).
 * @property {string} description - A textual description of the vehicle.
 * @property {Array<{ id: number; imageUrl: string; isPrimary: boolean }>} images - A list of images associated with the vehicle.
 * @property {number} [engineSize] - The engine displacement in liters or cubic centimeters.
 * @property {number} [horsePower] - The horsepower of the vehicle's engine.
 * @property {string} [country] - The country of origin or where the vehicle is listed.
 * @property {Array<{ name: string }>} [features] - A list of features the vehicle has.
 */
export interface Vehicle {
  id: number;
  make: string;
  model: string;
  year: number;
  price: number;
  mileage: number;
  fuelType: string;
  transmission: string;
  vehicleType: string;
  description: string;
  images: Array<{ id: number; imageUrl: string; isPrimary: boolean }>;
  engineSize?: number;
  horsePower?: number;
  country?: string;
  features?: Array<{ name: string }>;
}

/**
 * @interface VehicleFeaturePayload
 * @summary Represents the payload for a vehicle feature.
 * @property {string} name - The name of the feature.
 */
export interface VehicleFeaturePayload {
  name: string;
}

/**
 * @interface VehicleCreatePayload
 * @summary Defines the structure for the payload when creating a new vehicle.
 *
 * @property {string} make - The manufacturer of the vehicle.
 * @property {string} model - The model of the vehicle.
 * @property {number} year - The manufacturing year.
 * @property {number} price - The price of the vehicle.
 * @property {number} [mileage] - The mileage of the vehicle.
 * @property {string} fuelType - The fuel type.
 * @property {string} transmission - The transmission type.
 * @property {string} vehicleType - The body style or category.
 * @property {number} [engineSize] - The engine displacement.
 * @property {number} [horsePower] - The engine horsepower.
 * @property {string} [country] - The country of origin or listing.
 * @property {string} description - A textual description of the vehicle.
 * @property {VehicleFeaturePayload[]} features - A list of features for the vehicle.
 */
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

/**
 * @interface UpdateVehiclePayload
 * @summary Defines the structure for the payload when updating an existing vehicle.
 *
 * @property {string} make - The manufacturer of the vehicle.
 * @property {string} model - The model of the vehicle.
 * @property {number} year - The manufacturing year.
 * @property {number} price - The price of the vehicle.
 * @property {number} [mileage] - The mileage of the vehicle.
 * @property {string} fuelType - The fuel type.
 * @property {string} transmission - The transmission type.
 * @property {string} vehicleType - The body style or category.
 * @property {number} [engineSize] - The engine displacement.
 * @property {number} [horsePower] - The engine horsepower.
 * @property {string} [country] - The country of origin or listing.
 * @property {string} description - A textual description of the vehicle.
 * @property {VehicleFeaturePayload[]} features - A list of features for the vehicle.
 */
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

/**
 * @typedef VehicleParams
 * @summary Represents a record of parameters for filtering vehicles.
 * @description Keys are parameter names (e.g., 'make', 'minPrice'), and values are their corresponding filter values.
 */
type VehicleParams = Record<
  string,
  string | number | boolean | undefined | string[]
>;

/**
 * @constant API_URL
 * @summary The base URL for the backend API.
 * @description It attempts to read from the `VITE_API_URL` environment variable, defaulting to '/api' if not set.
 */
const API_URL = import.meta.env.VITE_API_URL || '/api';

/**
 * @constant api
 * @summary Axios instance configured for API communication.
 * @description This instance has a `baseURL` set to `API_URL` and default headers for JSON content.
 * It also includes a request interceptor to attach JWT tokens.
 */
const api = axios.create({
  baseURL: API_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

/**
 * @remarks
 * Axios request interceptor.
 * This interceptor retrieves the JWT token from `localStorage` and, if found,
 * adds it to the `Authorization` header of outgoing requests using the Bearer scheme.
 */
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

/**
 * @summary Service object for authentication-related API calls.
 */
export const authService = {
  /**
   * @summary Logs in a user with the provided email and password.
   * @param {string} email - The user's email address.
   * @param {string} password - The user's password.
   * @returns {Promise<any>} A promise that resolves with the login response data (e.g., token, user info).
   * @throws {import('axios').AxiosError} If the API request fails.
   * @example
   * authService.login('user@example.com', 'password123')
   *   .then(data => console.log('Login successful:', data))
   *   .catch(error => console.error('Login failed:', error));
   */
  login: async (email: string, password: string) => {
    const response = await api.post('/api/auth/login', { email, password });
    return response.data;
  },
  /**
   * @summary Registers a new user with the provided user data.
   * @param {object} userData - The user registration data.
   * @param {string} userData.username - The desired username.
   * @param {string} userData.email - The user's email address.
   * @param {string} userData.password - The user's chosen password.
   * @param {string} [userData.firstName] - The user's first name (optional).
   * @param {string} [userData.lastName] - The user's last name (optional).
   * @param {string} [userData.phoneNumber] - The user's phone number (optional).
   * @returns {Promise<any>} A promise that resolves with the registration response data.
   * @throws {import('axios').AxiosError} If the API request fails.
   * @example
   * authService.register({ username: 'newuser', email: 'new@example.com', password: 'securePassword' })
   *   .then(data => console.log('Registration successful:', data))
   *   .catch(error => console.error('Registration failed:', error));
   */
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

/**
 * @summary Service object for vehicle-related API calls.
 */
export const vehicleService = {
  /**
   * @summary Fetches a list of vehicles, optionally filtered by parameters.
   * @param {VehicleParams} [params] - Optional parameters for filtering and pagination.
   * @returns {Promise<Vehicle[]>} A promise that resolves with an array of vehicles.
   * @remarks Handles potential ASP.NET Core `$values` wrapper in the response. Returns an empty array on error or if the API response is not an array.
   * @throws Logs an error to the console if fetching fails but does not re-throw.
   * @example
   * vehicleService.getVehicles({ make: 'Toyota', maxPrice: 20000 })
   *   .then(vehicles => console.log('Found vehicles:', vehicles));
   */
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

  /**
   * @summary Fetches a single vehicle by its ID.
   * @param {number} id - The ID of the vehicle to retrieve.
   * @returns {Promise<Vehicle>} A promise that resolves with the vehicle data.
   * @throws {import('axios').AxiosError} If the API request fails (e.g., vehicle not found).
   * @example
   * vehicleService.getVehicle(123)
   *   .then(vehicle => console.log('Vehicle details:', vehicle))
   *   .catch(error => console.error('Failed to get vehicle:', error));
   */
  getVehicle: async (id: number): Promise<Vehicle> => {
    const response = await api.get<Vehicle>(`/api/vehicles/${id}`);
    return response.data;
  },

  /**
   * @summary Creates a new vehicle.
   * @param {VehicleCreatePayload} vehicleData - The data for the new vehicle.
   * @returns {Promise<Vehicle>} A promise that resolves with the created vehicle data.
   * @throws {import('axios').AxiosError} If the API request fails.
   */
  createVehicle: async (
    vehicleData: VehicleCreatePayload
  ): Promise<Vehicle> => {
    const response = await api.post<Vehicle>('/api/vehicles', vehicleData);
    return response.data;
  },

  /**
   * @summary Updates an existing vehicle by its ID.
   * @param {number} id - The ID of the vehicle to update.
   * @param {UpdateVehiclePayload} vehicleData - The updated vehicle data.
   * @returns {Promise<void>} A promise that resolves when the update is successful.
   * @remarks The backend might return the updated vehicle; this function currently expects no content or ignores it.
   * @throws {import('axios').AxiosError} If the API request fails.
   */
  updateVehicle: async (
    id: number,
    vehicleData: UpdateVehiclePayload
  ): Promise<void> => {
    await api.put(`/api/vehicles/${id}`, vehicleData);
  },

  /**
   * @summary Uploads an image for a specific vehicle.
   * @param {number} vehicleId - The ID of the vehicle to associate the image with.
   * @param {File} imageFile - The image file to upload.
   * @returns {Promise<{ id: number; imageUrl: string; isPrimary: boolean }>} A promise that resolves with the details of the uploaded image.
   * @throws {import('axios').AxiosError} If the API request fails.
   */
  uploadVehicleImage: async (
    vehicleId: number,
    imageFile: File
  ): Promise<{ id: number; imageUrl: string; isPrimary: boolean }> => {
    const formData = new FormData();
    formData.append('imageFile', imageFile);
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

  /**
   * @summary Deletes an image associated with a vehicle.
   * @param {number} vehicleId - The ID of the vehicle.
   * @param {number} imageId - The ID of the image to delete.
   * @returns {Promise<void>} A promise that resolves when the image is successfully deleted.
   * @throws {import('axios').AxiosError} If the API request fails.
   */
  deleteVehicleImage: async (
    vehicleId: number,
    imageId: number
  ): Promise<void> => {
    await api.delete(`/api/vehicles/${vehicleId}/images/${imageId}`);
  },

  /**
   * @summary Sets a specific image as the primary image for a vehicle.
   * @param {number} vehicleId - The ID of the vehicle.
   * @param {number} imageId - The ID of the image to set as primary.
   * @returns {Promise<void>} A promise that resolves when the primary image is successfully set.
   * @throws {import('axios').AxiosError} If the API request fails.
   */
  setPrimaryVehicleImage: async (
    vehicleId: number,
    imageId: number
  ): Promise<void> => {
    await api.put(`/api/vehicles/${vehicleId}/images/${imageId}/primary`);
  },

  /**
   * @summary Fetches a list of available vehicle makes.
   * @returns {Promise<string[]>} A promise that resolves with an array of make names.
   * @throws Logs an error to the console and returns an empty array if fetching fails.
   */
  getAvailableMakes: async (): Promise<string[]> => {
    try {
      const response = await api.get('/api/vehicles/available-makes');
      return response.data;
    } catch (error) {
      console.error('Error fetching makes:', error);
      return [];
    }
  },

  /**
   * @summary Fetches a list of available vehicle models for a given make.
   * @param {string} make - The make of the vehicle to filter models by.
   * @returns {Promise<string[]>} A promise that resolves with an array of model names.
   * @throws Logs an error to the console and returns an empty array if fetching fails.
   */
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

  /**
   * @summary Fetches the minimum and maximum manufacturing year available for vehicles.
   * @returns {Promise<{ min: number; max: number }>} A promise that resolves with an object containing min and max years.
   * @throws Logs an error to the console and returns a default range if fetching fails.
   */
  getYearRange: async (): Promise<{ min: number; max: number }> => {
    try {
      const response = await api.get('/api/vehicles/year-range');
      return response.data;
    } catch (error) {
      console.error('Error fetching year range:', error);
      return { min: 1990, max: new Date().getFullYear() };
    }
  },

  /**
   * @summary Fetches the minimum and maximum engine size available for vehicles.
   * @returns {Promise<{ min: number; max: number }>} A promise that resolves with an object containing min and max engine sizes.
   * @throws Logs an error to the console and returns a default range if fetching fails.
   */
  getEngineSizeRange: async (): Promise<{ min: number; max: number }> => {
    try {
      const response = await api.get('/api/vehicles/engine-size-range');
      return response.data;
    } catch (error) {
      console.error('Error fetching engine size range:', error);
      return { min: 0, max: 8 };
    }
  },

  /**
   * @summary Fetches the minimum and maximum horsepower available for vehicles.
   * @returns {Promise<{ min: number; max: number }>} A promise that resolves with an object containing min and max horsepower values.
   * @throws Logs an error to the console and returns a default range if fetching fails.
   */
  getHorsepowerRange: async (): Promise<{ min: number; max: number }> => {
    try {
      const response = await api.get('/api/vehicles/horsepower-range');
      return response.data;
    } catch (error) {
      console.error('Error fetching horsepower range:', error);
      return { min: 0, max: 800 };
    }
  },
};

/**
 * @summary Service object for managing user's favorite vehicles.
 */
export const favoriteService = {
  /**
   * @summary Fetches the list of vehicles marked as favorite by the current user.
   * @returns {Promise<Vehicle[]>} A promise that resolves with an array of favorite vehicles.
   * @throws {import('axios').AxiosError} If the API request fails.
   */
  getFavorites: async () => {
    const response = await api.get<Vehicle[]>('/api/favorites');
    return response.data;
  },
  /**
   * @summary Adds a vehicle to the current user's favorites.
   * @param {number} vehicleId - The ID of the vehicle to add to favorites.
   * @returns {Promise<any>} A promise that resolves with the response data from the API.
   * @throws {import('axios').AxiosError} If the API request fails.
   */
  addFavorite: async (vehicleId: number) => {
    const response = await api.post(`/api/favorites/${vehicleId}`);
    return response.data;
  },
  /**
   * @summary Removes a vehicle from the current user's favorites.
   * @param {number} vehicleId - The ID of the vehicle to remove from favorites.
   * @returns {Promise<any>} A promise that resolves with the response data from the API.
   * @throws {import('axios').AxiosError} If the API request fails.
   */
  removeFavorite: async (vehicleId: number) => {
    const response = await api.delete(`/api/favorites/${vehicleId}`);
    return response.data;
  },
  /**
   * @summary Checks if a specific vehicle is in the current user's favorites.
   * @param {number} vehicleId - The ID of the vehicle to check.
   * @returns {Promise<boolean>} A promise that resolves with `true` if the vehicle is a favorite, `false` otherwise.
   * @throws {import('axios').AxiosError} If the API request fails.
   */
  checkFavorite: async (vehicleId: number) => {
    const response = await api.get<boolean>(
      `/api/favorites/check/${vehicleId}`
    );
    return response.data;
  },
  /**
   * @summary Gets the count of vehicles in the current user's favorites.
   * @returns {Promise<number>} A promise that resolves with the number of favorite vehicles.
   * @throws {import('axios').AxiosError} If the API request fails.
   */
  getFavoritesCount: async () => {
    const response = await api.get<number>('/api/favorites/count');
    return response.data;
  },
};

/**
 * @interface InquiryReplyData
 * @summary Defines the structure for the payload when replying to an inquiry.
 * @property {string} response - The text of the reply.
 */
interface InquiryReplyData {
  response: string;
}

/**
 * @summary Service object for managing inquiries.
 */
export const inquiryService = {
  /**
   * @summary Fetches inquiries for the current user.
   * @returns {Promise<any>} A promise that resolves with the list of inquiries (expected to be an array of Inquiry-like objects).
   * @throws {import('axios').AxiosError} If the API request fails.
   */
  getInquiries: async () => {
    const response = await api.get('/api/inquiries');
    return response.data;
  },
  /**
   * @summary Fetches a single inquiry by its ID.
   * @param {number} id - The ID of the inquiry to retrieve.
   * @returns {Promise<any>} A promise that resolves with the inquiry data.
   * @throws {import('axios').AxiosError} If the API request fails.
   */
  getInquiry: async (id: number) => {
    const response = await api.get(`/api/inquiries/${id}`);
    return response.data;
  },
  /**
   * @summary Creates a new inquiry.
   * @param {object} inquiryData - The data for the new inquiry.
   * @param {number} inquiryData.vehicleId - The ID of the vehicle the inquiry is about.
   * @param {string} inquiryData.subject - The subject of the inquiry.
   * @param {string} inquiryData.message - The message content of the inquiry.
   * @returns {Promise<any>} A promise that resolves with the created inquiry data.
   * @throws {import('axios').AxiosError} If the API request fails.
   */
  createInquiry: async (inquiryData: {
    vehicleId: number;
    subject: string;
    message: string;
  }) => {
    const response = await api.post('/api/inquiries', inquiryData);
    return response.data;
  },
  /**
   * @summary Closes an inquiry by its ID.
   * @param {number} id - The ID of the inquiry to close.
   * @returns {Promise<void>} A promise that resolves when the inquiry is successfully closed.
   * @throws {import('axios').AxiosError} If the API request fails.
   */
  closeInquiry: async (id: number) => {
    await api.put(`/api/inquiries/${id}/close`);
  },
  /**
   * @summary Fetches all inquiries, typically for admin users, optionally filtered by status.
   * @param {string} [status=''] - Optional status to filter inquiries by (e.g., "New", "Read").
   * @returns {Promise<any>} A promise that resolves with the list of inquiries.
   * @throws {import('axios').AxiosError} If the API request fails.
   */
  getAllInquiries: async (status = '') => {
    const response = await api.get(
      `/api/inquiries/admin${status ? `?status=${status}` : ''}`
    );
    return response.data;
  },
  /**
   * @summary Marks an inquiry as read by its ID.
   * @param {number} id - The ID of the inquiry to mark as read.
   * @returns {Promise<void>} A promise that resolves when the inquiry is successfully marked as read.
   * @throws {import('axios').AxiosError} If the API request fails.
   */
  markInquiryAsRead: async (id: number) => {
    await api.put(`/api/inquiries/${id}/MarkAsRead`);
  },
  /**
   * @summary Submits a reply to an inquiry.
   * @param {number} id - The ID of the inquiry to reply to.
   * @param {InquiryReplyData} replyData - The reply content.
   * @returns {Promise<void>} A promise that resolves when the reply is successfully submitted.
   * @throws {import('axios').AxiosError} If the API request fails.
   */
  replyToInquiry: async (id: number, replyData: InquiryReplyData) => {
    await api.put(`/api/inquiries/${id}/Reply`, replyData);
  },
};

/**
 * @summary Default export of the configured Axios instance.
 * @description Can be used for making direct API calls if needed, though using the service objects is preferred.
 */
export default api;
