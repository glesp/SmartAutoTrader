/**
 * @file RegisterPage.tsx
 * @summary Provides the `RegisterPage` component, which allows users to create a new account for the Smart Auto Trader application.
 *
 * @description The `RegisterPage` component renders a registration form where users can enter their details to create a new account.
 * It validates the input fields, ensures that passwords match, and interacts with the `AuthContext` to perform the registration operation.
 * Upon successful registration, users are redirected to the home page or a specified destination.
 *
 * @remarks
 * - The component uses Material-UI for layout and styling, including components such as `Container`, `Paper`, `TextField`, and `Button`.
 * - React Router is used for navigation, enabling redirection after successful registration.
 * - The `AuthContext` is used to handle the registration operation and manage authentication state.
 * - Error handling is implemented to display appropriate messages for invalid inputs or failed registration attempts.
 *
 * @dependencies
 * - React: `useState`, `useContext` for managing state and accessing the authentication context.
 * - Material-UI: Components for layout, styling, and form controls.
 * - React Router: `useNavigate`, `RouterLink` for navigation and redirection.
 * - `AuthContext`: For managing user authentication and registration operations.
 *
 * @example
 * <RegisterPage />
 */

import { useState, useContext, JSX } from 'react';
import { Link as RouterLink, useNavigate } from 'react-router-dom';
import { AuthContext, UserRegistration } from '../contexts/AuthContext';
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
  Grid,
} from '@mui/material';

/**
 * @function RegisterPage
 * @summary Renders the registration page, allowing users to create a new account.
 *
 * @returns {JSX.Element} The rendered registration page component.
 *
 * @remarks
 * - The component validates the input fields, ensuring that all required fields are filled and passwords match.
 * - It displays a loading indicator while the registration request is in progress.
 * - If the registration fails, an error message is displayed to the user.
 * - Upon successful registration, the user is redirected to the home page or a specified destination.
 *
 * @example
 * <RegisterPage />
 */
const RegisterPage = (): JSX.Element => {
  const [formData, setFormData] = useState<UserRegistration>({
    username: '',
    email: '',
    password: '',
    firstName: '',
    lastName: '',
    phoneNumber: '',
  });
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const { register } = useContext(AuthContext);
  const navigate = useNavigate();

  /**
   * @function handleChange
   * @summary Handles changes to the form input fields.
   *
   * @param {React.ChangeEvent<HTMLInputElement>} e - The change event triggered by the input field.
   */
  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: value,
    }));
  };

  /**
   * @function handleSubmit
   * @summary Handles the form submission for user registration.
   *
   * @param {React.FormEvent} e - The form submission event.
   *
   * @throws Will set an error message if the form inputs are invalid or the registration request fails.
   *
   * @remarks
   * - The function validates the form inputs, ensuring that all required fields are filled and passwords match.
   * - Upon successful registration, the user is redirected to the home page or a specified destination.
   */
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!formData.username || !formData.email || !formData.password) {
      setError('Username, email, and password are required');
      return;
    }

    if (formData.password !== confirmPassword) {
      setError('Passwords do not match');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      await register(formData);
      navigate('/', { replace: true }); // Navigate to home or login page after successful registration
    } catch (err: unknown) {
      const errorMessage =
        err instanceof Error
          ? err.message
          : 'Registration failed. Please try again.';
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Container component="main" maxWidth="sm" sx={{ mt: 8, mb: 4 }}>
      <Paper
        elevation={3}
        sx={{
          padding: 4,
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
        }}
      >
        <Typography component="h1" variant="h5" gutterBottom>
          Create an Account
        </Typography>

        {error && (
          <Alert severity="error" sx={{ width: '100%', mb: 2 }}>
            {error}
          </Alert>
        )}

        <Box
          component="form"
          onSubmit={handleSubmit}
          sx={{ mt: 1, width: '100%' }}
        >
          <Grid container spacing={2}>
            <Grid item xs={12}>
              <TextField
                required
                fullWidth
                id="username"
                label="Username"
                name="username"
                autoComplete="username"
                value={formData.username}
                onChange={handleChange}
                disabled={loading}
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                required
                fullWidth
                id="email"
                label="Email Address"
                name="email"
                autoComplete="email"
                value={formData.email}
                onChange={handleChange}
                disabled={loading}
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                id="firstName"
                label="First Name"
                name="firstName"
                autoComplete="given-name"
                value={formData.firstName}
                onChange={handleChange}
                disabled={loading}
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                id="lastName"
                label="Last Name"
                name="lastName"
                autoComplete="family-name"
                value={formData.lastName}
                onChange={handleChange}
                disabled={loading}
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                id="phoneNumber"
                label="Phone Number"
                name="phoneNumber"
                autoComplete="tel"
                value={formData.phoneNumber}
                onChange={handleChange}
                disabled={loading}
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                required
                fullWidth
                name="password"
                label="Password"
                type="password"
                id="password"
                autoComplete="new-password"
                value={formData.password}
                onChange={handleChange}
                disabled={loading}
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                required
                fullWidth
                name="confirmPassword"
                label="Confirm Password"
                type="password"
                id="confirmPassword"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                disabled={loading}
              />
            </Grid>
          </Grid>
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
              'Register'
            )}
          </Button>
          <Grid container justifyContent="flex-end">
            <Grid item>
              <Link component={RouterLink} to="/login" variant="body2">
                Already have an account? Log In
              </Link>
            </Grid>
          </Grid>
        </Box>
      </Paper>
    </Container>
  );
};

export default RegisterPage;
