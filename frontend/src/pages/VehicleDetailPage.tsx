// src/pages/VehicleDetailPage.tsx
import { useState, useEffect, useContext } from 'react';
import { useParams, Link } from 'react-router-dom';
import { vehicleService, favoriteService } from '../services/api';
import { AuthContext } from '../contexts/AuthContext';
import API_URL from '../services/api';
// Add Material-UI imports
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

// Define what your API actually returns
interface VehicleImage {
  id: number;
  imageUrl: string;
  isPrimary: boolean;
}

// Define a type for the ASP.NET Core reference format
interface ReferenceWrapper {
  $id?: string;
  $values: VehicleImage[];
}

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

const VehicleDetailPage = () => {
  const { id } = useParams<{ id: string }>();
  const [vehicle, setVehicle] = useState<ApiVehicle | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeImageIndex, setActiveImageIndex] = useState(0);
  const [isFavorite, setIsFavorite] = useState(false);
  const [checkingFavorite, setCheckingFavorite] = useState(false);
  const [favAnim, setFavAnim] = useState(false);
  const { isAuthenticated } = useContext(AuthContext);

  useEffect(() => {
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
    const checkFavorite = async () => {
      if (!isAuthenticated || !id) return;

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
  }, [id, isAuthenticated]);

  useEffect(() => {
    if (isFavorite !== undefined) {
      setFavAnim(true);
      const timeout = setTimeout(() => setFavAnim(false), 200);
      return () => clearTimeout(timeout);
    }
  }, [isFavorite]);

  const handleToggleFavorite = async () => {
    if (!isAuthenticated || !id) return;

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

  const getImageArray = (): VehicleImage[] => {
    if (!vehicle) return [];

    if (Array.isArray(vehicle.images)) {
      return vehicle.images;
    } else if (
      typeof vehicle.images === 'object' &&
      vehicle.images !== null &&
      '$values' in vehicle.images
    ) {
      const imagesWithValues = vehicle.images as { $values: VehicleImage[] };
      return imagesWithValues.$values;
    }
    return [];
  };

  const getImageUrl = (image: VehicleImage | undefined) => {
    if (!image || !image.imageUrl) return '/images/placeholder.jpg';
    return `${API_URL}/${image.imageUrl.replace(/^\/+/, '')}`;
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
    <Container maxWidth="lg" sx={{ py: 4 }}>
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

            {isAuthenticated ? (
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
