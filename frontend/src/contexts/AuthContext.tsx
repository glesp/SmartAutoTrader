import { createContext } from 'react'

// Define the User interface
export interface User {
  id: number
  username: string
  email: string
  firstName?: string
  lastName?: string
}

// Define a UserRegistration interface for the register function
export interface UserRegistration {
  username: string
  email: string
  password: string
  firstName?: string
  lastName?: string
  phoneNumber?: string
}

// Define the AuthContextType interface
export interface AuthContextType {
  user: User | null
  token: string | null
  isAuthenticated: boolean
  loading: boolean
  login: (email: string, password: string) => Promise<void>
  register: (userData: UserRegistration) => Promise<void>
  logout: () => void
}

// Create the context with default values
export const AuthContext = createContext<AuthContextType>({
  user: null,
  token: null,
  isAuthenticated: false,
  loading: true,
  login: async () => {},
  register: async () => {},
  logout: () => {},
})
