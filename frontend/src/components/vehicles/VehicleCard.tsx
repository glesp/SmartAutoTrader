import React, { useContext } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { VehicleProps } from '../../types/models';
import Card from '@mui/material/Card';
import Button from '@mui/material/Button';
import EditIcon from '@mui/icons-material/Edit';
import { AuthContext } from '../../contexts/AuthContext';

// Define image type to avoid type errors
interface VehicleImage {
  imageUrl: string;
  isPrimary: boolean;
}

// Helper function to extract arrays from ASP.NET response format
const extractArray = <T,>(
  data: T[] | { $values: T[] } | undefined | Record<string, unknown>
): T[] => {
  if (!data) return [];
  if (Array.isArray(data)) return data;
  if (data && '$values' in data && Array.isArray(data.$values))
    return data.$values as T[];
  return [];
};

// Map fuel type numbers to strings
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

const VehicleCard: React.FC<VehicleProps> = ({ vehicle }) => {
  const { user } = useContext(AuthContext);
  const navigate = useNavigate();

  // Get primary image URL
  const getImageUrl = () => {
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

  const handleEdit = (e: React.MouseEvent) => {
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
