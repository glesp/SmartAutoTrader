import React, {
  useState,
  useEffect,
  useContext,
  ChangeEvent,
  FormEvent,
  useRef,
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
  IconButton,
  Card,
  CardMedia,
  CardActions,
} from '@mui/material';
import { AuthContext } from '../contexts/AuthContext';
import {
  vehicleService,
  UpdateVehiclePayload,
  VehicleFeaturePayload,
  Vehicle as VehicleModel,
} from '../services/api';
import DeleteIcon from '@mui/icons-material/Delete';
import CloudUploadIcon from '@mui/icons-material/CloudUpload';
import StarIcon from '@mui/icons-material/Star';

// Enums definitions unchanged...
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

const getFullImageUrl = (imagePath?: string): string => {
  if (!imagePath) return '/images/placeholder.jpg';
  return imagePath;
};

const AdminEditVehiclePage: React.FC = () => {
  const { vehicleId } = useParams<{ vehicleId: string }>();
  const navigate = useNavigate();
  const { user, loading: authLoading } = useContext(AuthContext);
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Existing state variables
  const [initialLoading, setInitialLoading] = useState<boolean>(true);
  const [formData, setFormData] = useState<UpdateVehiclePayload | null>(null);
  const [featuresInput, setFeaturesInput] = useState<string>('');
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<
    Partial<Record<keyof UpdateVehiclePayload | 'features' | 'images', string>>
  >({});

  // Add new state variables for image management
  const [existingImages, setExistingImages] = useState<
    Array<{ id: number; imageUrl: string; isPrimary: boolean }>
  >([]);
  const [imagePreviews, setImagePreviews] = useState<string[]>([]);
  const [uploading, setUploading] = useState(false);
  const [imageMessage, setImageMessage] = useState<{
    text: string;
    type: 'success' | 'error';
  } | null>(null);

  // Auth check useEffect remains unchanged
  useEffect(() => {
    if (!authLoading && (!user || user.role !== 'Admin')) {
      navigate('/login', {
        state: { from: `/admin/vehicles/edit/${vehicleId}` },
      });
    }
  }, [user, authLoading, navigate, vehicleId]);

  // Clean up image preview URLs
  useEffect(() => {
    return () => {
      imagePreviews.forEach((url) => URL.revokeObjectURL(url));
    };
  }, [imagePreviews]);

  // Fetch vehicle data effect
  useEffect(() => {
    if (!vehicleId) {
      setError('Vehicle ID is missing.');
      setInitialLoading(false);
      return;
    }
    const fetchVehicleData = async () => {
      try {
        const vehicleData: VehicleModel = await vehicleService.getVehicle(
          parseInt(vehicleId, 10)
        );
        setFormData({
          make: vehicleData.make,
          model: vehicleData.model,
          year: vehicleData.year,
          price: vehicleData.price,
          mileage: vehicleData.mileage,
          fuelType: vehicleData.fuelType,
          transmission: vehicleData.transmission,
          vehicleType: vehicleData.vehicleType,
          engineSize: vehicleData.engineSize,
          horsePower: vehicleData.horsePower,
          country: vehicleData.country || '',
          description: vehicleData.description,
          features: vehicleData.features || [],
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
  }, [vehicleId]);

  // Existing handlers
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

  // New image handlers
  const handleImageSelect = () => {
    fileInputRef.current?.click();
  };

  const handleImageUpload = async (
    event: React.ChangeEvent<HTMLInputElement>
  ) => {
    const files = event.target.files;
    if (!files || files.length === 0 || !vehicleId) return;

    setUploading(true);
    setImageMessage(null);

    try {
      const file = files[0];

      // Create a preview for the selected file
      const previewUrl = URL.createObjectURL(file);
      setImagePreviews([...imagePreviews, previewUrl]);

      // Upload the image file
      const response = await vehicleService.uploadVehicleImage(
        parseInt(vehicleId),
        file
      );

      // Update the existing images list with the new image
      if (response) {
        setExistingImages([
          ...existingImages,
          {
            id: response.id,
            imageUrl: response.imageUrl,
            isPrimary: response.isPrimary,
          },
        ]);
        setImageMessage({
          text: 'Image uploaded successfully',
          type: 'success',
        });
      }
    } catch (error) {
      console.error('Error uploading image:', error);
      setImageMessage({ text: 'Failed to upload image', type: 'error' });

      // Remove the last preview if upload fails
      if (imagePreviews.length > 0) {
        const lastPreview = imagePreviews[imagePreviews.length - 1];
        URL.revokeObjectURL(lastPreview);
        setImagePreviews(imagePreviews.slice(0, -1));
      }
    } finally {
      setUploading(false);
      // Clear the file input
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    }
  };

  const handleDeleteImage = async (imageId: number) => {
    if (!vehicleId) return;

    try {
      await vehicleService.deleteVehicleImage(parseInt(vehicleId), imageId);

      // Remove the deleted image from the state
      setExistingImages(existingImages.filter((img) => img.id !== imageId));
      setImageMessage({ text: 'Image deleted successfully', type: 'success' });
    } catch (error) {
      console.error('Error deleting image:', error);
      setImageMessage({ text: 'Failed to delete image', type: 'error' });
    }
  };

  const handleSetPrimaryImage = async (imageId: number) => {
    if (!vehicleId) return;

    try {
      await vehicleService.setPrimaryVehicleImage(parseInt(vehicleId), imageId);

      // Update the primary status in the local state
      setExistingImages(
        existingImages.map((img) => ({
          ...img,
          isPrimary: img.id === imageId,
        }))
      );
      setImageMessage({
        text: 'Primary image set successfully',
        type: 'success',
      });
    } catch (error) {
      console.error('Error setting primary image:', error);
      setImageMessage({ text: 'Failed to set primary image', type: 'error' });
    }
  };

  // Continue with existing form validation and submit handlers
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
      newErrors.price = 'Price seems too high (max 10,000,000).';

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
        `Vehicle "${vehiclePayload.make} ${vehiclePayload.model}" (ID: ${vehicleId}) updated successfully.`
      );
    } catch (err: unknown) {
      // Your existing error handling code...
      console.error('Error updating vehicle:', err);
      let errorMessage =
        'Failed to update vehicle. Please check console for details.';

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

  // Loading, auth check, and data check branches
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

  // Main form render
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
        </Typography>
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
            {/* Vehicle details fields - unchanged */}
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

            {/* New image management section */}
            <Grid item xs={12}>
              <Typography variant="h6" sx={{ mt: 2, mb: 2 }}>
                Image Management
              </Typography>

              {imageMessage && (
                <Alert severity={imageMessage.type} sx={{ mb: 2 }}>
                  {imageMessage.text}
                </Alert>
              )}

              {/* Hidden file input */}
              <input
                type="file"
                ref={fileInputRef}
                style={{ display: 'none' }}
                accept="image/*"
                onChange={handleImageUpload}
              />

              {/* Upload button */}
              <Button
                variant="contained"
                color="primary"
                startIcon={<CloudUploadIcon />}
                onClick={handleImageSelect}
                disabled={uploading}
                sx={{ mb: 3 }}
              >
                {uploading ? 'Uploading...' : 'Upload New Image'}
              </Button>

              {/* Image Gallery */}
              <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2, mb: 4 }}>
                {existingImages.length > 0 ? (
                  existingImages.map((image) => (
                    <Card
                      key={image.id}
                      sx={{ width: 200, position: 'relative' }}
                    >
                      <CardMedia
                        component="img"
                        height="140"
                        image={getFullImageUrl(image.imageUrl)}
                        alt="Vehicle image"
                      />
                      <CardActions disableSpacing>
                        <IconButton
                          aria-label="set as primary"
                          color={image.isPrimary ? 'primary' : 'default'}
                          disabled={image.isPrimary}
                          onClick={() => handleSetPrimaryImage(image.id)}
                          title={
                            image.isPrimary ? 'Primary Image' : 'Set as Primary'
                          }
                        >
                          <StarIcon />
                        </IconButton>
                        <IconButton
                          aria-label="delete"
                          color="error"
                          onClick={() => handleDeleteImage(image.id)}
                          sx={{ marginLeft: 'auto' }}
                        >
                          <DeleteIcon />
                        </IconButton>
                      </CardActions>
                      {image.isPrimary && (
                        <Box
                          sx={{
                            position: 'absolute',
                            top: 8,
                            right: 8,
                            backgroundColor: 'primary.main',
                            color: 'white',
                            px: 1,
                            py: 0.5,
                            borderRadius: 1,
                            fontSize: '0.75rem',
                          }}
                        >
                          Primary
                        </Box>
                      )}
                    </Card>
                  ))
                ) : (
                  <Typography color="textSecondary">
                    No images uploaded yet
                  </Typography>
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
