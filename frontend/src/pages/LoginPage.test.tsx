// frontend/src/contexts/AuthProvider.test.tsx
import {
  render,
  screen,
  fireEvent,
  waitFor,
  act,
} from '@testing-library/react';
import { AuthProvider, AuthContext, User, UserRegistration } from '../contexts'; // Corrected path
import { authService } from '../services/api'; // This path seems correct
import { vi, type Mock } from 'vitest';
import '@testing-library/jest-dom';
import { useContext } from 'react';
import { storage } from '../utils/storage';
import * as tokenUtils from '../utils/tokenUtils';

// Mock the authService from services/api
vi.mock('../services/api', () => ({
  authService: {
    login: vi.fn(),
    register: vi.fn(),
  },
}));

// Mock the storage utility
vi.mock('../utils/storage', () => ({
  storage: {
    getToken: vi.fn(),
    getUser: vi.fn(),
    saveToken: vi.fn(),
    saveUser: vi.fn(),
    clearAuthData: vi.fn(),
  },
}));

// Mock tokenUtils
vi.mock('../utils/tokenUtils', () => ({
  decodeTokenAndExtractRoles: vi.fn(),
}));

// Helper component to consume and display context values
const TestConsumer = () => {
  const auth = useContext(AuthContext);
  if (!auth) {
    // Should not happen if AuthProvider is used correctly
    throw new Error('AuthContext was not provided');
  }

  return (
    <div>
      <div data-testid="isAuthenticated">{auth.isAuthenticated.toString()}</div>
      <div data-testid="isLoading">{auth.loading.toString()}</div>
      <div data-testid="user">{auth.user ? auth.user.username : 'null'}</div>
      <div data-testid="token">{auth.token || 'null'}</div>
      <button onClick={() => auth.login('test@example.com', 'password')}>
        Attempt Login
      </button>
      <button
        onClick={() =>
          auth.register({
            username: 'newbie',
            email: 'newbie@example.com',
            password: 'password123',
            firstName: 'New',
            lastName: 'User',
            phoneNumber: '12345',
          })
        }
      >
        Attempt Register
      </button>
      <button onClick={auth.logout}>Attempt Logout</button>
    </div>
  );
};

const renderAuthProviderWithConsumer = () => {
  return render(
    <AuthProvider>
      <TestConsumer />
    </AuthProvider>
  );
};

describe('AuthProvider', () => {
  // Define mock user and token at describe level scope
  const mockUser: User = {
    id: 1,
    username: 'testuser',
    email: 'test@example.com',
    firstName: 'Test',
    lastName: 'User',
    roles: ['User'],
  };

  // Use a structurally valid JWT string with three parts
  const mockToken =
    'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiIxIiwidW5pcXVlX25hbWUiOiJ0ZXN0dXNlciIsImVtYWlsIjoidGVzdEBleGFtcGxlLmNvbSIsInJvbGUiOlsiVXNlciJdLCJleHAiOjE3MTY0MzkwMjJ9.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c';

  const mockUserRegistration: UserRegistration = {
    username: 'newbie',
    email: 'newbie@example.com',
    password: 'password123',
    firstName: 'New',
    lastName: 'User',
    phoneNumber: '12345',
  };

  beforeEach(() => {
    // Reset all mocks
    vi.clearAllMocks();
    localStorage.clear();

    // Set default mock behavior
    (storage.getToken as Mock).mockReturnValue(null);
    (storage.getUser as Mock).mockReturnValue(null);

    // Create self-contained mock implementation that doesn't rely on external variables
    (tokenUtils.decodeTokenAndExtractRoles as Mock).mockImplementation(
      (token) => {
        if (!token) return null;

        // Default mock for any valid-looking token
        return {
          nameid: '1',
          unique_name: 'testuser',
          email: 'test@example.com',
          roles: ['User'],
          exp: Math.floor(Date.now() / 1000) + 3600, // Expires in 1 hour
        };
      }
    );
  });

  test('initial state: not authenticated, not loading (after useEffect finishes)', async () => {
    renderAuthProviderWithConsumer();

    // Wait for auth provider to initialize
    await waitFor(() => {
      expect(screen.getByTestId('isLoading')).toHaveTextContent('false');
    });

    expect(screen.getByTestId('isAuthenticated')).toHaveTextContent('false');
    expect(screen.getByTestId('user')).toHaveTextContent('null');
    expect(screen.getByTestId('token')).toHaveTextContent('null');
  });

  test('loads and validates token and user from localStorage on initial mount', async () => {
    // Mock storage to return token and user
    (storage.getToken as Mock).mockReturnValue(mockToken);
    (storage.getUser as Mock).mockReturnValue(mockUser);

    renderAuthProviderWithConsumer();

    // Wait for auth provider to initialize and process the token/user
    await waitFor(() => {
      expect(screen.getByTestId('isLoading')).toHaveTextContent('false');
    });

    // Now check authentication status
    await waitFor(() => {
      expect(screen.getByTestId('isAuthenticated')).toHaveTextContent('true');
      expect(screen.getByTestId('user')).toHaveTextContent(mockUser.username);
      expect(screen.getByTestId('token')).toHaveTextContent(mockToken);
    });
  });

  test('login: successful login updates context and localStorage', async () => {
    // Mock successful login response
    (authService.login as Mock).mockResolvedValueOnce({
      token: mockToken,
      user: mockUser,
    });

    renderAuthProviderWithConsumer();

    // Wait for initial loading to complete
    await waitFor(() => {
      expect(screen.getByTestId('isLoading')).toHaveTextContent('false');
    });

    // Attempt login
    const loginButton = screen.getByRole('button', { name: /Attempt Login/i });

    await act(async () => {
      fireEvent.click(loginButton);
    });

    // Verify login was called with correct parameters
    await waitFor(() => {
      expect(authService.login).toHaveBeenCalledWith(
        'test@example.com',
        'password'
      );
    });

    // Wait for authentication state to update
    await waitFor(() => {
      expect(screen.getByTestId('isAuthenticated')).toHaveTextContent('true');
      expect(screen.getByTestId('user')).toHaveTextContent(mockUser.username);
      expect(screen.getByTestId('token')).toHaveTextContent(mockToken);
    });

    // Verify storage was updated
    expect(storage.saveToken).toHaveBeenCalledWith(mockToken);
    expect(storage.saveUser).toHaveBeenCalledWith(
      expect.objectContaining({
        username: mockUser.username,
      })
    );
  });

  test('login: failed login does not update context and throws error', async () => {
    const loginError = new Error('Invalid credentials');
    (authService.login as Mock).mockRejectedValueOnce(loginError);

    renderAuthProviderWithConsumer();

    // Wait for initial loading to complete
    await waitFor(() => {
      expect(screen.getByTestId('isLoading')).toHaveTextContent('false');
    });

    const loginButton = screen.getByRole('button', { name: /Attempt Login/i });

    // Create a promise that will resolve when an error is logged to console
    const consoleErrorPromise = new Promise<void>((resolve) => {
      const originalConsoleError = console.error;
      console.error = (...args: any[]) => {
        console.error = originalConsoleError; // Restore original immediately
        originalConsoleError(...args);
        resolve();
      };
    });

    // Click the button to trigger the login action
    fireEvent.click(loginButton);

    // Wait for console.error to be called (when auth.login rejects)
    await consoleErrorPromise;

    // Wait for login to be called
    await waitFor(() => {
      expect(authService.login).toHaveBeenCalledWith(
        'test@example.com',
        'password'
      );
    });

    // Verify authentication state remains unchanged
    expect(screen.getByTestId('isAuthenticated')).toHaveTextContent('false');
    expect(storage.saveToken).not.toHaveBeenCalled();
    expect(storage.saveUser).not.toHaveBeenCalled();
  });

  test('logout: clears context and localStorage', async () => {
    // Set up initial logged-in state
    (storage.getToken as Mock).mockReturnValue(mockToken);
    (storage.getUser as Mock).mockReturnValue(mockUser);

    renderAuthProviderWithConsumer();

    // Wait for authentication to be set up correctly
    await waitFor(() => {
      expect(screen.getByTestId('isLoading')).toHaveTextContent('false');
      expect(screen.getByTestId('isAuthenticated')).toHaveTextContent('true');
    });

    // Perform logout
    const logoutButton = screen.getByRole('button', {
      name: /Attempt Logout/i,
    });

    await act(async () => {
      fireEvent.click(logoutButton);
    });

    // Verify authentication state is cleared
    expect(screen.getByTestId('isAuthenticated')).toHaveTextContent('false');
    expect(screen.getByTestId('user')).toHaveTextContent('null');
    expect(screen.getByTestId('token')).toHaveTextContent('null');
    expect(storage.clearAuthData).toHaveBeenCalled();
  });

  test('register: successful registration calls authService.register, then authService.login, and updates context', async () => {
    // Mock successful register and login
    (authService.register as Mock).mockResolvedValueOnce(undefined);
    (authService.login as Mock).mockResolvedValueOnce({
      token: mockToken,
      user: mockUser,
    });

    renderAuthProviderWithConsumer();

    // Wait for initial loading to complete
    await waitFor(() => {
      expect(screen.getByTestId('isLoading')).toHaveTextContent('false');
    });

    // Attempt registration
    const registerButton = screen.getByRole('button', {
      name: /Attempt Register/i,
    });

    await act(async () => {
      fireEvent.click(registerButton);
    });

    // Verify register was called with correct data
    await waitFor(() => {
      expect(authService.register).toHaveBeenCalledWith(mockUserRegistration);
    });

    // Verify login was called with correct credentials
    await waitFor(() => {
      expect(authService.login).toHaveBeenCalledWith(
        mockUserRegistration.email,
        mockUserRegistration.password
      );
    });

    // Verify authentication state updated
    await waitFor(() => {
      expect(screen.getByTestId('isAuthenticated')).toHaveTextContent('true');
      expect(screen.getByTestId('user')).toHaveTextContent(mockUser.username);
    });
  });

  test('register: failed registration does not call login or update context', async () => {
    const registerError = new Error('Email already exists');
    (authService.register as Mock).mockRejectedValueOnce(registerError);

    renderAuthProviderWithConsumer();

    // Wait for initial loading to complete
    await waitFor(() => {
      expect(screen.getByTestId('isLoading')).toHaveTextContent('false');
    });

    const registerButton = screen.getByRole('button', {
      name: /Attempt Register/i,
    });

    // Create a promise that will resolve when an error is logged to console
    const consoleErrorPromise = new Promise<void>((resolve) => {
      const originalConsoleError = console.error;
      console.error = (...args: any[]) => {
        console.error = originalConsoleError; // Restore original immediately
        originalConsoleError(...args);
        resolve();
      };
    });

    // Click the button to trigger the register action
    fireEvent.click(registerButton);

    // Wait for console.error to be called (when auth.register rejects)
    await consoleErrorPromise;

    // Wait for register to be called
    await waitFor(() => {
      expect(authService.register).toHaveBeenCalledWith(mockUserRegistration);
    });

    // Verify login was not called and auth state didn't change
    expect(authService.login).not.toHaveBeenCalled();
    expect(screen.getByTestId('isAuthenticated')).toHaveTextContent('false');
  });
});

// We need to create a simple LoginPage mock component for the tests
// This is a simplified version that will display the authentication state
import { useNavigate } from 'react-router-dom';

const LoginPage = () => {
  const navigate = useNavigate();
  const auth = useContext(AuthContext);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await auth.login('test@example.com', 'password123');
      navigate('/', { replace: true });
    } catch (error) {
      console.error('Login failed', error);
    }
  };

  return (
    <div>
      <div data-testid="isAuthenticated">{auth.isAuthenticated.toString()}</div>
      <div data-testid="userUsername">{auth.user?.username || 'none'}</div>
      <div data-testid="tokenValue">{auth.token || 'none'}</div>

      <form onSubmit={handleSubmit}>
        <label htmlFor="email">Email</label>
        <input id="email" type="email" />
        <label htmlFor="password">Password</label>
        <input id="password" type="password" />
        <button type="submit">Log In</button>
      </form>

      <button onClick={auth.logout}>Log Out</button>
    </div>
  );
};

// Mock the router
vi.mock('react-router-dom', () => ({
  useNavigate: vi.fn(() => vi.fn()),
  Link: ({ children }: { children: React.ReactNode }) => <a>{children}</a>,
}));

describe('LoginPage', () => {
  // Define mock user and token for LoginPage tests
  const mockUser: User = {
    id: 1,
    username: 'testuser',
    email: 'test@example.com',
    firstName: 'Test',
    lastName: 'User',
    roles: ['User'],
  };

  // Use a structurally valid JWT string with three parts
  const mockToken =
    'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiIxIiwidW5pcXVlX25hbWUiOiJ0ZXN0dXNlciIsImVtYWlsIjoidGVzdEBleGFtcGxlLmNvbSIsInJvbGUiOlsiVXNlciJdLCJleHAiOjE3MTY0MzkwMjJ9.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c';

  beforeEach(() => {
    vi.clearAllMocks();

    // Set default mocks for logged-out state
    (storage.getToken as Mock).mockReturnValue(null);
    (storage.getUser as Mock).mockReturnValue(null);

    // Reset mocks
    (authService.login as Mock).mockReset();
    (authService.register as Mock).mockReset();
    (storage.saveToken as Mock).mockReset();
    (storage.saveUser as Mock).mockReset();
    (storage.clearAuthData as Mock).mockReset();

    // Mock token decoding for LoginPage tests
    (tokenUtils.decodeTokenAndExtractRoles as Mock).mockImplementation(
      (token) => {
        if (!token) return null;

        return {
          nameid: '1',
          unique_name: 'testuser',
          email: 'test@example.com',
          roles: ['User'],
          exp: Math.floor(Date.now() / 1000) + 3600,
        };
      }
    );
  });

  // Helper function to set up logged-in state
  const setupLoggedInState = () => {
    (storage.getToken as Mock).mockReturnValue(mockToken);
    (storage.getUser as Mock).mockReturnValue(mockUser);
  };

  test('loads and validates token and user from localStorage on initial mount', async () => {
    setupLoggedInState();

    render(
      <AuthProvider>
        <LoginPage />
      </AuthProvider>
    );

    // Wait for the async operations in AuthProvider's useEffect to complete
    await waitFor(() => {
      expect(screen.getByTestId('isAuthenticated')).toHaveTextContent('true');
      expect(screen.getByTestId('userUsername')).toHaveTextContent(
        mockUser.username
      );
      expect(screen.getByTestId('tokenValue')).toHaveTextContent(mockToken);
    });
  });

  test('logout: clears context and localStorage', async () => {
    // Setup initial logged-in state
    setupLoggedInState();

    render(
      <AuthProvider>
        <LoginPage />
      </AuthProvider>
    );

    // First wait for logged-in state
    await waitFor(() => {
      expect(screen.getByTestId('isAuthenticated')).toHaveTextContent('true');
    });

    // Find and click logout button
    const logoutButton = screen.getByRole('button', { name: /Log Out/i });

    await act(async () => {
      fireEvent.click(logoutButton);
    });

    // Then verify logged-out state
    expect(screen.getByTestId('isAuthenticated')).toHaveTextContent('false');
    expect(storage.clearAuthData).toHaveBeenCalled();
  });

  test('login: successful login updates context and localStorage', async () => {
    // Mock the login API response
    (authService.login as Mock).mockResolvedValueOnce({
      token: mockToken,
      user: mockUser,
    });

    // Ensure the token decode mock returns the expected roles
    // This is important because AuthProvider's login function uses these roles
    (tokenUtils.decodeTokenAndExtractRoles as Mock).mockReturnValueOnce({
      nameid: '1',
      unique_name: 'testuser',
      email: 'test@example.com',
      roles: ['User'],
      exp: Math.floor(Date.now() / 1000) + 3600,
    });

    const navigate = vi.fn();
    (useNavigate as Mock).mockReturnValue(navigate);

    render(
      <AuthProvider>
        <LoginPage />
      </AuthProvider>
    );

    // Submit the login form
    const loginButton = screen.getByRole('button', { name: /Log In/i });

    await act(async () => {
      fireEvent.click(loginButton);
    });

    // Wait for login API call
    await waitFor(() => {
      expect(authService.login).toHaveBeenCalledWith(
        'test@example.com',
        'password123'
      );
    });

    // Wait for state updates
    await waitFor(() => {
      expect(screen.getByTestId('isAuthenticated')).toHaveTextContent('true');
    });

    // Verify token and user were saved with the enhanced user object
    expect(storage.saveToken).toHaveBeenCalledWith(mockToken);
    expect(storage.saveUser).toHaveBeenCalledWith(
      expect.objectContaining({
        username: mockUser.username,
        roles: ['User'], // Explicitly assert the roles array
      })
    );
    expect(navigate).toHaveBeenCalledWith('/', { replace: true });
  });
});
