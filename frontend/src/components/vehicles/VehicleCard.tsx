/**
 * @file VehicleCard.tsx
 * @summary Defines the `VehicleCard` component, which displays a card representation of a vehicle with its details.
 *
 * @description The `VehicleCard` component is a reusable UI element that displays information about a vehicle, including its image, make, model, year, price, mileage, and fuel type.
 * It also provides an "Edit" button for admin users to navigate to the vehicle editing page. The component is styled using Material-UI and is designed to be responsive and visually appealing.
 *
 * @remarks
 * - The component uses Material-UI for layout and styling, including `Card` and `Button` components.
 * - React Router is used for navigation, enabling seamless routing to vehicle details or editing pages.
 * - The `AuthContext` is used to determine the user's role and conditionally render admin-specific actions.
 * - The component handles edge cases such as missing images or invalid fuel type values gracefully.
 *
 * @dependencies
 * - Material-UI components: `Card`, `Button`
 * - Material-UI icons: `EditIcon`
 * - React Router: `Link`, `useNavigate` for navigation
 * - Context: `AuthContext` for user authentication and role management
 */

import React, { useContext } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { VehicleProps } from '../../types/models';
import Card from '@mui/material/Card';
import Button from '@mui/material/Button';
import EditIcon from '@mui/icons-material/Edit';
import { AuthContext } from '../../contexts/AuthContext';

/**
 * @interface VehicleImage
 * @summary Represents an image associated with a vehicle.
 *
 * @property {string} imageUrl - The URL of the image.
 * @property {boolean} isPrimary - Indicates whether the image is the primary image for the vehicle.
 */
interface VehicleImage {
  imageUrl: string;
  isPrimary: boolean;
}

/**
 * @function extractArray
 * @summary Extracts an array from various possible ASP.NET response formats.
 *
 * @template T
 * @param {T[] | { $values: T[] } | undefined | Record<string, unknown>} data - The data to extract the array from.
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
const extractArray = <T,>(
  data: T[] | { $values: T[] } | undefined | Record<string, unknown>
): T[] => {
  if (!data) return [];
  if (Array.isArray(data)) return data;
  if (data && '$values' in data && Array.isArray(data.$values))
    return data.$values as T[];
  return [];
};

/**
 * @function getFuelTypeName
 * @summary Maps a fuel type number or string to its corresponding name.
 *
 * @param {number | string | undefined} fuelType - The fuel type to map.
 * @returns {string} The name of the fuel type, or "Unknown" if the value is invalid.
 *
 * @remarks
 * - This function supports both numeric and string representations of fuel types.
 * - If the input is undefined or invalid, it returns an empty string or "Unknown".
 *
 * @example
 * const fuelTypeName = getFuelTypeName(2); // "Electric"
 */
const getFuelTypeName = (fuelType?: number | string): string => {
  if (!fuelType) return '';
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

/**
 * @function VehicleCard
 * @summary Renders a card displaying details of a vehicle.
 *
 * @param {VehicleProps} props - The props for the component, including the `vehicle` object.
 * @returns {JSX.Element} The rendered vehicle card component.
 *
 * @remarks
 * - The card includes an image, make, model, year, price, mileage, and fuel type of the vehicle.
 * - Admin users see an "Edit" button that navigates to the vehicle editing page.
 * - The component gracefully handles missing images and invalid data.
 *
 * @example
 * <VehicleCard vehicle={{ id: 1, make: 'Toyota', model: 'Corolla', year: 2020, price: 20000, mileage: 15000, fuelType: 0, images: [] }} />
 */
const VehicleCard: React.FC<VehicleProps> = ({ vehicle }) => {
  const { user } = useContext(AuthContext);
  const navigate = useNavigate();

  /**
   * @function getImageUrl
   * @summary Retrieves the URL of the primary image for the vehicle.
   *
   * @returns {string} The URL of the primary image, or a placeholder URL if no valid image is found.
   *
   * @remarks
   * - If the vehicle has no images, a placeholder image is used.
   * - If no primary image is found, the first image in the list is used as a fallback.
   */
  const getImageUrl = (): string => {
    const images = extractArray<VehicleImage>(vehicle.images);

    if (!images || images.length === 0) {
      return '/images/placeholder.jpg'; // Local frontend placeholder
    }

    const primaryImage = images.find((img) => img.isPrimary);
    const imageToUse = primaryImage || images[0]; // Fallback to the first image

    if (imageToUse && imageToUse.imageUrl) {
      return imageToUse.imageUrl; // This is now the full public URL
    }

    return '/images/placeholder.jpg'; // Fallback placeholder if no valid URL found
  };

  /**
   * @function handleEdit
   * @summary Handles the "Edit" button click for admin users.
   *
   * @param {React.MouseEvent} e - The mouse event triggered by the button click.
   * @returns {void}
   *
   * @remarks
   * - Prevents the default link navigation behavior and stops event propagation.
   * - Navigates to the vehicle editing page.
   */
  const handleEdit = (e: React.MouseEvent): void => {
    e.preventDefault(); // Prevent link navigation if button is inside Link
    e.stopPropagation(); // Prevent link navigation
    navigate(`/admin/vehicles/edit/${vehicle.id}`);
  };

  return (
    <Link
      to={`/vehicles/${vehicle.id}`}
      style={{
        textDecoration: 'none',
        color: 'inherit',
        display: 'block',
        width: '100%',
        height: '100%', // Ensure Link takes full height for Card
      }}
    >
      <Card
        sx={{
          transition: 'transform 0.2s ease-in-out, box-shadow 0.2s ease-in-out',
          '&:hover, &:focus': {
            transform: 'translateY(-4px)',
            boxShadow: (theme) => theme.shadows[6],
          },
          border: '1px solid #eee',
          borderRadius: '8px',
          overflow: 'hidden',
          height: '100%', // Ensure Card takes full height
          display: 'flex',
          flexDirection: 'column',
          position: 'relative', // For admin button positioning
        }}
      >
        {user && user.role === 'Admin' && (
          <Button
            size="small"
            variant="contained"
            color="secondary"
            startIcon={<EditIcon />}
            onClick={handleEdit}
            sx={{
              position: 'absolute',
              top: 8,
              right: 8,
              zIndex: 1, // Ensure it's above other elements
            }}
          >
            Edit
          </Button>
        )}
        <div style={{ height: '200px', overflow: 'hidden' }}>
          <img
            src={getImageUrl()}
            alt={`${vehicle.make} ${vehicle.model}`}
            style={{
              width: '100%',
              height: '100%',
              objectFit: 'cover',
            }}
            onError={(e) => {
              e.currentTarget.style.display = 'none'; // optionally hide if broken
            }}
          />
        </div>

        <div
          style={{
            padding: '16px',
            flexGrow: 1,
            display: 'flex',
            flexDirection: 'column',
            justifyContent: 'space-between',
          }}
        >
          <div>
            <h3
              style={{ margin: '0 0 8px', fontSize: '1.2rem', fontWeight: 600 }}
            >
              {vehicle.year} {vehicle.make} {vehicle.model}
            </h3>

            <p
              style={{
                margin: '0 0 8px',
                fontWeight: 'bold',
                fontSize: '1.1rem',
                color: '#1976d2',
              }}
            >
              €{vehicle.price.toLocaleString()}
            </p>

            <div
              style={{
                display: 'flex',
                justifyContent: 'space-between',
                margin: '8px 0',
                color: '#666',
                fontSize: '0.9rem',
              }}
            >
              <span>{vehicle.mileage.toLocaleString()} km</span>
              {vehicle.fuelType && (
                <span>{getFuelTypeName(vehicle.fuelType)}</span>
              )}
            </div>
          </div>
          <div style={{ marginTop: 'auto', paddingTop: '12px' }}>
            <div
              style={{
                display: 'inline-block',
                backgroundColor: '#1976d2',
                color: 'white',
                padding: '8px 16px',
                borderRadius: '4px',
                textAlign: 'center',
                width: '100%',
                fontWeight: 500,
              }}
            >
              View Details
            </div>
          </div>
        </div>
      </Card>
    </Link>
  );
};

export default VehicleCard;
