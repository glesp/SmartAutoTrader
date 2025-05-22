/**
 * @file storage.ts
 * @summary Provides utility functions for interacting with `localStorage` to manage authentication tokens and user data.
 *
 * @description This module offers a simple and safe interface for storing, retrieving, and removing
 * authentication-related data (JWT tokens and user objects) from the browser's `localStorage`.
 * It includes error handling for `localStorage` access and JSON parsing.
 *
 * @remarks
 * - All `localStorage` operations are wrapped in try-catch blocks to handle potential errors,
 *   such as `SecurityError` in restricted environments or `QuotaExceededError`.
 * - Uses a `safeJsonParse` utility to prevent application crashes from malformed JSON.
 * - Defines constants for `localStorage` keys to avoid magic strings.
 *
 * @dependencies
 * - User (../contexts/AuthContext): Type definition for the user object.
 */
import { User } from '../contexts/AuthContext';

/**
 * @constant TOKEN_KEY
 * @summary The key used to store the authentication token in `localStorage`.
 * @type {string}
 */
const TOKEN_KEY = 'token';

/**
 * @constant USER_KEY
 * @summary The key used to store the user object in `localStorage`.
 * @type {string}
 */
const USER_KEY = 'user';

/**
 * @function safeJsonParse
 * @summary Safely parses a JSON string, returning null if parsing fails or the string is null.
 * @template T - The expected type of the parsed object.
 * @param {string | null} jsonString - The JSON string to parse.
 * @returns {T | null} The parsed object of type T, or null if parsing fails or input is null.
 * @remarks Logs an error to the console if JSON parsing fails.
 * @example
 * const myData = safeJsonParse<MyType>(localStorage.getItem('myDataKey'));
 * if (myData) {
 *   // Use myData
 * }
 */
const safeJsonParse = <T>(jsonString: string | null): T | null => {
  if (!jsonString) return null;
  try {
    return JSON.parse(jsonString) as T;
  } catch (e) {
    console.error('Failed to parse JSON from storage', e);
    return null;
  }
};

/**
 * @summary An object encapsulating methods for interacting with `localStorage` for authentication data.
 * @description Provides methods to get, save, and remove tokens and user objects,
 * as well as a utility to clear all authentication data.
 */
export const storage = {
  /**
   * @summary Retrieves the authentication token from `localStorage`.
   * @returns {string | null} The token string if found and accessible, otherwise null.
   * @remarks Handles potential `localStorage` access errors.
   * @example
   * const token = storage.getToken();
   * if (token) {
   *   // Use the token
   * }
   */
  getToken: (): string | null => {
    try {
      return localStorage.getItem(TOKEN_KEY);
    } catch (e) {
      // Handle potential SecurityError in restricted environments
      console.error('Failed to access localStorage', e);
      return null;
    }
  },

  /**
   * @summary Saves the authentication token to `localStorage`.
   * @param {string} token - The token string to save.
   * @returns {void}
   * @remarks Handles potential `localStorage` access errors.
   * @example
   * storage.saveToken("your_jwt_token_here");
   */
  saveToken: (token: string): void => {
    try {
      localStorage.setItem(TOKEN_KEY, token);
    } catch (e) {
      console.error('Failed to access localStorage', e);
    }
  },

  /**
   * @summary Removes the authentication token from `localStorage`.
   * @returns {void}
   * @remarks Handles potential `localStorage` access errors.
   * @example
   * storage.removeToken();
   */
  removeToken: (): void => {
    try {
      localStorage.removeItem(TOKEN_KEY);
    } catch (e) {
      console.error('Failed to access localStorage', e);
    }
  },

  /**
   * @summary Retrieves the user object from `localStorage`.
   * @returns {User | null} The user object if found, parsed correctly, and accessible, otherwise null.
   * @remarks
   * - Uses `safeJsonParse` for parsing.
   * - If parsing fails for existing data, it attempts to clean up the invalid entry from `localStorage`.
   * - Handles potential `localStorage` access errors.
   * @example
   * const currentUser = storage.getUser();
   * if (currentUser) {
   *   console.log(currentUser.username);
   * }
   */
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

  /**
   * @summary Saves the user object to `localStorage`.
   * @param {User} user - The user object to save.
   * @returns {void}
   * @remarks
   * - The user object is stringified before saving.
   * - Handles potential `localStorage` access errors.
   * @example
   * const newUser = { id: 1, username: 'testuser', email: 'test@example.com', role: 'User' };
   * storage.saveUser(newUser);
   */
  saveUser: (user: User): void => {
    try {
      localStorage.setItem(USER_KEY, JSON.stringify(user));
    } catch (e) {
      console.error('Failed to access localStorage', e);
    }
  },

  /**
   * @summary Removes the user object from `localStorage`.
   * @returns {void}
   * @remarks Handles potential `localStorage` access errors.
   * @example
   * storage.removeUser();
   */
  removeUser: (): void => {
    try {
      localStorage.removeItem(USER_KEY);
    } catch (e) {
      console.error('Failed to access localStorage', e);
    }
  },

  /**
   * @summary Clears both the authentication token and user object from `localStorage`.
   * @returns {void}
   * @description A convenience function that calls `removeToken` and `removeUser`.
   * @example
   * storage.clearAuthData(); // Useful for logout procedures
   */
  clearAuthData: (): void => {
    storage.removeToken();
    storage.removeUser();
  },
};
