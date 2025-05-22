/**
 * @file LoginPage.tsx
 * @summary Provides the `LoginPage` component, which allows users to log in to the Smart Auto Trader application.
 *
 * @description The `LoginPage` component renders a login form where users can enter their email and password to authenticate.
 * It validates the input fields, displays error messages for invalid credentials, and redirects authenticated users to their intended destination.
 * The component interacts with the `AuthContext` to perform the login operation and uses React Router for navigation.
 *
 * @remarks
 * - The component uses Material-UI for layout and styling, including components such as `Container`, `Paper`, `TextField`, and `Button`.
 * - React Router is used for navigation, enabling redirection after successful login.
 * - The `AuthContext` is used to handle the login operation and manage authentication state.
 * - Error handling is implemented to display appropriate messages for invalid credentials or failed login attempts.
 *
 * @dependencies
 * - React: `useState`, `useContext` for managing state and accessing the authentication context.
 * - Material-UI: Components for layout, styling, and form controls.
 * - React Router: `useNavigate`, `useLocation`, `RouterLink` for navigation and redirection.
 * - `AuthContext`: For managing user authentication and login operations.
 *
 * @example
 * <LoginPage />
 */

import { useState, useContext, JSX } from 'react';
import { Link as RouterLink, useNavigate, useLocation } from 'react-router-dom';
import { AuthContext } from '../contexts/AuthContext';
import {
  Container,
  Box,
  Typography,
  TextField,
  Button,
  Paper,
  Link,
  CircularProgress,
  Alert,
} from '@mui/material';

/**
 * @function LoginPage
 * @summary Renders the login page, allowing users to authenticate with their email and password.
 *
 * @returns {JSX.Element} The rendered login page component.
 *
 * @remarks
 * - The component validates the email and password fields before submitting the login request.
 * - It displays a loading indicator while the login request is in progress.
 * - If the login fails, an error message is displayed to the user.
 * - Upon successful login, the user is redirected to their intended destination or the home page.
 *
 * @example
 * <LoginPage />
 */
const LoginPage = (): JSX.Element => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const { login } = useContext(AuthContext);
  const navigate = useNavigate();
  const location = useLocation();

  /**
   * @constant from
   * @summary The path to redirect to after successful login.
   *
   * @type {string}
   * @default '/'
   *
   * @remarks
   * - The redirect path is retrieved from the location state or defaults to the home page.
   */
  const from = (location.state as { from?: string })?.from || '/';

  /**
   * @function handleSubmit
   * @summary Handles the form submission for logging in.
   *
   * @param {React.FormEvent} e - The form submission event.
   *
   * @throws Will display an error message if the email or password is missing or if the login request fails.
   *
   * @remarks
   * - The function validates the email and password fields before attempting to log in.
   * - If the login is successful, the user is redirected to the `from` path.
   * - If the login fails, an error message is displayed to the user.
   */
  const handleSubmit = async (e: React.FormEvent): Promise<void> => {
    e.preventDefault();

    if (!email || !password) {
      setError('Please enter your email and password');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      await login(email, password);
      navigate(from, { replace: true });
    } catch (err: unknown) {
      const errorMessage =
        err instanceof Error
          ? err.message
          : 'Failed to login. Please check your credentials.';
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Container component="main" maxWidth="xs" sx={{ mt: 8, mb: 4 }}>
      <Paper
        elevation={3}
        sx={{
          padding: 4,
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
        }}
      >
        <Typography component="h1" variant="h5" sx={{ mb: 3 }}>
          Log In
        </Typography>

        {error && (
          <Alert severity="error" sx={{ width: '100%', mb: 2 }}>
            {error}
          </Alert>
        )}

        <Box component="form" onSubmit={handleSubmit} sx={{ width: '100%' }}>
          <TextField
            margin="normal"
            required
            fullWidth
            id="email"
            label="Email Address"
            name="email"
            autoComplete="email"
            autoFocus
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            disabled={loading}
          />
          <TextField
            margin="normal"
            required
            fullWidth
            name="password"
            label="Password"
            type="password"
            id="password"
            autoComplete="current-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            disabled={loading}
          />
          <Box sx={{ display: 'flex', justifyContent: 'flex-end', my: 1 }}>
            <Link component={RouterLink} to="/forgot-password" variant="body2">
              Forgot Password?
            </Link>
          </Box>
          <Button
            type="submit"
            fullWidth
            variant="contained"
            disabled={loading}
            sx={{ mt: 3, mb: 2, py: 1.5 }}
          >
            {loading ? (
              <CircularProgress size={24} color="inherit" />
            ) : (
              'Log In'
            )}
          </Button>
        </Box>

        <Typography variant="body2" color="text.secondary" align="center">
          Don't have an account?{' '}
          <Link component={RouterLink} to="/register" variant="body2">
            Register
          </Link>
        </Typography>
      </Paper>
    </Container>
  );
};

export default LoginPage;
