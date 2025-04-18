import { useState, useEffect, ReactNode } from 'react';
import { jwtDecode, JwtPayload } from 'jwt-decode';
import { authService } from '../services/api';
import { AuthContext, User, UserRegistration } from './AuthContext';

// Define a custom interface for the JWT payload that includes potential role claims
interface JwtUserPayload extends JwtPayload {
  // Standard claims
  nameid?: string;
  unique_name?: string;
  email?: string;
  // Role claims - could be string or array of strings
  role?: string | string[];
  // .NET standard claims format
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'?:
    | string
    | string[];
}

// Helper function to decode JWT token and extract user roles
const decodeToken = (
  token: string
): (JwtUserPayload & { roles: string[] }) | null => {
  try {
    const decoded: JwtUserPayload = jwtDecode<JwtUserPayload>(token);

    // Extract roles from token claims
    let roles: string[] = [];

    // Case 1: Standard ClaimTypes.Role format from .NET
    if (
      decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
    ) {
      const roleClaim =
        decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      roles = Array.isArray(roleClaim) ? roleClaim : [roleClaim];
    }
    // Case 2: Simple "role" claim
    else if (decoded.role) {
      roles = Array.isArray(decoded.role) ? decoded.role : [decoded.role];
    }

    return { ...decoded, roles };
  } catch (error) {
    console.error('Failed to decode token:', error);
    return null;
  }
};

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>(
    localStorage.getItem('token')
  );
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    // Check if token exists in localStorage and validate it
    const checkAuth = async () => {
      const storedToken = localStorage.getItem('token');
      const storedUser = localStorage.getItem('user');

      if (storedToken && storedUser) {
        // Decode token to get role information
        const decodedToken = decodeToken(storedToken);

        // Parse stored user
        const parsedUser = JSON.parse(storedUser) as User;

        // Update user with roles from token if available
        if (decodedToken?.roles) {
          parsedUser.roles = decodedToken.roles;

          // Also keep singular role property for backward compatibility
          if (decodedToken.roles.length > 0) {
            parsedUser.role = decodedToken.roles[0];
          }

          // Update stored user with roles
          localStorage.setItem('user', JSON.stringify(parsedUser));
        }

        setToken(storedToken);
        setUser(parsedUser);
      }

      setLoading(false);
    };

    checkAuth();
  }, []);

  const login = async (email: string, password: string) => {
    try {
      const response = await authService.login(email, password);

      // Extract roles from JWT token
      const decodedToken = decodeToken(response.token);

      // Enhance user object with roles from token
      const enhancedUser: User = {
        ...response.user,
        roles: decodedToken?.roles || [],
        // Keep singular role for backward compatibility
        role: decodedToken?.roles?.length ? decodedToken.roles[0] : undefined,
      };

      setToken(response.token);
      setUser(enhancedUser);

      localStorage.setItem('token', response.token);
      localStorage.setItem('user', JSON.stringify(enhancedUser));
    } catch (error) {
      console.error('Login error:', error);
      throw error;
    }
  };

  const register = async (userData: UserRegistration) => {
    try {
      await authService.register(userData);
      // Automatically login after registration
      await login(userData.email, userData.password);
    } catch (error) {
      console.error('Registration error:', error);
      throw error;
    }
  };

  const logout = () => {
    setToken(null);
    setUser(null);
    localStorage.removeItem('token');
    localStorage.removeItem('user');
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
