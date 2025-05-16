// frontend/src/pages/RegisterPage.test.tsx
import {
  render,
  screen,
  fireEvent,
  waitFor,
  act,
} from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import RegisterPage from './RegisterPage';
import {
  AuthContext,
  AuthContextType,
  UserRegistration,
} from '../contexts/AuthContext';
import { vi, type Mock } from 'vitest';
import '@testing-library/jest-dom';

vi.mock('react-router-dom', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router-dom')>();
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

vi.mock('../utils/storage', () => ({
  storage: {
    saveToken: vi.fn(),
    saveUser: vi.fn(),
    getToken: vi.fn(),
    getUser: vi.fn(),
    clearAuthData: vi.fn(),
  },
}));

const mockNavigate = vi.fn();

describe('RegisterPage Component', () => {
  let mockRegisterFn: Mock;

  const renderRegisterPageWithContext = (
    authContextOverride: Partial<AuthContextType> = {}
  ) => {
    const contextValue: AuthContextType = {
      isAuthenticated: false,
      user: null,
      token: null,
      loading: false,
      login: vi.fn(),
      register: mockRegisterFn,
      logout: vi.fn(),
      ...authContextOverride,
    };
    return render(
      <AuthContext.Provider value={contextValue}>
        <MemoryRouter initialEntries={['/register']}>
          <Routes>
            <Route path="/register" element={<RegisterPage />} />
            <Route path="/" element={<div>Mock Homepage</div>} />
          </Routes>
        </MemoryRouter>
      </AuthContext.Provider>
    );
  };

  beforeEach(() => {
    mockRegisterFn = vi.fn();
    mockNavigate.mockClear();
  });

  test('renders registration form with all fields and submit button', () => {
    renderRegisterPageWithContext({});
    expect(
      screen.getByRole('textbox', { name: /Username/i })
    ).toBeInTheDocument(); // NEW
    expect(
      screen.getByRole('textbox', { name: /Email Address/i })
    ).toBeInTheDocument(); // NEW
    expect(
      screen.getByRole('textbox', { name: /First Name/i })
    ).toBeInTheDocument(); // NEW
    expect(
      screen.getByRole('textbox', { name: /Last Name/i })
    ).toBeInTheDocument(); // NEW
    expect(screen.getByLabelText(/Phone Number/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/^Password \*/i)).toBeInTheDocument(); // More specific for "Password"
    expect(screen.getByLabelText(/Confirm Password \*/i)).toBeInTheDocument(); // Specific for "Confirm Password"
    expect(
      screen.getByRole('button', { name: /Register/i })
    ).toBeInTheDocument();
  });

  test('allows typing into form fields', () => {
    renderRegisterPageWithContext({});
    const usernameInput = screen.getByRole('textbox', {
      name: /Username/i,
    }) as HTMLInputElement; // NEW
    fireEvent.change(usernameInput, { target: { value: 'newuser123' } });
    expect(usernameInput.value).toBe('newuser123');
  });

  test('calls register from AuthContext with form data and navigates on successful submit', async () => {
    mockRegisterFn.mockResolvedValueOnce(undefined);
    renderRegisterPageWithContext({ register: mockRegisterFn });

    const userData: UserRegistration = {
      username: 'newuser',
      email: 'new@example.com',
      password: 'ValidPassword123!',
      firstName: 'New',
      lastName: 'User',
      phoneNumber: '1234567890',
    };

    fireEvent.change(screen.getByRole('textbox', { name: /Username/i }), {
      target: { value: userData.username },
    });
    fireEvent.change(screen.getByRole('textbox', { name: /Email Address/i }), {
      target: { value: userData.email },
    });
    fireEvent.change(screen.getByRole('textbox', { name: /First Name/i }), {
      target: { value: userData.firstName },
    });
    fireEvent.change(screen.getByRole('textbox', { name: /Last Name/i }), {
      target: { value: userData.lastName },
    });
    fireEvent.change(screen.getByLabelText(/Phone Number/i), {
      target: { value: userData.phoneNumber },
    });
    fireEvent.change(screen.getByLabelText(/^Password \*/i), {
      target: { value: userData.password },
    });
    fireEvent.change(screen.getByLabelText(/Confirm Password \*/i), {
      target: { value: userData.password },
    });

    await act(async () => {
      fireEvent.click(screen.getByRole('button', { name: /Register/i }));
    });

    await waitFor(() => {
      expect(mockRegisterFn).toHaveBeenCalledWith(userData);
    });

    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith('/', { replace: true });
    });
  });

  test('displays an error message on failed registration', async () => {
    mockRegisterFn.mockRejectedValueOnce(new Error('Registration has failed'));
    renderRegisterPageWithContext({ register: mockRegisterFn });

    fireEvent.change(screen.getByRole('textbox', { name: /Username/i }), {
      target: { value: 'failuser' },
    });
    fireEvent.change(screen.getByRole('textbox', { name: /Email Address/i }), {
      target: { value: 'fail@example.com' },
    });
    fireEvent.change(screen.getByRole('textbox', { name: /First Name/i }), {
      target: { value: 'Fail' },
    });
    fireEvent.change(screen.getByRole('textbox', { name: /Last Name/i }), {
      target: { value: 'User' },
    });
    fireEvent.change(screen.getByLabelText(/Phone Number/i), {
      target: { value: '000' },
    });
    fireEvent.change(screen.getByLabelText(/^Password \*/i), {
      target: { value: 'password123' },
    });
    fireEvent.change(screen.getByLabelText(/Confirm Password \*/i), {
      target: { value: 'password123' },
    });

    await act(async () => {
      fireEvent.click(screen.getByRole('button', { name: /Register/i }));
    });

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(
        'Registration has failed'
      );
    });
    expect(mockNavigate).not.toHaveBeenCalled();
  });

  test('renders link to login page', () => {
    renderRegisterPageWithContext({});
    const loginLink = screen.getByRole('link', { name: /Log In/i });
    expect(loginLink).toBeInTheDocument();
    expect(loginLink).toHaveAttribute('href', '/login');
    expect(screen.getByText(/Already have an account\?/i)).toBeInTheDocument();
  });

  test('password and confirm password validation: mismatch', async () => {
    renderRegisterPageWithContext({ register: mockRegisterFn });

    fireEvent.change(screen.getByRole('textbox', { name: /Username/i }), {
      target: { value: 'testuser' },
    });
    fireEvent.change(screen.getByRole('textbox', { name: /Email Address/i }), {
      target: { value: 'test@example.com' },
    });
    fireEvent.change(screen.getByLabelText(/^Password \*/i), {
      target: { value: 'password123' },
    });
    fireEvent.change(screen.getByLabelText(/Confirm Password \*/i), {
      target: { value: 'password456' },
    }); // Mismatch

    await act(async () => {
      fireEvent.click(screen.getByRole('button', { name: /Register/i }));
    });

    const alertElement = await screen.findByRole('alert');
    expect(alertElement).toHaveTextContent(/passwords do not match/i);

    expect(mockRegisterFn).not.toHaveBeenCalled();
  });
});
