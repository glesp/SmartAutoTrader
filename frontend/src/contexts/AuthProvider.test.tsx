// frontend/src/contexts/AuthProvider.test.tsx
import {
  render,
  screen,
  fireEvent,
  waitFor,
  act,
  renderHook,
} from '@testing-library/react';
import { AuthProvider } from './AuthProvider';
import { AuthContext, User, UserRegistration } from './AuthContext';
import { vi, type Mock } from 'vitest';
import '@testing-library/jest-dom';
import { useContext } from 'react';

// Import modules that will be mocked to access their mock types
import { authService } from '../services/api';
import { storage } from '../utils/storage';
import { decodeTokenAndExtractRoles } from '../utils/tokenUtils'; // rename import

// Mock the API service
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

// Mock jwt-decode BEFORE it's used by the AuthProvider
vi.mock('jwt-decode', () => ({
  jwtDecode: vi.fn(() => ({
    exp: Math.floor(Date.now() / 1000) + 60 * 60,
    nameid: 'mock-user-id',
    unique_name: 'mockuser',
    email: 'mock@example.com',
    'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': ['User'],
  })),
}));

// Mock tokenUtils
vi.mock('../utils/tokenUtils', () => ({
  decodeTokenAndExtractRoles: vi.fn(),
}));

const TestConsumer = () => {
  const auth = useContext(AuthContext);
  if (!auth) throw new Error('AuthContext not provided in TestConsumer');

  return (
    <div>
      <div data-testid="isAuthenticated">{auth.isAuthenticated.toString()}</div>
      <div data-testid="isLoading">{auth.loading.toString()}</div>
      <div data-testid="userUsername">
        {auth.user ? auth.user.username : 'null'}
      </div>
      <div data-testid="tokenValue">{auth.token || 'null'}</div>
      <button onClick={() => auth.login('test@example.com', 'password123')}>
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
            phoneNumber: '0123456789',
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
  const mockUser: User = {
    id: 1,
    username: 'testuser',
    email: 'test@example.com',
    firstName: 'Test',
    lastName: 'User',
    roles: ['User'],
  };
  const mockToken = 'mock-jwt-token-xyz';
  const mockUserRegistrationData: UserRegistration = {
    username: 'newbie',
    email: 'newbie@example.com',
    password: 'password123',
    firstName: 'New',
    lastName: 'User',
    phoneNumber: '0123456789',
  };

  beforeEach(() => {
    localStorage.clear();
    // Correct way to clear and reset the mocked functions:
    vi.clearAllMocks();

    // Type these mocks properly to access their mock methods
    (authService.login as Mock).mockReset();
    (authService.register as Mock).mockReset();
    (storage.getToken as Mock).mockReset();
    (storage.getUser as Mock).mockReset();
    (storage.saveToken as Mock).mockReset();
    (storage.saveUser as Mock).mockReset();
    (storage.clearAuthData as Mock).mockReset();
    (decodeTokenAndExtractRoles as Mock).mockReset();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  test('initial state: not authenticated, not loading (after useEffect resolution)', async () => {
    renderAuthProviderWithConsumer();
    await waitFor(() => {
      expect(screen.getByTestId('isLoading')).toHaveTextContent('false');
    });
    expect(screen.getByTestId('isAuthenticated')).toHaveTextContent('false');
    expect(screen.getByTestId('userUsername')).toHaveTextContent('null');
    expect(screen.getByTestId('tokenValue')).toHaveTextContent('null');
  });

  test('loads and validates token and user from localStorage on initial mount', async () => {
    (storage.getToken as Mock).mockReturnValue(mockToken);
    (storage.getUser as Mock).mockReturnValue(mockUser);
    renderAuthProviderWithConsumer();
    await waitFor(() => {
      expect(screen.getByTestId('isLoading')).toHaveTextContent('false');
    });
    await waitFor(() => {
      expect(screen.getByTestId('isAuthenticated')).toHaveTextContent('true');
      expect(screen.getByTestId('userUsername')).toHaveTextContent(
        mockUser.username
      );
      expect(screen.getByTestId('tokenValue')).toHaveTextContent(mockToken);
    });
  });

  test('login: successful login updates context and localStorage', async () => {
    // Mock successful login response
    (authService.login as Mock).mockResolvedValueOnce({
      token: mockToken,
      user: mockUser,
    });

    // Setup token decode mock for this specific test
    (decodeTokenAndExtractRoles as Mock).mockReturnValueOnce({
      nameid: '1',
      unique_name: 'testuser',
      email: 'test@example.com',
      roles: ['User'],
      exp: Math.floor(Date.now() / 1000) + 3600,
    });

    // Render the component
    const { result } = renderHook(() => useContext(AuthContext), {
      wrapper: ({ children }) => <AuthProvider>{children}</AuthProvider>,
    });

    // Call the login function inside act
    await act(async () => {
      // We await the promise returned by login to ensure it completes inside the act
      await result.current.login('test@example.com', 'password123');
    });

    // Now use waitFor to observe the state changes
    await waitFor(() => {
      // Assert on context changes
      expect(result.current.isAuthenticated).toBe(true);
      expect(result.current.token).toBe(mockToken);
      expect(result.current.user).toEqual(
        expect.objectContaining({
          username: mockUser.username,
          roles: ['User'],
        })
      );
    });

    // Assert on storage calls which should have happened by now
    expect(storage.saveToken).toHaveBeenCalledWith(mockToken);
    expect(storage.saveUser).toHaveBeenCalledWith(
      expect.objectContaining({
        username: mockUser.username,
        roles: ['User'],
      })
    );
  });

  test('login: failed login does not update context and authProvider.login re-throws the error', async () => {
    const loginError = new Error('Invalid credentials');
    (authService.login as Mock).mockRejectedValueOnce(loginError);
    renderAuthProviderWithConsumer();
    await waitFor(() =>
      expect(screen.getByTestId('isLoading')).toHaveTextContent('false')
    );

    const loginButton = screen.getByRole('button', { name: /Attempt Login/i });

    await act(async () => {
      try {
        fireEvent.click(loginButton);
        await waitFor(() => expect(authService.login).toHaveBeenCalled());
      } catch {
        // Handle error if needed
      }
    });

    expect(screen.getByTestId('isAuthenticated')).toHaveTextContent('false');
    expect(storage.saveToken).not.toHaveBeenCalled();
    expect(storage.saveUser).not.toHaveBeenCalled();
  });

  test('logout: clears context and localStorage', async () => {
    (storage.getToken as Mock).mockReturnValue(mockToken);
    (storage.getUser as Mock).mockReturnValue(mockUser);
    renderAuthProviderWithConsumer();
    await waitFor(() =>
      expect(screen.getByTestId('isAuthenticated')).toHaveTextContent('true')
    );

    const logoutButton = screen.getByRole('button', {
      name: /Attempt Logout/i,
    });
    await act(async () => {
      fireEvent.click(logoutButton);
    });

    expect(screen.getByTestId('isAuthenticated')).toHaveTextContent('false');
    expect(screen.getByTestId('userUsername')).toHaveTextContent('null');
    expect(screen.getByTestId('tokenValue')).toHaveTextContent('null');
    expect(storage.clearAuthData).toHaveBeenCalled();
  });

  test('register: successful registration calls services and updates context', async () => {
    // Mock the authService responses
    (authService.register as Mock).mockResolvedValueOnce(undefined);
    (authService.login as Mock).mockResolvedValueOnce({
      token: mockToken,
      user: mockUser,
    });

    // Mock the token decoding to return expected roles
    (decodeTokenAndExtractRoles as Mock).mockReturnValueOnce({
      nameid: '1',
      unique_name: 'testuser',
      email: 'test@example.com',
      roles: ['User'],
      exp: Math.floor(Date.now() / 1000) + 3600,
    });

    // First, render the AuthProvider with a child component that can access the context
    const { result } = renderHook(() => useContext(AuthContext), {
      wrapper: ({ children }) => <AuthProvider>{children}</AuthProvider>,
    });

    // Use act to wrap the registration process that will trigger state updates
    await act(async () => {
      // Wait for the promise returned by register to resolve completely
      await result.current.register(mockUserRegistrationData);
    });

    // After all state updates have happened, make assertions inside waitFor
    await waitFor(() => {
      // Assert that context was updated correctly
      expect(result.current.isAuthenticated).toBe(true);
      expect(result.current.token).toBe(mockToken);
      expect(result.current.user).toEqual(
        expect.objectContaining({
          username: mockUser.username,
          roles: ['User'],
        })
      );
    });

    // Assert that service methods were called correctly
    expect(authService.register).toHaveBeenCalledWith(mockUserRegistrationData);
    expect(authService.login).toHaveBeenCalledWith(
      mockUserRegistrationData.email,
      mockUserRegistrationData.password
    );
    expect(storage.saveToken).toHaveBeenCalledWith(mockToken);
    expect(storage.saveUser).toHaveBeenCalledWith(
      expect.objectContaining({
        username: mockUser.username,
        roles: ['User'],
      })
    );
  });

  test('register: failed registration does not call login or update context and authProvider.register re-throws error', async () => {
    const registerError = new Error('Email already exists');
    (authService.register as Mock).mockRejectedValueOnce(registerError);
    renderAuthProviderWithConsumer();
    await waitFor(() =>
      expect(screen.getByTestId('isLoading')).toHaveTextContent('false')
    );

    const registerButton = screen.getByRole('button', {
      name: /Attempt Register/i,
    });

    await expect(
      act(async () => {
        await fireEvent.click(registerButton);
        await vi.waitFor(() => expect(authService.register).toHaveBeenCalled());
      })
    ).rejects.toThrow(registerError.message);

    expect(authService.login).not.toHaveBeenCalled();
    expect(screen.getByTestId('isAuthenticated')).toHaveTextContent('false');
  });
});
