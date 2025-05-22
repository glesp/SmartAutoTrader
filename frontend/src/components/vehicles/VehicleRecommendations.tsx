/**
 * @file VehicleRecommendations.tsx
 * @summary Defines the `VehicleRecommendations` component, which displays AI-powered vehicle recommendations for authenticated users.
 *
 * @description The `VehicleRecommendations` component renders a list of recommended vehicles based on the user's preferences and interactions with the AI assistant.
 * It displays vehicle details such as make, model, year, price, mileage, and fuel type in a card layout. The component also handles cases where no recommendations are available
 * or the user is not authenticated.
 *
 * @remarks
 * - The component uses Material-UI for layout and styling, including `Box`, `Typography`, `Card`, and `Button` components.
 * - React Router is used for navigation, enabling seamless routing to vehicle detail pages.
 * - The `AuthContext` is used to determine the user's authentication state and conditionally render content.
 * - The component gracefully handles edge cases such as missing images or invalid data.
 *
 * @dependencies
 * - Material-UI components: `Box`, `Typography`, `Card`, `CardMedia`, `CardContent`, `CardActions`, `Button`.
 * - React Router: `Link` for navigation.
 * - Context: `AuthContext` for user authentication state.
 * - Types: `Vehicle`, `VehicleRecommendationsProps` for defining the structure of props and vehicle data.
 */

import { JSX, useContext } from 'react';
import { AuthContext } from '../../contexts/AuthContext';
import { Vehicle } from '../../types/models.ts';
import { VehicleRecommendationsProps } from '../../types/models.ts';
import {
  Box,
  Typography,
  Button,
  Card,
  CardMedia,
  CardContent,
  CardActions,
} from '@mui/material';
import { Link } from 'react-router-dom';

/**
 * @function extractArray
 * @summary Extracts an array from various possible ASP.NET response formats.
 *
 * @template T
 * @param {T[] | { $values: T[] } | undefined} data - The data to extract the array from.
 * @returns {T[]} The extracted array.
 *
 * @remarks
 * - This function handles different formats of array-like data, including ASP.NET's `$values` format.
 * - If the input is undefined or not an array, it returns an empty array.
 *
 * @example
 * const data = { $values: [1, 2, 3] };
 * const result = extractArray(data); // [1, 2, 3]
 */
const extractArray = <T,>(data: T[] | { $values: T[] } | undefined): T[] => {
  if (!data) return [];
  if (Array.isArray(data)) return data;
  if (data && '$values' in data) return (data as { $values: T[] }).$values;
  return [];
};

/**
 * @function VehicleRecommendations
 * @summary Renders a list of AI-powered vehicle recommendations for authenticated users.
 *
 * @param {VehicleRecommendationsProps} props - The props for the component, including the list of recommended vehicles.
 * @returns {JSX.Element} The rendered vehicle recommendations component.
 *
 * @remarks
 * - The component displays a grid of vehicle cards, each showing details such as make, model, year, price, mileage, and fuel type.
 * - If the user is not authenticated, a message prompting them to sign in is displayed.
 * - If no recommendations are available, a placeholder message with example queries is shown.
 *
 * @example
 * <VehicleRecommendations recommendedVehicles={vehicles} />
 */
const VehicleRecommendations = ({
  recommendedVehicles,
}: VehicleRecommendationsProps): JSX.Element => {
  const { user } = useContext(AuthContext);

  /**
   * @function getVehicles
   * @summary Retrieves the list of recommended vehicles from the props.
   *
   * @returns {Vehicle[]} The list of recommended vehicles.
   *
   * @remarks
   * - This function handles different formats of the `recommendedVehicles` prop, including ASP.NET's `$values` format.
   */
  const getVehicles = (): Vehicle[] => {
    if (!recommendedVehicles) return [];
    if (Array.isArray(recommendedVehicles)) return recommendedVehicles;
    if (recommendedVehicles && '$values' in recommendedVehicles) {
      return (recommendedVehicles as { $values: Vehicle[] }).$values;
    }
    return [];
  };

  /**
   * @function getImageUrl
   * @summary Retrieves the URL of the primary image for a vehicle.
   *
   * @param {Vehicle} vehicle - The vehicle object.
   * @returns {string} The URL of the primary image, or a placeholder URL if no valid image is found.
   *
   * @remarks
   * - If the vehicle has no images, a placeholder image is used.
   * - If no primary image is found, the first image in the list is used as a fallback.
   */
  const getImageUrl = (vehicle: Vehicle): string => {
    const images = extractArray<{ imageUrl: string; isPrimary: boolean }>(
      vehicle.images
    );
    if (!images || images.length === 0) {
      return '/images/placeholder.jpg'; // Local frontend placeholder
    }

    const primaryImage = images.find((img) => img.isPrimary);
    const imageToUse = primaryImage || images[0];

    if (imageToUse && imageToUse.imageUrl) {
      return imageToUse.imageUrl;
    }

    return '/images/placeholder.jpg'; // Fallback placeholder if no valid URL found
  };

  /**
   * @function getFuelTypeName
   * @summary Maps a fuel type number or string to its corresponding name.
   *
   * @param {number | string} fuelType - The fuel type to map.
   * @returns {string} The name of the fuel type, or "Unknown" if the value is invalid.
   *
   * @remarks
   * - This function supports both numeric and string representations of fuel types.
   */
  const getFuelTypeName = (fuelType: number | string): string => {
    if (typeof fuelType === 'string') return fuelType;

    const fuelTypes: Record<number, string> = {
      0: 'Petrol',
      1: 'Diesel',
      2: 'Electric',
      3: 'Hybrid',
      4: 'Plugin Hybrid',
    };
    return fuelTypes[fuelType] || 'Unknown';
  };

  // Retrieve the list of vehicles
  const vehicles = getVehicles();

  // Render a message if the user is not authenticated
  if (!user) {
    return (
      <Box p={4}>
        <Typography variant="h6" textAlign="center">
          Please sign in to view recommendations.
        </Typography>
      </Box>
    );
  }

  return (
    <Box p={2}>
      <Typography variant="h4" fontWeight="bold" mb={3}>
        AI-Powered Recommendations
      </Typography>

      {vehicles.length === 0 ? (
        <Box
          textAlign="center"
          py={6}
          px={2}
          bgcolor="background.paper"
          borderRadius={2}
        >
          <Typography variant="h6" mb={2}>
            No recommendations available yet
          </Typography>
          <Typography variant="body1" color="text.secondary" mb={2}>
            Chat with our AI assistant to get personalized vehicle suggestions.
          </Typography>
          <Typography variant="body2" color="text.secondary" fontStyle="italic">
            Try asking: "Show me electric SUVs" or "I need a family car under
            €25,000"
          </Typography>
        </Box>
      ) : (
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: {
              xs: '1fr',
              sm: 'repeat(2, 1fr)',
              md: 'repeat(3, 1fr)',
              lg: 'repeat(4, 1fr)',
            },
            gap: 3,
          }}
        >
          {vehicles.map((vehicle) => (
            <Card
              key={vehicle.id}
              sx={{
                height: '100%',
                display: 'flex',
                flexDirection: 'column',
                transition: 'transform 0.2s, box-shadow 0.2s',
                '&:hover': {
                  transform: 'translateY(-4px)',
                  boxShadow: 4,
                },
                borderRadius: 2,
              }}
              elevation={2}
            >
              <CardMedia
                component="img"
                height={200}
                image={getImageUrl(vehicle)}
                alt={`${vehicle.make} ${vehicle.model}`}
                sx={{ objectFit: 'cover' }}
                onError={(e) => {
                  e.currentTarget.src =
                    'https://via.placeholder.com/300x200?text=No+Image';
                }}
              />
              <CardContent sx={{ flexGrow: 1, pb: 1 }}>
                <Typography
                  gutterBottom
                  variant="h6"
                  component="h3"
                  fontWeight={500}
                >
                  {vehicle.year} {vehicle.make} {vehicle.model}
                </Typography>
                <Typography
                  variant="h6"
                  color="primary"
                  fontWeight={600}
                  mb={1}
                >
                  €{vehicle.price.toLocaleString()}
                </Typography>
                <Box
                  display="flex"
                  justifyContent="space-between"
                  color="text.secondary"
                  mt={1}
                >
                  <Typography variant="body2">
                    {vehicle.mileage.toLocaleString()} km
                  </Typography>
                  <Typography variant="body2">
                    {getFuelTypeName(vehicle.fuelType)}
                  </Typography>
                </Box>
              </CardContent>
              <CardActions sx={{ p: 2, pt: 0 }}>
                <Button
                  component={Link}
                  to={`/vehicles/${vehicle.id}`}
                  variant="contained"
                  color="primary"
                  fullWidth
                  sx={{ borderRadius: 1 }}
                >
                  View Details
                </Button>
              </CardActions>
            </Card>
          ))}
        </Box>
      )}
    </Box>
  );
};

export default VehicleRecommendations;
