/**
 * @file AuthProvider.tsx
 * @summary Provides the `AuthProvider` component, which manages user authentication and authorization state for the Smart Auto Trader application.
 *
 * @description The `AuthProvider` component wraps the application and provides authentication-related state and actions via the `AuthContext`.
 * It handles login, logout, registration, and token management, ensuring that user roles and authentication status are properly maintained.
 * The component also decodes JWT tokens to extract user roles and synchronizes authentication data with local storage.
 *
 * @remarks
 * - The `AuthProvider` uses React's `useState` and `useEffect` hooks to manage authentication state and side effects.
 * - JWT tokens are decoded to extract user roles, which are stored in both single-role (`role`) and multi-role (`roles`) formats for backward compatibility.
 * - The `AuthProvider` ensures that authentication data is persisted in local storage and reloaded on application startup.
 * - The `AuthContext` is used to provide authentication state and actions to child components.
 *
 * @dependencies
 * - React: `useState`, `useEffect`, `ReactNode` for managing state and rendering children.
 * - `jwt-decode`: For decoding JWT tokens to extract user roles and claims.
 * - `authService`: For making API calls related to authentication (e.g., login, registration).
 * - `storage`: For managing authentication data in local storage.
 * - `AuthContext`: For providing authentication state and actions to child components.
 */

import { useState, useEffect, ReactNode, JSX } from 'react';
import { jwtDecode, JwtPayload } from 'jwt-decode';
import { authService } from '../services/api';
import { AuthContext, User, UserRegistration } from './AuthContext';
import { storage } from '../utils/storage';

/**
 * @interface JwtUserPayload
 * @summary Represents the structure of a decoded JWT payload, including user roles and claims.
 *
 * @property {string} [nameid] - The unique identifier for the user.
 * @property {string} [unique_name] - The username of the user.
 * @property {string} [email] - The email address of the user.
 * @property {string | string[]} [role] - The user's role(s), which can be a string or an array of strings.
 * @property {string | string[]} ['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] - The .NET standard format for user roles.
 *
 * @remarks
 * - This interface extends the `JwtPayload` interface from `jwt-decode` to include custom claims used in the application.
 */
interface JwtUserPayload extends JwtPayload {
  nameid?: string;
  unique_name?: string;
  email?: string;
  role?: string | string[];
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'?:
    | string
    | string[];
}

/**
 * @function decodeToken
 * @summary Decodes a JWT token and extracts user roles and claims.
 *
 * @param {string} token - The JWT token to decode.
 * @returns {(JwtUserPayload & { roles: string[] }) | null} The decoded token with extracted roles, or `null` if decoding fails.
 *
 * @throws Will log an error to the console if the token cannot be decoded.
 *
 * @remarks
 * - The function supports both standard and .NET-specific formats for user roles.
 * - If the token is invalid or cannot be decoded, the function returns `null`.
 *
 * @example
 * const decoded = decodeToken(token);
 * console.log(decoded?.roles); // ['Admin', 'User']
 */
const decodeToken = (
  token: string
): (JwtUserPayload & { roles: string[] }) | null => {
  try {
    const decoded: JwtUserPayload = jwtDecode<JwtUserPayload>(token);

    let roles: string[] = [];

    if (
      decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
    ) {
      const roleClaim =
        decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      roles = Array.isArray(roleClaim) ? roleClaim : [roleClaim];
    } else if (decoded.role) {
      roles = Array.isArray(decoded.role) ? decoded.role : [decoded.role];
    }

    return { ...decoded, roles };
  } catch (error) {
    console.error('Failed to decode token:', error);
    return null;
  }
};

/**
 * @function AuthProvider
 * @summary Provides authentication state and actions to child components via the `AuthContext`.
 *
 * @param {{ children: ReactNode }} props - The props for the component.
 * @param {ReactNode} props.children - The child components to render within the provider.
 * @returns {JSX.Element} The rendered `AuthContext.Provider` component.
 *
 * @remarks
 * - The `AuthProvider` initializes authentication state from local storage and synchronizes it with the `AuthContext`.
 * - It provides methods for login, logout, and registration, as well as loading and authentication status.
 * - JWT tokens are decoded to extract user roles, which are stored in both single-role and multi-role formats for compatibility.
 *
 * @example
 * <AuthProvider>
 *   <App />
 * </AuthProvider>
 */
export const AuthProvider = ({
  children,
}: {
  children: ReactNode;
}): JSX.Element => {
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>(storage.getToken() || null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const checkAuth = async () => {
      const storedToken = storage.getToken();
      const storedUser = storage.getUser();

      if (storedToken && storedUser) {
        const decodedToken = decodeToken(storedToken);

        if (decodedToken?.roles) {
          storedUser.roles = decodedToken.roles;
          if (decodedToken.roles.length > 0) {
            storedUser.role = decodedToken.roles[0];
          }
          storage.saveUser(storedUser);
        }

        setToken(storedToken);
        setUser(storedUser);
      }

      setLoading(false);
    };

    checkAuth();
  }, []);

  /**
   * @function login
   * @summary Logs in a user with their email and password.
   *
   * @param {string} email - The user's email address.
   * @param {string} password - The user's password.
   * @returns {Promise<void>} A promise that resolves when the login is successful.
   *
   * @throws Will throw an error if the login request fails.
   *
   * @remarks
   * - The function updates the authentication state and stores the token and user data in local storage.
   * - User roles are extracted from the JWT token and added to the user object.
   */
  const login = async (email: string, password: string): Promise<void> => {
    try {
      const response = await authService.login(email, password);

      const decodedToken = decodeToken(response.token);

      const enhancedUser: User = {
        ...response.user,
        roles: decodedToken?.roles || [],
        role: decodedToken?.roles?.length ? decodedToken.roles[0] : undefined,
      };

      setToken(response.token);
      setUser(enhancedUser);

      storage.saveToken(response.token);
      storage.saveUser(enhancedUser);
    } catch (error) {
      console.error('Login error:', error);
      setUser(null);
      setToken(null);
      throw error;
    }
  };

  /**
   * @function register
   * @summary Registers a new user and logs them in.
   *
   * @param {UserRegistration} userData - The registration data for the new user.
   * @returns {Promise<void>} A promise that resolves when the registration is successful.
   *
   * @throws Will throw an error if the registration request fails.
   *
   * @remarks
   * - The function automatically logs in the user after successful registration.
   */
  const register = async (userData: UserRegistration): Promise<void> => {
    try {
      await authService.register(userData);
      await login(userData.email, userData.password);
    } catch (error) {
      console.error('Registration error:', error);
      setUser(null);
      setToken(null);
      throw error;
    }
  };

  /**
   * @function logout
   * @summary Logs out the currently authenticated user.
   *
   * @returns {void}
   *
   * @remarks
   * - The function clears the authentication state and removes authentication data from local storage.
   */
  const logout = (): void => {
    setToken(null);
    setUser(null);
    storage.clearAuthData();
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        token,
        isAuthenticated: !!token,
        loading,
        login,
        register,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};
