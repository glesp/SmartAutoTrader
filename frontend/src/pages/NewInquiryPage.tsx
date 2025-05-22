/**
 * @file NewInquiryPage.tsx
 * @summary Provides the `NewInquiryPage` component, which allows users to send inquiries about specific vehicles.
 *
 * @description The `NewInquiryPage` component renders a form for users to submit inquiries about a specific vehicle.
 * It fetches vehicle details based on the `vehicleId` query parameter, validates the form inputs, and sends the inquiry to the backend API.
 * The component ensures that only authenticated users can access the page and redirects unauthenticated users to the login page.
 *
 * @remarks
 * - The component uses Material-UI for layout and styling, including components such as `Container`, `Paper`, `TextField`, and `Button`.
 * - React Router is used for navigation, enabling redirection to the login page for unauthenticated users and navigation after successful inquiry submission.
 * - The `AuthContext` is used to check the user's authentication status.
 * - Error handling is implemented to display appropriate messages for missing vehicle details, invalid form inputs, or failed API requests.
 *
 * @dependencies
 * - React: `useState`, `useEffect`, `useContext` for managing state and accessing the authentication context.
 * - Material-UI: Components for layout, styling, and form controls.
 * - React Router: `useNavigate`, `useSearchParams`, `Link` for navigation and query parameter handling.
 * - `AuthContext`: For managing user authentication and access control.
 * - `inquiryService`: For sending inquiries to the backend API.
 * - `vehicleService`: For fetching vehicle details from the backend API.
 *
 * @example
 * <NewInquiryPage />
 */

import {
  useState,
  useEffect,
  useContext,
  ChangeEvent,
  FormEvent,
  JSX,
} from 'react';
import { useNavigate, useSearchParams, Link } from 'react-router-dom';
import { inquiryService, vehicleService } from '../services/api';
import { AuthContext } from '../contexts/AuthContext';
import {
  Container,
  Paper,
  Typography,
  TextField,
  Button,
  Box,
  Alert,
  CircularProgress,
  Breadcrumbs,
} from '@mui/material';
import { Vehicle } from '../types/models';

/**
 * @interface FormData
 * @summary Represents the structure of the inquiry form data.
 *
 * @property {string} subject - The subject of the inquiry.
 * @property {string} message - The message content of the inquiry.
 */
interface FormData {
  subject: string;
  message: string;
}

/**
 * @function NewInquiryPage
 * @summary Renders the page for submitting a new inquiry about a specific vehicle.
 *
 * @returns {JSX.Element} The rendered new inquiry page component.
 *
 * @remarks
 * - The component fetches vehicle details based on the `vehicleId` query parameter and displays them at the top of the form.
 * - It validates the form inputs and ensures that all required fields are filled before submission.
 * - If the user is not authenticated, they are redirected to the login page with the intended destination preserved in the state.
 * - Upon successful submission, the user is redirected to their profile page with a success message.
 *
 * @example
 * <NewInquiryPage />
 */
const NewInquiryPage = (): JSX.Element => {
  const [searchParams] = useSearchParams();
  const vehicleId = searchParams.get('vehicleId');
  const navigate = useNavigate();
  const { isAuthenticated, loading: authLoading } = useContext(AuthContext);

  const [vehicle, setVehicle] = useState<Vehicle | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string>('');
  const [submitting, setSubmitting] = useState<boolean>(false);
  const [formData, setFormData] = useState<FormData>({
    subject: '',
    message: '',
  });

  useEffect(() => {
    if (!vehicleId) {
      setError('Vehicle ID is required');
      setLoading(false);
      return;
    }

    /**
     * @function fetchVehicle
     * @summary Fetches the details of the vehicle associated with the inquiry.
     *
     * @throws Will set an error message if the vehicle details cannot be fetched.
     */
    const fetchVehicle = async () => {
      try {
        const data = await vehicleService.getVehicle(parseInt(vehicleId));
        setVehicle(data);
      } catch (err) {
        console.error('Error fetching vehicle:', err);
        setError('Failed to load vehicle details');
      } finally {
        setLoading(false);
      }
    };

    fetchVehicle();
  }, [vehicleId]);

  // Redirect if not authenticated
  useEffect(() => {
    if (!authLoading && !isAuthenticated) {
      navigate('/login', {
        state: { from: `/inquiries/new?vehicleId=${vehicleId}` },
      });
    }
  }, [isAuthenticated, authLoading, navigate, vehicleId]);

  /**
   * @function handleChange
   * @summary Handles changes to the form input fields.
   *
   * @param {ChangeEvent<HTMLInputElement | HTMLTextAreaElement>} e - The change event triggered by the input field.
   */
  const handleChange = (
    e: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  /**
   * @function handleSubmit
   * @summary Handles the form submission for sending an inquiry.
   *
   * @param {FormEvent<HTMLFormElement>} e - The form submission event.
   *
   * @throws Will set an error message if the form inputs are invalid or the API request fails.
   *
   * @remarks
   * - The function validates the form inputs and ensures that the `vehicleId` is present.
   * - Upon successful submission, the user is redirected to their profile page with a success message.
   */
  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    if (!formData.subject.trim() || !formData.message.trim()) {
      setError('Please complete all required fields');
      return;
    }

    if (!vehicleId) {
      setError('Vehicle ID is missing');
      return;
    }

    setSubmitting(true);
    setError('');

    try {
      await inquiryService.createInquiry({
        vehicleId: parseInt(vehicleId),
        subject: formData.subject,
        message: formData.message,
      });

      navigate('/profile', {
        state: {
          activeTab: 'inquiries',
          success: 'Inquiry sent successfully!',
        },
      });
    } catch (err) {
      console.error('Error sending inquiry:', err);
      setError('Failed to send inquiry. Please try again.');
    } finally {
      setSubmitting(false);
    }
  };

  if (authLoading || loading) {
    return (
      <Container maxWidth="md" sx={{ py: 8, textAlign: 'center' }}>
        <CircularProgress />
        <Typography sx={{ mt: 2 }}>Loading...</Typography>
      </Container>
    );
  }

  return (
    <Container maxWidth="md" sx={{ py: 4 }}>
      {/* Breadcrumbs */}
      <Breadcrumbs sx={{ mb: 3 }}>
        <Link to="/" style={{ textDecoration: 'none', color: 'inherit' }}>
          Home
        </Link>
        <Link
          to="/vehicles"
          style={{ textDecoration: 'none', color: 'inherit' }}
        >
          Vehicles
        </Link>
        {vehicle && vehicleId && (
          <Link
            to={`/vehicles/${vehicleId}`}
            style={{ textDecoration: 'none', color: 'inherit' }}
          >
            {vehicle.year} {vehicle.make} {vehicle.model}
          </Link>
        )}
        <Typography color="text.primary">New Inquiry</Typography>
      </Breadcrumbs>

      <Paper elevation={2} sx={{ p: 4, borderRadius: 2 }}>
        <Typography variant="h4" component="h1" gutterBottom>
          Send Inquiry
        </Typography>

        {vehicle && (
          <Typography variant="h6" color="text.secondary" gutterBottom>
            Regarding: {vehicle.year} {vehicle.make} {vehicle.model}
          </Typography>
        )}

        {error && (
          <Alert severity="error" sx={{ mb: 3 }}>
            {error}
          </Alert>
        )}

        <Box component="form" onSubmit={handleSubmit} sx={{ mt: 3 }}>
          <TextField
            fullWidth
            label="Subject"
            name="subject"
            value={formData.subject}
            onChange={handleChange}
            margin="normal"
            required
            disabled={submitting}
          />

          <TextField
            fullWidth
            label="Message"
            name="message"
            value={formData.message}
            onChange={handleChange}
            margin="normal"
            required
            multiline
            rows={6}
            disabled={submitting}
            helperText="Please include any questions you have about this vehicle"
          />

          <Box sx={{ mt: 3, display: 'flex', gap: 2 }}>
            <Button
              variant="contained"
              color="primary"
              type="submit"
              disabled={submitting}
              sx={{ minWidth: 120 }}
            >
              {submitting ? <CircularProgress size={24} /> : 'Send Inquiry'}
            </Button>

            <Button
              variant="outlined"
              component={Link}
              to={vehicleId ? `/vehicles/${vehicleId}` : '/vehicles'}
              disabled={submitting}
            >
              Cancel
            </Button>
          </Box>
        </Box>
      </Paper>
    </Container>
  );
};

export default NewInquiryPage;
