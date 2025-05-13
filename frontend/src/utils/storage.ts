import { User } from '../contexts/AuthContext';

const TOKEN_KEY = 'token';
const USER_KEY = 'user';

// Basic error handling for JSON parsing
const safeJsonParse = <T>(jsonString: string | null): T | null => {
  if (!jsonString) return null;
  try {
    return JSON.parse(jsonString) as T;
  } catch (e) {
    console.error('Failed to parse JSON from storage', e);
    return null;
  }
};

export const storage = {
  getToken: (): string | null => {
    try {
      return localStorage.getItem(TOKEN_KEY);
    } catch (e) {
      // Handle potential SecurityError in restricted environments
      console.error('Failed to access localStorage', e);
      return null;
    }
  },
  saveToken: (token: string): void => {
    try {
      localStorage.setItem(TOKEN_KEY, token);
    } catch (e) {
      console.error('Failed to access localStorage', e);
    }
  },
  removeToken: (): void => {
    try {
      localStorage.removeItem(TOKEN_KEY);
    } catch (e) {
      console.error('Failed to access localStorage', e);
    }
  },

  getUser: (): User | null => {
    try {
      const userStr = localStorage.getItem(USER_KEY);
      const user = safeJsonParse<User>(userStr);
      if (!user && userStr) {
        // If parsing failed for existing data
        localStorage.removeItem(USER_KEY); // Clean up invalid data
      }
      return user;
    } catch (e) {
      console.error('Failed to access localStorage', e);
      return null;
    }
  },
  saveUser: (user: User): void => {
    try {
      localStorage.setItem(USER_KEY, JSON.stringify(user));
    } catch (e) {
      console.error('Failed to access localStorage', e);
    }
  },
  removeUser: (): void => {
    try {
      localStorage.removeItem(USER_KEY);
    } catch (e) {
      console.error('Failed to access localStorage', e);
    }
  },

  // Convenience function to clear both
  clearAuthData: (): void => {
    storage.removeToken();
    storage.removeUser();
  },
};
