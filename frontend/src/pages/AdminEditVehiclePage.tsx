import React, {
  useState,
  useEffect,
  useContext,
  ChangeEvent,
  FormEvent,
} from 'react';
import {
  useNavigate,
  useParams,
  Navigate,
  Link as RouterLink,
} from 'react-router-dom';
import {
  Container,
  Paper,
  Typography,
  TextField,
  Button,
  Box,
  Grid,
  Select,
  MenuItem,
  InputLabel,
  FormControl,
  CircularProgress,
  Alert,
  SelectChangeEvent,
  Breadcrumbs,
  Link,
} from '@mui/material';
import { AuthContext } from '../contexts/AuthContext';
import {
  vehicleService,
  UpdateVehiclePayload, // Use the new payload type
  VehicleFeaturePayload,
  Vehicle as VehicleModel, // Existing frontend Vehicle model
} from '../services/api';

// Assuming these enums are defined as in AdminCreateVehiclePage.tsx
enum FuelTypeFrontend {
  Petrol = 'Petrol',
  Diesel = 'Diesel',
  Electric = 'Electric',
  Hybrid = 'Hybrid',
  PluginHybrid = 'PluginHybrid',
}
enum TransmissionTypeFrontend {
  Manual = 'Manual',
  Automatic = 'Automatic',
  SemiAutomatic = 'SemiAutomatic',
}
enum VehicleTypeFrontend {
  Sedan = 'Sedan',
  SUV = 'SUV',
  Hatchback = 'Hatchback',
  Coupe = 'Coupe',
  Convertible = 'Convertible',
  Minivan = 'Minivan',
  Truck = 'Truck',
  Van = 'Van',
}

// Helper to get image URL, assuming similar logic to VehicleCard or VehicleDetailPage
// You might want to centralize this helper if used in multiple places.
const getFullImageUrl = (imagePath?: string): string => {
  if (!imagePath) return '/images/placeholder.jpg'; // Default placeholder
  // Assuming imagePath is already a full URL from the backend
  return imagePath;
};

const AdminEditVehiclePage: React.FC = () => {
  const { vehicleId } = useParams<{ vehicleId: string }>();
  const navigate = useNavigate(); // Ensure navigate is initialized
  const { user, loading: authLoading } = useContext(AuthContext);

  const [initialLoading, setInitialLoading] = useState<boolean>(true);
  const [formData, setFormData] = useState<UpdateVehiclePayload | null>(null);
  const [featuresInput, setFeaturesInput] = useState<string>('');
  // For displaying existing images (not for new uploads in this form)
  const [existingImages, setExistingImages] = useState<
    Array<{ id: number; imageUrl: string; isPrimary: boolean }>
  >([]);

  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<
    Partial<Record<keyof UpdateVehiclePayload | 'features', string>>
  >({});

  useEffect(() => {
    if (!authLoading && (!user || user.role !== 'Admin')) {
      navigate('/login', {
        state: { from: `/admin/vehicles/edit/${vehicleId}` },
      }); // Use vehicleId here
    }
  }, [user, authLoading, navigate, vehicleId]); // Use vehicleId here

  useEffect(() => {
    if (!vehicleId) {
      // Use vehicleId here
      setError('Vehicle ID is missing.');
      setInitialLoading(false);
      return;
    }
    const fetchVehicleData = async () => {
      try {
        const vehicleData: VehicleModel = await vehicleService.getVehicle(
          parseInt(vehicleId, 10)
        ); // Use vehicleId here
        setFormData({
          make: vehicleData.make,
          model: vehicleData.model,
          year: vehicleData.year,
          price: vehicleData.price,
          mileage: vehicleData.mileage,
          fuelType: vehicleData.fuelType, // Assuming backend returns string compatible with FuelTypeFrontend
          transmission: vehicleData.transmission, // Assuming backend returns string compatible
          vehicleType: vehicleData.vehicleType, // Assuming backend returns string compatible
          engineSize: vehicleData.engineSize,
          horsePower: vehicleData.horsePower,
          country: vehicleData.country || '',
          description: vehicleData.description,
          features: vehicleData.features || [], // Ensure features is an array
        });
        setFeaturesInput(
          (vehicleData.features || []).map((f) => f.name).join(', ')
        );
        setExistingImages(vehicleData.images || []);
      } catch (err) {
        console.error('Error fetching vehicle data:', err);
        setError(
          'Failed to load vehicle data. It might have been deleted or an error occurred.'
        );
      } finally {
        setInitialLoading(false);
      }
    };
    fetchVehicleData();
  }, [vehicleId]); // Use vehicleId here

  const handleChange = (
    e: ChangeEvent<
      HTMLInputElement | HTMLTextAreaElement | { name?: string; value: unknown }
    >
  ) => {
    const { name, value } = e.target;
    setFormData((prev) => (prev ? { ...prev, [name as string]: value } : null));
    if (fieldErrors[name as keyof UpdateVehiclePayload]) {
      setFieldErrors((prev) => ({
        ...prev,
        [name as keyof UpdateVehiclePayload]: undefined,
      }));
    }
  };

  const handleSelectChange = (
    e: SelectChangeEvent<string>,
    fieldName: keyof UpdateVehiclePayload
  ) => {
    setFormData((prev) =>
      prev ? { ...prev, [fieldName]: e.target.value as string } : null
    );
    if (fieldErrors[fieldName]) {
      setFieldErrors((prev) => ({ ...prev, [fieldName]: undefined }));
    }
  };

  const handleFeatureInputChange = (e: ChangeEvent<HTMLInputElement>) => {
    setFeaturesInput(e.target.value);
    if (fieldErrors.features) {
      setFieldErrors((prev) => ({ ...prev, features: undefined }));
    }
  };

  // Basic validation, adapt from AdminCreateVehiclePage or make more specific
  const validateForm = (): boolean => {
    if (!formData) return false;
    const newErrors: Partial<
      Record<keyof UpdateVehiclePayload | 'features', string>
    > = {};
    const currentYear = new Date().getFullYear();

    if (!formData.make.trim()) newErrors.make = 'Make is required.';
    else if (formData.make.trim().length < 2)
      newErrors.make = 'Make must be at least 2 characters.';
    else if (formData.make.trim().length > 50)
      newErrors.make = 'Make cannot exceed 50 characters.';

    if (!formData.model.trim()) newErrors.model = 'Model is required.';
    else if (formData.model.trim().length < 1)
      newErrors.model = 'Model must be at least 1 character.';
    else if (formData.model.trim().length > 50)
      newErrors.model = 'Model cannot exceed 50 characters.';

    if (isNaN(Number(formData.year)))
      newErrors.year = 'Year must be a valid number.';
    else if (
      Number(formData.year) < 1900 ||
      Number(formData.year) > currentYear + 1
    )
      newErrors.year = `Year must be between 1900 and ${currentYear + 1}.`;

    if (isNaN(Number(formData.price)))
      newErrors.price = 'Price must be a valid number.';
    else if (Number(formData.price) <= 0)
      newErrors.price = 'Price must be greater than 0.';
    else if (Number(formData.price) > 10000000)
      newErrors.price = 'Price seems to high (max 10,000,000).';

    if (
      formData.mileage !== undefined &&
      formData.mileage !== null &&
      (isNaN(Number(formData.mileage)) || Number(formData.mileage) < 0)
    ) {
      newErrors.mileage = 'Mileage must be a non-negative number.';
    } else if (formData.mileage && Number(formData.mileage) > 1000000) {
      newErrors.mileage = 'Mileage seems too high (max 1,000,000).';
    }

    if (!formData.fuelType) newErrors.fuelType = 'Fuel Type is required.';
    if (!formData.transmission)
      newErrors.transmission = 'Transmission is required.';
    if (!formData.vehicleType)
      newErrors.vehicleType = 'Vehicle Type is required.';

    if (
      formData.engineSize !== undefined &&
      formData.engineSize !== null &&
      (isNaN(Number(formData.engineSize)) ||
        Number(formData.engineSize) <= 0 ||
        Number(formData.engineSize) > 15.0)
    ) {
      newErrors.engineSize =
        'Engine Size must be a positive number between 0.1 and 15.0 L.';
    }
    if (
      formData.horsePower !== undefined &&
      formData.horsePower !== null &&
      (isNaN(Number(formData.horsePower)) ||
        Number(formData.horsePower) <= 0 ||
        Number(formData.horsePower) > 2000)
    ) {
      newErrors.horsePower =
        'Horsepower must be a positive number between 1 and 2000.';
    }

    if (!formData.description.trim())
      newErrors.description = 'Description is required.';
    else if (formData.description.trim().length < 10)
      newErrors.description = 'Description must be at least 10 characters.';
    else if (formData.description.trim().length > 5000)
      newErrors.description = 'Description cannot exceed 5000 characters.';

    setFieldErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError(null);
    setSuccess(null);

    if (!formData || !vehicleId) {
      setError('Form data or Vehicle ID is missing.');
      return;
    }

    if (!validateForm()) {
      setError('Please correct the errors in the form.');
      return;
    }

    setLoading(true);

    const parsedFeatures: VehicleFeaturePayload[] = featuresInput
      .split(',')
      .map((f) => f.trim())
      .filter((f) => f)
      .map((name) => ({ name }));

    const vehiclePayload: UpdateVehiclePayload = {
      ...formData,
      year: Number(formData.year),
      price: Number(formData.price),
      mileage: formData.mileage ? Number(formData.mileage) : undefined,
      engineSize: formData.engineSize ? Number(formData.engineSize) : undefined,
      horsePower: formData.horsePower ? Number(formData.horsePower) : undefined,
      features: parsedFeatures,
    };

    try {
      await vehicleService.updateVehicle(
        parseInt(vehicleId, 10),
        vehiclePayload
      );
      setSuccess(
        `Vehicle "${vehiclePayload.make} ${vehiclePayload.model}" (ID: ${vehicleId}) updated successfully. Redirecting...`
      );

      // Navigate to admin dashboard after a delay
      setTimeout(() => {
        navigate('/admin/dashboard');
      }, 2500); // 2.5 second delay
    } catch (err: unknown) {
      console.error('Error updating vehicle:', err);
      let errorMessage =
        'Failed to update vehicle. Please check console for details.';
      // Basic error handling, can be expanded like in AdminCreateVehiclePage
      if (typeof err === 'object' && err !== null && 'response' in err) {
        const response = (
          err as {
            response?: {
              data?: { message?: string; errors?: Record<string, string[]> };
            };
          }
        ).response;
        errorMessage =
          response?.data?.message ||
          (err as unknown as Error)?.message ||
          errorMessage;
        if (response?.data?.errors) {
          const backendErrors = response.data.errors;
          const formattedErrors: Partial<
            Record<keyof UpdateVehiclePayload | 'features', string>
          > = {};
          for (const key in backendErrors) {
            const lowerKey = key.toLowerCase() as keyof UpdateVehiclePayload;
            formattedErrors[lowerKey] = Array.isArray(backendErrors[key])
              ? (backendErrors[key] as string[]).join(', ')
              : String(backendErrors[key]);
          }
          setFieldErrors((prev) => ({ ...prev, ...formattedErrors }));
        }
      } else if (err instanceof Error) {
        errorMessage = err.message;
      }
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  if (authLoading || initialLoading) {
    return (
      <Box
        display="flex"
        justifyContent="center"
        alignItems="center"
        minHeight="80vh"
      >
        <CircularProgress />
      </Box>
    );
  }

  if (!user || user.role !== 'Admin') {
    // This should be caught by the useEffect redirect, but as a fallback
    return <Navigate to="/" />;
  }

  if (!formData) {
    return (
      <Container maxWidth="md" sx={{ py: 4 }}>
        <Paper elevation={3} sx={{ p: 4 }}>
          <Typography variant="h5" color="error.main">
            {error || 'Could not load vehicle data for editing.'}
          </Typography>
          <Button component={RouterLink} to="/admin/dashboard" sx={{ mt: 2 }}>
            Back to Dashboard
          </Button>
        </Paper>
      </Container>
    );
  }

  return (
    <Container maxWidth="md" sx={{ py: 4 }}>
      <Breadcrumbs aria-label="breadcrumb" sx={{ mb: 2 }}>
        <Link
          component={RouterLink}
          underline="hover"
          color="inherit"
          to="/admin/dashboard"
        >
          Admin
        </Link>
        <Link
          component={RouterLink}
          underline="hover"
          color="inherit"
          to="/admin/vehicles"
        >
          Vehicles
        </Link>
        <Typography color="text.primary">
          Edit Vehicle (ID: {vehicleId})
        </Typography>{' '}
        {/* Use vehicleId here */}
      </Breadcrumbs>
      <Paper elevation={3} sx={{ p: 4 }}>
        <Typography variant="h4" component="h1" gutterBottom>
          Edit Vehicle Listing
        </Typography>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}
        {success && (
          <Alert severity="success" sx={{ mb: 2 }}>
            {success}
          </Alert>
        )}

        <Box component="form" onSubmit={handleSubmit} noValidate>
          <Grid container spacing={3}>
            {/* Fields similar to AdminCreateVehiclePage, pre-filled with formData */}
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Make"
                name="make"
                value={formData.make}
                onChange={handleChange}
                required
                error={!!fieldErrors.make}
                helperText={fieldErrors.make}
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Model"
                name="model"
                value={formData.model}
                onChange={handleChange}
                required
                error={!!fieldErrors.model}
                helperText={fieldErrors.model}
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Year"
                name="year"
                type="number"
                value={formData.year}
                onChange={handleChange}
                required
                error={!!fieldErrors.year}
                helperText={fieldErrors.year}
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Price (€)"
                name="price"
                type="number"
                value={formData.price}
                onChange={handleChange}
                required
                InputProps={{ inputProps: { min: 0, step: '0.01' } }}
                error={!!fieldErrors.price}
                helperText={fieldErrors.price}
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Mileage (km)"
                name="mileage"
                type="number"
                value={formData.mileage || ''}
                onChange={handleChange}
                InputProps={{ inputProps: { min: 0 } }}
                error={!!fieldErrors.mileage}
                helperText={fieldErrors.mileage}
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Country"
                name="country"
                value={formData.country || ''}
                onChange={handleChange}
                error={!!fieldErrors.country}
                helperText={fieldErrors.country}
              />
            </Grid>

            <Grid item xs={12} sm={6}>
              <FormControl fullWidth required error={!!fieldErrors.fuelType}>
                <InputLabel id="fuel-type-label">Fuel Type</InputLabel>
                <Select
                  labelId="fuel-type-label"
                  name="fuelType"
                  value={formData.fuelType}
                  label="Fuel Type"
                  onChange={(e) => handleSelectChange(e, 'fuelType')}
                >
                  {Object.values(FuelTypeFrontend).map((type) => (
                    <MenuItem key={type} value={type}>
                      {type}
                    </MenuItem>
                  ))}
                </Select>
                {fieldErrors.fuelType && (
                  <Typography color="error" variant="caption">
                    {fieldErrors.fuelType}
                  </Typography>
                )}
              </FormControl>
            </Grid>
            <Grid item xs={12} sm={6}>
              <FormControl
                fullWidth
                required
                error={!!fieldErrors.transmission}
              >
                <InputLabel id="transmission-type-label">
                  Transmission
                </InputLabel>
                <Select
                  labelId="transmission-type-label"
                  name="transmission"
                  value={formData.transmission}
                  label="Transmission"
                  onChange={(e) => handleSelectChange(e, 'transmission')}
                >
                  {Object.values(TransmissionTypeFrontend).map((type) => (
                    <MenuItem key={type} value={type}>
                      {type}
                    </MenuItem>
                  ))}
                </Select>
                {fieldErrors.transmission && (
                  <Typography color="error" variant="caption">
                    {fieldErrors.transmission}
                  </Typography>
                )}
              </FormControl>
            </Grid>
            <Grid item xs={12} sm={6}>
              <FormControl fullWidth required error={!!fieldErrors.vehicleType}>
                <InputLabel id="vehicle-type-label">Vehicle Type</InputLabel>
                <Select
                  labelId="vehicle-type-label"
                  name="vehicleType"
                  value={formData.vehicleType}
                  label="Vehicle Type"
                  onChange={(e) => handleSelectChange(e, 'vehicleType')}
                >
                  {Object.values(VehicleTypeFrontend).map((type) => (
                    <MenuItem key={type} value={type}>
                      {type}
                    </MenuItem>
                  ))}
                </Select>
                {fieldErrors.vehicleType && (
                  <Typography color="error" variant="caption">
                    {fieldErrors.vehicleType}
                  </Typography>
                )}
              </FormControl>
            </Grid>

            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Engine Size (L)"
                name="engineSize"
                type="number"
                value={formData.engineSize || ''}
                onChange={handleChange}
                InputProps={{ inputProps: { min: 0, step: '0.1' } }}
                error={!!fieldErrors.engineSize}
                helperText={fieldErrors.engineSize}
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Horsepower (HP)"
                name="horsePower"
                type="number"
                value={formData.horsePower || ''}
                onChange={handleChange}
                InputProps={{ inputProps: { min: 0 } }}
                error={!!fieldErrors.horsePower}
                helperText={fieldErrors.horsePower}
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Description"
                name="description"
                value={formData.description}
                onChange={handleChange}
                multiline
                rows={4}
                required
                error={!!fieldErrors.description}
                helperText={fieldErrors.description}
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Features (comma-separated)"
                value={featuresInput}
                onChange={handleFeatureInputChange}
                helperText="e.g., Sunroof, Leather Seats, GPS"
                error={!!fieldErrors.features}
              />
              {fieldErrors.features && (
                <Typography color="error" variant="caption">
                  {fieldErrors.features}
                </Typography>
              )}
            </Grid>

            <Grid item xs={12}>
              <Typography variant="subtitle1" gutterBottom>
                Existing Images
              </Typography>
              <Typography variant="caption" display="block" sx={{ mb: 1 }}>
                Image uploads are handled separately. Use the "Upload Images"
                feature on the vehicle detail page if needed.
              </Typography>
              <Box display="flex" flexWrap="wrap" gap={1} mt={1}>
                {existingImages.length > 0 ? (
                  existingImages.map((image, index) => (
                    <Box
                      key={image.id || index}
                      sx={{ border: '1px solid #ddd', p: 0.5 }}
                    >
                      <img
                        src={getFullImageUrl(image.imageUrl)}
                        alt={`vehicle ${index}`}
                        style={{ width: 100, height: 100, objectFit: 'cover' }}
                      />
                    </Box>
                  ))
                ) : (
                  <Typography>No images available.</Typography>
                )}
              </Box>
            </Grid>

            <Grid item xs={12}>
              <Button
                type="submit"
                variant="contained"
                color="primary"
                disabled={loading}
                fullWidth
                sx={{ mt: 2, py: 1.5 }}
              >
                {loading ? (
                  <CircularProgress size={24} color="inherit" />
                ) : (
                  'Save Changes'
                )}
              </Button>
            </Grid>
          </Grid>
        </Box>
      </Paper>
    </Container>
  );
};

export default AdminEditVehiclePage;
