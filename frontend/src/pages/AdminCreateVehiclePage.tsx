/**
 * @file AdminCreateVehiclePage.tsx
 * @summary Provides the `AdminCreateVehiclePage` component, which allows administrators to create new vehicle listings.
 *
 * @description The `AdminCreateVehiclePage` component is a form-based interface for administrators to create new vehicle listings.
 * It includes fields for vehicle details such as make, model, year, price, mileage, fuel type, transmission, and more.
 * The component also supports image uploads, feature management, and form validation. It interacts with the backend API to create vehicles
 * and upload images, and it handles error and success states gracefully.
 *
 * @remarks
 * - The component uses Material-UI for layout and styling, including components such as `Container`, `Paper`, `TextField`, `Select`, and `Button`.
 * - React Router is used for navigation, enabling redirection based on user roles and authentication status.
 * - The `AuthContext` is used to determine if the user is authenticated and has the required admin role.
 * - The component includes extensive form validation and error handling to ensure data integrity.
 *
 * @dependencies
 * - React: `useState`, `useEffect`, `useContext` for managing state and side effects.
 * - Material-UI: Components for layout, styling, and form controls.
 * - React Router: `useNavigate`, `Navigate` for navigation and redirection.
 * - `AuthContext`: For managing user authentication and role-based access control.
 * - `vehicleService`: For interacting with the backend API to create vehicles and upload images.
 *
 * @example
 * <AdminCreateVehiclePage />
 */

import React, {
  useState,
  useEffect,
  useContext,
  ChangeEvent,
  FormEvent,
} from 'react';
import { useNavigate, Navigate } from 'react-router-dom';
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
  IconButton,
  SelectChangeEvent,
} from '@mui/material';
import DeleteIcon from '@mui/icons-material/Delete';
import { AuthContext } from '../contexts/AuthContext';
import {
  vehicleService,
  VehicleCreatePayload,
  VehicleFeaturePayload,
} from '../services/api';
import { Vehicle } from '../types/models';

/**
 * @enum FuelTypeFrontend
 * @summary Enum for fuel types used in the frontend.
 */
enum FuelTypeFrontend {
  Petrol = 'Petrol',
  Diesel = 'Diesel',
  Electric = 'Electric',
  Hybrid = 'Hybrid',
  PluginHybrid = 'PluginHybrid',
}

/**
 * @enum TransmissionTypeFrontend
 * @summary Enum for transmission types used in the frontend.
 */
enum TransmissionTypeFrontend {
  Manual = 'Manual',
  Automatic = 'Automatic',
  SemiAutomatic = 'SemiAutomatic',
}

/**
 * @enum VehicleTypeFrontend
 * @summary Enum for vehicle types used in the frontend.
 */
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

/**
 * @function AdminCreateVehiclePage
 * @summary Renders a page for administrators to create new vehicle listings.
 *
 * @returns {JSX.Element} The rendered admin create vehicle page component.
 *
 * @remarks
 * - The component includes form fields for vehicle details, image uploads, and feature management.
 * - It validates form inputs and displays error messages for invalid fields.
 * - The component interacts with the backend API to create vehicles and upload images.
 * - Only users with the "Admin" role can access this page.
 *
 * @example
 * <AdminCreateVehiclePage />
 */
const AdminCreateVehiclePage: React.FC = () => {
  const navigate = useNavigate();
  const { user, loading: authLoading } = useContext(AuthContext);

  /**
   * @constant initialFormData
   * @summary The initial state for the vehicle creation form.
   */
  const initialFormData: VehicleCreatePayload = {
    make: '',
    model: '',
    year: new Date().getFullYear(),
    price: 0,
    mileage: undefined,
    fuelType: FuelTypeFrontend.Petrol,
    transmission: TransmissionTypeFrontend.Manual,
    vehicleType: VehicleTypeFrontend.Sedan,
    engineSize: undefined,
    horsePower: undefined,
    country: '',
    description: '',
    features: [],
  };

  const [formData, setFormData] =
    useState<VehicleCreatePayload>(initialFormData);
  const [featuresInput, setFeaturesInput] = useState<string>('');
  const [selectedFiles, setSelectedFiles] = useState<File[]>([]);
  const [imagePreviews, setImagePreviews] = useState<string[]>([]);
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<
    Partial<Record<keyof VehicleCreatePayload | 'features' | 'images', string>>
  >({});

  useEffect(() => {
    if (!authLoading && (!user || user.role !== 'Admin')) {
      navigate('/login', { state: { from: '/admin/vehicles/create' } });
    }
  }, [user, authLoading, navigate]);

  useEffect(() => {
    // Clean up object URLs
    return () => {
      imagePreviews.forEach((url) => URL.revokeObjectURL(url));
    };
  }, [imagePreviews]);

  /**
   * @function handleChange
   * @summary Handles changes to form input fields.
   *
   * @param {ChangeEvent<HTMLInputElement | HTMLTextAreaElement | { name?: string; value: unknown }>} e - The change event.
   */
  const handleChange = (
    e: ChangeEvent<
      HTMLInputElement | HTMLTextAreaElement | { name?: string; value: unknown }
    >
  ) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name as string]: value }));
    if (fieldErrors[name as keyof VehicleCreatePayload]) {
      setFieldErrors((prev) => ({
        ...prev,
        [name as keyof VehicleCreatePayload]: undefined,
      }));
    }
  };

  /**
   * @function handleSelectChange
   * @summary Handles changes to select dropdown fields.
   *
   * @param {SelectChangeEvent<string>} e - The select change event.
   * @param {keyof VehicleCreatePayload} fieldName - The name of the field being updated.
   */
  const handleSelectChange = (
    e: SelectChangeEvent<string>,
    fieldName: keyof VehicleCreatePayload
  ) => {
    setFormData((prev) => ({ ...prev, [fieldName]: e.target.value as string }));
    if (fieldErrors[fieldName]) {
      setFieldErrors((prev) => ({ ...prev, [fieldName]: undefined }));
    }
  };

  /**
   * @function handleFeatureInputChange
   * @summary Handles changes to the features input field.
   *
   * @param {ChangeEvent<HTMLInputElement>} e - The change event.
   */
  const handleFeatureInputChange = (e: ChangeEvent<HTMLInputElement>) => {
    setFeaturesInput(e.target.value);
    if (fieldErrors.features) {
      setFieldErrors((prev) => ({ ...prev, features: undefined }));
    }
  };

  /**
   * @function handleFileChange
   * @summary Handles file selection for image uploads.
   *
   * @param {ChangeEvent<HTMLInputElement>} e - The file input change event.
   */
  const handleFileChange = (e: ChangeEvent<HTMLInputElement>) => {
    if (e.target.files) {
      const filesArray = Array.from(e.target.files);
      setSelectedFiles((prev) => [...prev, ...filesArray]);

      const newPreviews = filesArray.map((file) => URL.createObjectURL(file));
      setImagePreviews((prev) => [...prev, ...newPreviews]);
    }
    if (fieldErrors.images) {
      setFieldErrors((prev) => ({ ...prev, images: undefined }));
    }
  };

  /**
   * @function handleRemoveImage
   * @summary Removes an image from the selected files and previews.
   *
   * @param {number} index - The index of the image to remove.
   */
  const handleRemoveImage = (index: number) => {
    URL.revokeObjectURL(imagePreviews[index]);
    setImagePreviews((prev) => prev.filter((_, i) => i !== index));
    setSelectedFiles((prev) => prev.filter((_, i) => i !== index));
  };

  interface FormData {
    make: string;
    model: string;
    year: string;
    price: string;
    mileage: string;
    fuelType: string;
    transmission: string;
    vehicleType: string;
    engineSize: string;
    horsePower: string;
    description: string;
    country?: string;
    // ... any other fields
  }

  interface FormErrors {
    make?: string;
    model?: string;
    year?: string;
    price?: string;
    mileage?: string;
    fuelType?: string;
    transmission?: string;
    vehicleType?: string;
    engineSize?: string;
    horsePower?: string;
    description?: string;
    country?: string;
    // ... any other fields
  }

  // Assuming you have a component state for formData and setErrors
  // const [formData, setFormData] = useState<FormData>({...});
  // const [errors, setErrors] = useState<FormErrors>({});

  const validateForm = (): boolean => {
    // Map the component's state 'formData' (VehicleCreatePayload)
    // to the 'formDataForValidation' structure (local FormData interface).
    const formDataForValidation: FormData = {
      make: formData.make ?? '',
      model: formData.model ?? '',
      year: formData.year !== undefined ? String(formData.year) : '',
      price: formData.price !== undefined ? String(formData.price) : '',
      mileage: formData.mileage !== undefined ? String(formData.mileage) : '',
      fuelType: formData.fuelType ?? '',
      transmission: formData.transmission ?? '',
      vehicleType: formData.vehicleType ?? '',
      engineSize:
        formData.engineSize !== undefined ? String(formData.engineSize) : '',
      horsePower:
        formData.horsePower !== undefined ? String(formData.horsePower) : '',
      description: formData.description ?? '',
      country: formData.country ?? '',
    };

    const newErrors: FormErrors = {};
    const currentYear = new Date().getFullYear();

    // Make validation
    if (!formDataForValidation.make.trim()) {
      newErrors.make = 'Make is required.';
    } else if (formDataForValidation.make.trim().length < 2) {
      newErrors.make = 'Make must be at least 2 characters long.';
    } else if (formDataForValidation.make.trim().length > 50) {
      newErrors.make = 'Make cannot exceed 50 characters.';
    }

    // Model validation
    if (!formDataForValidation.model.trim()) {
      newErrors.model = 'Model is required.';
    } else if (formDataForValidation.model.trim().length < 1) {
      newErrors.model = 'Model must be at least 1 character long.';
    } else if (formDataForValidation.model.trim().length > 50) {
      newErrors.model = 'Model cannot exceed 50 characters.';
    }

    // Year validation
    if (!formDataForValidation.year.trim()) {
      newErrors.year = 'Year is required.';
    } else {
      const yearNum = parseInt(formDataForValidation.year, 10);
      if (isNaN(yearNum)) {
        newErrors.year = 'Year must be a valid number.';
      } else if (yearNum < 1900 || yearNum > currentYear + 1) {
        newErrors.year = `Year must be between 1900 and ${currentYear + 1}.`;
      }
    }

    // Price validation
    if (!formDataForValidation.price.trim()) {
      newErrors.price = 'Price is required.';
    } else {
      const priceNum = parseFloat(formDataForValidation.price);
      if (isNaN(priceNum)) {
        newErrors.price = 'Price must be a valid number.';
      } else if (priceNum <= 0) {
        newErrors.price = 'Price must be greater than 0.';
      } else if (priceNum > 10000000) {
        newErrors.price = 'Price seems too high (max 10,000,000).';
      }
    }

    // Mileage validation (optional)
    if (formDataForValidation.mileage && formDataForValidation.mileage.trim()) {
      const mileageNum = parseInt(formDataForValidation.mileage, 10);
      if (isNaN(mileageNum)) {
        newErrors.mileage = 'Mileage must be a valid number.';
      } else if (mileageNum < 0) {
        newErrors.mileage = 'Mileage cannot be negative.';
      } else if (mileageNum > 1000000) {
        newErrors.mileage = 'Mileage seems too high (max 1,000,000).';
      }
    }

    // Fuel Type validation (required)
    if (
      !formDataForValidation.fuelType ||
      formDataForValidation.fuelType.trim() === ''
    ) {
      newErrors.fuelType = 'Fuel Type is required.';
    }

    // Transmission validation (required)
    if (
      !formDataForValidation.transmission ||
      formDataForValidation.transmission.trim() === ''
    ) {
      newErrors.transmission = 'Transmission is required.';
    }

    // Vehicle Type validation (required)
    if (
      !formDataForValidation.vehicleType ||
      formDataForValidation.vehicleType.trim() === ''
    ) {
      newErrors.vehicleType = 'Vehicle Type is required.';
    }

    // Engine Size validation (optional)
    if (
      formDataForValidation.engineSize &&
      formDataForValidation.engineSize.trim()
    ) {
      const engineSizeNum = parseFloat(formDataForValidation.engineSize);
      if (isNaN(engineSizeNum)) {
        newErrors.engineSize = 'Engine Size must be a valid number.';
      } else if (engineSizeNum <= 0 || engineSizeNum > 15.0) {
        newErrors.engineSize = 'Engine Size must be between 0.1 and 15.0 L.';
      }
    }

    // Horsepower validation (optional)
    if (
      formDataForValidation.horsePower &&
      formDataForValidation.horsePower.trim()
    ) {
      const horsePowerNum = parseInt(formDataForValidation.horsePower, 10);
      if (isNaN(horsePowerNum)) {
        newErrors.horsePower = 'Horsepower must be a valid number.';
      } else if (horsePowerNum <= 0 || horsePowerNum > 2000) {
        newErrors.horsePower = 'Horsepower must be between 1 and 2000.';
      }
    }

    // Description validation
    if (!formDataForValidation.description.trim()) {
      newErrors.description = 'Description is required.';
    } else if (formDataForValidation.description.trim().length < 10) {
      newErrors.description =
        'Description must be at least 10 characters long.';
    } else if (formDataForValidation.description.trim().length > 5000) {
      newErrors.description = 'Description cannot exceed 5000 characters.';
    }

    // Country validation (if any specific rules were needed, they'd go here)
    // Example: if (formDataForValidation.country && formDataForValidation.country.trim().length > 50) {
    //   newErrors.country = 'Country name cannot exceed 50 characters.';
    // }

    setFieldErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  /**
   * @function handleSubmit
   * @summary Handles form submission to create a new vehicle.
   *
   * @param {FormEvent<HTMLFormElement>} e - The form submission event.
   */
  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError(null);
    setSuccess(null);

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

    const vehiclePayload: VehicleCreatePayload = {
      ...formData,
      year: Number(formData.year),
      price: Number(formData.price),
      mileage: formData.mileage ? Number(formData.mileage) : undefined,
      engineSize: formData.engineSize ? Number(formData.engineSize) : undefined,
      horsePower: formData.horsePower ? Number(formData.horsePower) : undefined,
      features: parsedFeatures,
    };

    try {
      const newVehicle: Vehicle =
        await vehicleService.createVehicle(vehiclePayload);
      setSuccess(
        `Vehicle "${newVehicle.make} ${newVehicle.model}" created successfully with ID: ${newVehicle.id}. Uploading images...`
      );

      if (selectedFiles.length > 0) {
        let imagesUploadedCount = 0;
        for (const file of selectedFiles) {
          try {
            await vehicleService.uploadVehicleImage(newVehicle.id, file);
            imagesUploadedCount++;
          } catch (imgErr) {
            console.error('Error uploading image:', imgErr);
            setError((prevError) =>
              prevError
                ? prevError + ` Failed to upload image: ${file.name}.`
                : `Failed to upload image: ${file.name}.`
            );
          }
        }
        setSuccess(
          (prevSuccess) =>
            prevSuccess +
            ` ${imagesUploadedCount}/${selectedFiles.length} images uploaded.`
        );
      } else {
        setSuccess(
          (prevSuccess) => prevSuccess + ` No images were selected for upload.`
        );
      }

      setFormData(initialFormData); // Reset form
      setFeaturesInput('');
      setSelectedFiles([]);
      setImagePreviews([]);
      setFieldErrors({});
    } catch (err: unknown) {
      console.error('Error creating vehicle:', err);

      interface ApiErrorResponse {
        data?: {
          message?: string;
          errors?: Record<string, string[] | string>;
        };
      }

      interface ErrorWithMessage {
        message?: string;
      }

      let errorMessage =
        'Failed to create vehicle. Please check console for details.';
      const formattedErrors: Partial<
        Record<keyof VehicleCreatePayload | 'features' | 'images', string>
      > = {};

      if (
        typeof err === 'object' &&
        err !== null &&
        'response' in err &&
        typeof (err as { response?: ApiErrorResponse }).response === 'object'
      ) {
        const response = (err as { response: ApiErrorResponse }).response;
        errorMessage =
          response?.data?.message ||
          (err as ErrorWithMessage).message ||
          errorMessage;

        if (response?.data?.errors) {
          const backendErrors = response.data.errors;
          for (const key in backendErrors) {
            const lowerKey = key.toLowerCase() as keyof VehicleCreatePayload; // Assuming backend keys might not be lowercase
            const errorValue = backendErrors[key];
            formattedErrors[lowerKey] = Array.isArray(errorValue)
              ? errorValue.join(', ')
              : String(errorValue);
          }
        }
      } else if (typeof err === 'object' && err !== null && 'message' in err) {
        errorMessage = (err as ErrorWithMessage).message || errorMessage;
      }

      setError(errorMessage);
      if (Object.keys(formattedErrors).length > 0) {
        setFieldErrors((prev) => ({ ...prev, ...formattedErrors }));
      }
    } finally {
      setLoading(false);
    }
  };

  if (authLoading || (!user && !authLoading)) {
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
  if (user?.role !== 'Admin') {
    return <Navigate to="/" />;
  }

  return (
    <Container maxWidth="md" sx={{ py: 4 }}>
      <Paper elevation={3} sx={{ p: 4 }}>
        <Typography variant="h4" component="h1" gutterBottom>
          Create New Vehicle Listing
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
                Upload Images
              </Typography>
              <Button variant="contained" component="label" sx={{ mb: 1 }}>
                Select Images
                <input
                  type="file"
                  hidden
                  multiple
                  accept="image/*"
                  onChange={handleFileChange}
                />
              </Button>
              {fieldErrors.images && (
                <Typography color="error" variant="caption" display="block">
                  {fieldErrors.images}
                </Typography>
              )}
              <Box display="flex" flexWrap="wrap" gap={1} mt={1}>
                {imagePreviews.map((previewUrl, index) => (
                  <Box
                    key={index}
                    sx={{
                      position: 'relative',
                      border: '1px solid #ddd',
                      p: 0.5,
                    }}
                  >
                    <img
                      src={previewUrl}
                      alt={`preview ${index}`}
                      style={{ width: 100, height: 100, objectFit: 'cover' }}
                    />
                    <IconButton
                      size="small"
                      onClick={() => handleRemoveImage(index)}
                      sx={{
                        position: 'absolute',
                        top: 0,
                        right: 0,
                        backgroundColor: 'rgba(255,255,255,0.7)',
                      }}
                    >
                      <DeleteIcon fontSize="small" />
                    </IconButton>
                  </Box>
                ))}
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
                  'Create Vehicle'
                )}
              </Button>
            </Grid>
          </Grid>
        </Box>
      </Paper>
    </Container>
  );
};

export default AdminCreateVehiclePage;
