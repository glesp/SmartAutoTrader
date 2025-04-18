import { createContext } from 'react';

export interface User {
  id: number;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  role?: string;
  roles?: string[];
}

export interface UserRegistration {
  username: string;
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phoneNumber: string;
}

export interface AuthContextType {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  loading: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (userData: UserRegistration) => Promise<void>;
  logout: () => void;
}

export const AuthContext = createContext<AuthContextType>({
  user: null,
  token: null,
  isAuthenticated: false,
  loading: true,
  login: async () => {},
  register: async () => {},
  logout: () => {},
});
