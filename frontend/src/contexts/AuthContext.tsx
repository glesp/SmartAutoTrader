/**
 * @file AuthContext.tsx
 * @summary Defines the `AuthContext` and related types for managing user authentication and authorization in the Smart Auto Trader application.
 *
 * @description This file provides the `AuthContext` for managing authentication state, user information, and authentication-related actions such as login, logout, and registration.
 * It includes TypeScript interfaces for defining the structure of user data, registration data, and the context itself. The `AuthContext` is used throughout the application to
 * determine the user's authentication status and roles.
 *
 * @remarks
 * - The `AuthContext` is initialized with default values to ensure type safety and prevent runtime errors.
 * - The `AuthContextType` interface defines the shape of the context, including methods for login, registration, and logout.
 * - The `User` interface supports both single-role (`role`) and multi-role (`roles`) formats for backward compatibility.
 * - This context is typically consumed using React's `useContext` hook in components that require authentication state.
 *
 * @dependencies
 * - React: `createContext` for creating the authentication context.
 */

import { createContext } from 'react';

/**
 * @interface User
 * @summary Represents a user in the authentication system.
 *
 * @property {number} id - The unique identifier for the user.
 * @property {string} username - The username of the user.
 * @property {string} email - The email address of the user.
 * @property {string} firstName - The first name of the user.
 * @property {string} lastName - The last name of the user.
 * @property {string} [role] - The user's role (e.g., "Admin", "User"). Optional for backward compatibility.
 * @property {string[]} [roles] - An array of roles assigned to the user. Optional for backward compatibility.
 *
 * @remarks
 * - The `role` property is a single-role format, while `roles` supports multiple roles.
 * - Both properties are optional to ensure compatibility with different backend implementations.
 */
export interface User {
  id: number;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  role?: string;
  roles?: string[];
}

/**
 * @interface UserRegistration
 * @summary Represents the data required for user registration.
 *
 * @property {string} username - The desired username for the new user.
 * @property {string} email - The email address of the new user.
 * @property {string} password - The password for the new user.
 * @property {string} firstName - The first name of the new user.
 * @property {string} lastName - The last name of the new user.
 * @property {string} phoneNumber - The phone number of the new user.
 *
 * @remarks
 * - This interface defines the structure of the data required to register a new user.
 * - All fields are required to ensure complete user information during registration.
 */
export interface UserRegistration {
  username: string;
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phoneNumber: string;
}

/**
 * @interface AuthContextType
 * @summary Defines the shape of the authentication context.
 *
 * @property {User | null} user - The currently authenticated user, or `null` if no user is authenticated.
 * @property {string | null} token - The authentication token for the current session, or `null` if not authenticated.
 * @property {boolean} isAuthenticated - Indicates whether the user is authenticated.
 * @property {boolean} loading - Indicates whether authentication data is being loaded.
 * @property {(email: string, password: string) => Promise<void>} login - A function to log in a user with their email and password.
 * @property {(userData: UserRegistration) => Promise<void>} register - A function to register a new user with the provided data.
 * @property {() => void} logout - A function to log out the currently authenticated user.
 *
 * @remarks
 * - The `login` and `register` methods return promises to handle asynchronous operations.
 * - The `loading` property is useful for displaying loading indicators while authentication data is being fetched.
 *
 * @example
 * const { user, login, logout } = useContext(AuthContext);
 * if (user) {
 *   console.log(`Logged in as ${user.username}`);
 * }
 */
export interface AuthContextType {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  loading: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (userData: UserRegistration) => Promise<void>;
  logout: () => void;
}

/**
 * @constant AuthContext
 * @summary The authentication context for managing user authentication state and actions.
 *
 * @type {React.Context<AuthContextType>}
 *
 * @remarks
 * - The context is initialized with default values to ensure type safety.
 * - This context should be provided at the top level of the application (e.g., in `App.tsx`) and consumed using `useContext`.
 *
 * @example
 * <AuthContext.Provider value={authContextValue}>
 *   <App />
 * </AuthContext.Provider>
 */
export const AuthContext = createContext<AuthContextType>({
  user: null,
  token: null,
  isAuthenticated: false,
  loading: true,
  login: async () => {},
  register: async () => {},
  logout: () => {},
});
