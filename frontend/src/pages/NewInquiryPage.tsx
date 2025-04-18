import { useState, useEffect, useContext, ChangeEvent, FormEvent } from 'react';
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

interface FormData {
  subject: string;
  message: string;
}

const NewInquiryPage = () => {
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

  const handleChange = (
    e: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

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
