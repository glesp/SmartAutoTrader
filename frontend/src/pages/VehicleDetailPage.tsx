/**
 * @file VehicleDetailPage.tsx
 * @summary Provides the `VehicleDetailPage` component, which displays detailed information about a specific vehicle.
 *
 * @description The `VehicleDetailPage` component fetches and displays detailed information about a specific vehicle, including its images,
 * specifications, and description. It allows authenticated users to save the vehicle to their favorites and send inquiries. Admin users
 * can navigate to the edit page for the vehicle. The component handles API interactions for fetching vehicle details and managing favorites.
 *
 * @remarks
 * - The component uses Material-UI for layout and styling, including components such as `Container`, `Paper`, `Grid`, and `Button`.
 * - React Router is used for navigation, enabling redirection and dynamic routing based on the vehicle ID.
 * - The `AuthContext` is used to check the user's authentication status and role.
 * - The `vehicleService` and `favoriteService` are used to fetch vehicle details and manage favorite status, respectively.
 * - Error handling is implemented to display fallback content in case of API failures.
 *
 * @dependencies
 * - React: `useState`, `useEffect`, `useContext` for managing state and accessing the authentication context.
 * - Material-UI: Components for layout, styling, and icons.
 * - React Router: `useParams`, `Link`, `useNavigate` for navigation and dynamic routing.
 * - `AuthContext`: For managing user authentication and access control.
 * - `vehicleService`: For fetching vehicle details from the backend API.
 * - `favoriteService`: For managing the user's favorite vehicles.
 *
 * @example
 * <VehicleDetailPage />
 */

import { useState, useEffect, useContext, JSX } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { vehicleService, favoriteService } from '../services/api';
import { AuthContext } from '../contexts/AuthContext';
import {
  Box,
  Typography,
  Container,
  Grid,
  Paper,
  Button,
  Breadcrumbs,
  Link as MuiLink,
  ButtonBase,
  Divider,
  IconButton,
  CircularProgress,
} from '@mui/material';
import FavoriteIcon from '@mui/icons-material/Favorite';
import FavoriteBorderIcon from '@mui/icons-material/FavoriteBorder';
import EditIcon from '@mui/icons-material/Edit';

/**
 * @interface VehicleImage
 * @summary Represents an image associated with a vehicle.
 *
 * @property {number} id - The unique identifier for the image.
 * @property {string} imageUrl - The URL of the image.
 * @property {boolean} isPrimary - Indicates whether the image is the primary image for the vehicle.
 */
interface VehicleImage {
  id: number;
  imageUrl: string;
  isPrimary: boolean;
}

/**
 * @interface ReferenceWrapper
 * @summary Represents a wrapper for ASP.NET Core reference format data.
 *
 * @property {string} [$id] - The unique identifier for the reference.
 * @property {VehicleImage[]} $values - The array of vehicle images.
 */
interface ReferenceWrapper {
  $id?: string;
  $values: VehicleImage[];
}

/**
 * @interface ApiVehicle
 * @summary Represents the structure of a vehicle object returned by the API.
 *
 * @property {number} id - The unique identifier for the vehicle.
 * @property {string} make - The make of the vehicle.
 * @property {string} model - The model of the vehicle.
 * @property {number} year - The year of manufacture of the vehicle.
 * @property {number} price - The price of the vehicle.
 * @property {number} mileage - The mileage of the vehicle in kilometers.
 * @property {string} fuelType - The fuel type of the vehicle (e.g., Petrol, Diesel).
 * @property {string} transmission - The transmission type of the vehicle (e.g., Manual, Automatic).
 * @property {string} vehicleType - The body type of the vehicle (e.g., Sedan, SUV).
 * @property {string} description - A detailed description of the vehicle.
 * @property {VehicleImage[] | ReferenceWrapper | null | undefined} images - The images associated with the vehicle.
 */
interface ApiVehicle {
  id: number;
  make: string;
  model: string;
  year: number;
  price: number;
  mileage: number;
  fuelType: string;
  transmission: string;
  vehicleType: string;
  description: string;
  images: VehicleImage[] | ReferenceWrapper | null | undefined;
}

/**
 * @function VehicleDetailPage
 * @summary Renders the vehicle detail page, displaying detailed information about a specific vehicle.
 *
 * @returns {JSX.Element} The rendered vehicle detail page component.
 *
 * @remarks
 * - The component fetches vehicle details from the backend API based on the vehicle ID from the URL parameters.
 * - It allows authenticated users to save the vehicle to their favorites and send inquiries.
 * - Admin users can navigate to the edit page for the vehicle.
 * - The component handles API interactions for fetching vehicle details and managing favorite status.
 *
 * @example
 * <VehicleDetailPage />
 */
const VehicleDetailPage = (): JSX.Element => {
  const { id } = useParams<{ id: string }>();
  const { user } = useContext(AuthContext);
  const navigate = useNavigate();
  const [vehicle, setVehicle] = useState<ApiVehicle | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeImageIndex, setActiveImageIndex] = useState(0);
  const [isFavorite, setIsFavorite] = useState(false);
  const [checkingFavorite, setCheckingFavorite] = useState(false);
  const [favAnim, setFavAnim] = useState(false);

  useEffect(() => {
    /**
     * @function fetchVehicle
     * @summary Fetches the details of the vehicle from the backend API.
     *
     * @throws Will set an error message if the API request fails.
     */
    const fetchVehicle = async () => {
      if (!id) return;

      setLoading(true);
      setError(null);

      try {
        const data = await vehicleService.getVehicle(parseInt(id));
        setVehicle(data);

        let imageArray: VehicleImage[] = [];
        if (data.images) {
          if (Array.isArray(data.images)) {
            imageArray = data.images;
          } else if (
            typeof data.images === 'object' &&
            data.images !== null &&
            '$values' in data.images
          ) {
            const imagesWithValues = data.images as { $values: VehicleImage[] };
            imageArray = imagesWithValues.$values;
          }
        }

        if (imageArray.length > 0) {
          const primaryIndex = imageArray.findIndex((img) => img.isPrimary);
          setActiveImageIndex(primaryIndex >= 0 ? primaryIndex : 0);
        }
      } catch (err) {
        console.error('Error fetching vehicle:', err);
        setError('Failed to load vehicle details');
      } finally {
        setLoading(false);
      }
    };

    fetchVehicle();
  }, [id]);

  useEffect(() => {
    /**
     * @function checkFavorite
     * @summary Checks if the vehicle is marked as a favorite by the user.
     *
     * @throws Will log an error if the API request fails.
     */
    const checkFavorite = async () => {
      if (!id) return;

      setCheckingFavorite(true);
      try {
        const favoriteStatus = await favoriteService.checkFavorite(
          parseInt(id)
        );
        setIsFavorite(favoriteStatus);
      } catch (err) {
        console.error('Error checking favorite status:', err);
      } finally {
        setCheckingFavorite(false);
      }
    };

    checkFavorite();
  }, [id]);

  useEffect(() => {
    if (isFavorite !== undefined) {
      setFavAnim(true);
      const timeout = setTimeout(() => setFavAnim(false), 200);
      return () => clearTimeout(timeout);
    }
  }, [isFavorite]);

  /**
   * @function handleToggleFavorite
   * @summary Toggles the favorite status of the vehicle for the user.
   *
   * @throws Will log an error if the API request fails.
   */
  const handleToggleFavorite = async () => {
    if (!id) return;

    try {
      if (isFavorite) {
        await favoriteService.removeFavorite(parseInt(id));
      } else {
        await favoriteService.addFavorite(parseInt(id));
      }
      setIsFavorite(!isFavorite);
    } catch (err) {
      console.error('Error toggling favorite:', err);
    }
  };

  /**
   * @function getImageArray
   * @summary Retrieves the array of images associated with the vehicle.
   *
   * @returns {VehicleImage[]} The array of vehicle images.
   */
  const getImageArray = (): VehicleImage[] => {
    if (!vehicle) return [];

    if (Array.isArray(vehicle.images)) {
      return vehicle.images;
    } else if (
      typeof vehicle.images === 'object' &&
      vehicle.images !== null &&
      '$values' in vehicle.images &&
      Array.isArray((vehicle.images as ReferenceWrapper).$values)
    ) {
      return (vehicle.images as ReferenceWrapper).$values;
    }
    return [];
  };

  /**
   * @function getImageUrl
   * @summary Retrieves the URL of a specific vehicle image.
   *
   * @param {VehicleImage | undefined} image - The vehicle image object.
   * @returns {string} The URL of the image or a placeholder URL if the image is undefined.
   */
  const getImageUrl = (image: VehicleImage | undefined): string => {
    if (!image || !image.imageUrl) {
      return '/images/placeholder.jpg'; // Local frontend placeholder
    }
    return image.imageUrl; // This is now the full public URL
  };

  if (loading) {
    return (
      <Container maxWidth="lg" sx={{ py: 8 }}>
        <Box
          display="flex"
          justifyContent="center"
          alignItems="center"
          minHeight="300px"
        >
          <Typography>Loading vehicle details...</Typography>
        </Box>
      </Container>
    );
  }

  if (error || !vehicle) {
    return (
      <Container maxWidth="lg" sx={{ py: 8 }}>
        <Paper
          sx={{
            p: 4,
            textAlign: 'center',
            bgcolor: 'error.light',
            color: 'error.dark',
          }}
        >
          <Typography variant="h6" gutterBottom>
            {error || 'Vehicle not found'}
          </Typography>
          <Button
            variant="contained"
            component={Link}
            to="/vehicles"
            sx={{ mt: 2 }}
          >
            Back to Vehicles
          </Button>
        </Paper>
      </Container>
    );
  }

  const imageArray = getImageArray();

  return (
    <Container maxWidth="lg" sx={{ py: 8 }}>
      {/* Breadcrumbs */}
      <Breadcrumbs sx={{ mb: 3 }}>
        <MuiLink component={Link} to="/" underline="hover" color="inherit">
          Home
        </MuiLink>
        <MuiLink
          component={Link}
          to="/vehicles"
          underline="hover"
          color="inherit"
        >
          Vehicles
        </MuiLink>
        <Typography color="text.primary">
          {vehicle.year} {vehicle.make} {vehicle.model}
        </Typography>
      </Breadcrumbs>

      {/* Edit button for Admins */}
      {user && user.role === 'Admin' && vehicle && (
        <Box sx={{ mb: 2, display: 'flex', justifyContent: 'flex-end' }}>
          <Button
            variant="contained"
            color="secondary"
            startIcon={<EditIcon />}
            onClick={() => navigate(`/admin/vehicles/edit/${vehicle.id}`)}
          >
            Edit Vehicle
          </Button>
        </Box>
      )}

      {/* Main content Grid container */}
      <Grid container spacing={4}>
        {/* Left column - Vehicle Images */}
        <Grid item xs={12} md={6}>
          {/* Main image */}
          <Paper
            elevation={2}
            sx={{
              mb: 2,
              borderRadius: 2,
              overflow: 'hidden',
              maxHeight: '450px',
              display: 'flex',
              justifyContent: 'center',
              alignItems: 'center',
            }}
          >
            {imageArray.length > 0 ? (
              <Box
                component="img"
                src={getImageUrl(imageArray[activeImageIndex])}
                alt={`${vehicle.make} ${vehicle.model}`}
                sx={{
                  width: '100%',
                  height: '100%',
                  objectFit: 'contain',
                  maxHeight: '450px',
                }}
              />
            ) : (
              <Box sx={{ p: 4, textAlign: 'center' }}>
                <Typography color="text.secondary">
                  No image available
                </Typography>
              </Box>
            )}
          </Paper>

          {/* Thumbnails */}
          {imageArray.length > 1 && (
            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: {
                  xs: 'repeat(3, 1fr)',
                  sm: 'repeat(5, 1fr)',
                },
                gap: 1,
                mb: { xs: 4, md: 0 },
              }}
            >
              {imageArray.map((image, index) => (
                <ButtonBase
                  key={image.id}
                  onClick={() => setActiveImageIndex(index)}
                  sx={{
                    border:
                      index === activeImageIndex
                        ? '2px solid #1976d2'
                        : '2px solid transparent',
                    borderRadius: 1,
                    overflow: 'hidden',
                    height: '70px',
                  }}
                >
                  <Box
                    component="img"
                    src={getImageUrl(image)}
                    alt={`${vehicle.make} ${vehicle.model} thumbnail ${index + 1}`}
                    sx={{
                      width: '100%',
                      height: '100%',
                      objectFit: 'cover',
                    }}
                  />
                </ButtonBase>
              ))}
            </Box>
          )}
        </Grid>

        {/* Right column - Vehicle details */}
        <Grid item xs={12} md={6}>
          <Paper
            elevation={3}
            sx={{
              p: 3,
              borderRadius: 2,
              position: { md: 'sticky' },
              top: { md: '24px' },
            }}
          >
            <Typography variant="h4" gutterBottom fontWeight="bold">
              {vehicle.year} {vehicle.make} {vehicle.model}
            </Typography>

            <Typography
              variant="h4"
              sx={{ color: 'primary.main', fontWeight: 'bold', mb: 3 }}
            >
              €{vehicle.price.toLocaleString()}
            </Typography>

            <Divider sx={{ mb: 2 }} />

            <Grid container spacing={2} sx={{ mb: 3 }}>
              <Grid item xs={6}>
                <Typography color="text.secondary">Mileage</Typography>
                <Typography fontWeight="500">
                  {vehicle.mileage.toLocaleString()} km
                </Typography>
              </Grid>
              <Grid item xs={6}>
                <Typography color="text.secondary">Fuel Type</Typography>
                <Typography fontWeight="500">{vehicle.fuelType}</Typography>
              </Grid>
              <Grid item xs={6}>
                <Typography color="text.secondary">Transmission</Typography>
                <Typography fontWeight="500">{vehicle.transmission}</Typography>
              </Grid>
              <Grid item xs={6}>
                <Typography color="text.secondary">Body Type</Typography>
                <Typography fontWeight="500">{vehicle.vehicleType}</Typography>
              </Grid>
            </Grid>

            {/* Buttons */}
            <Button
              variant="contained"
              component={Link}
              to={`/inquiries/new?vehicleId=${vehicle.id}`}
              fullWidth
              size="large"
              sx={{ mb: 2 }}
            >
              Send Inquiry
            </Button>

            {user ? (
              <IconButton
                onClick={handleToggleFavorite}
                disabled={checkingFavorite} // Disable button while checking favorite status
                sx={{
                  transition: 'transform 0.2s',
                  transform: favAnim ? 'scale(1.15)' : 'scale(1.0)',
                  color: isFavorite ? 'error.main' : 'grey.500',
                }}
              >
                {checkingFavorite ? (
                  <CircularProgress size={24} color="inherit" />
                ) : isFavorite ? (
                  <FavoriteIcon />
                ) : (
                  <FavoriteBorderIcon />
                )}
              </IconButton>
            ) : (
              <Button variant="outlined" component={Link} to="/login" fullWidth>
                Login to Save to Favorites
              </Button>
            )}
          </Paper>
        </Grid>

        {/* Description section - Full width below the other content */}
        <Grid item xs={12}>
          <Box sx={{ mt: { xs: 0, md: 4 } }}>
            <Typography variant="h5" gutterBottom fontWeight="500">
              Description
            </Typography>
            <Paper elevation={1} sx={{ p: 3, borderRadius: 2 }}>
              <Typography variant="body1" sx={{ whiteSpace: 'pre-line' }}>
                {vehicle.description}
              </Typography>
            </Paper>
          </Box>
        </Grid>
      </Grid>
    </Container>
  );
};

export default VehicleDetailPage;
