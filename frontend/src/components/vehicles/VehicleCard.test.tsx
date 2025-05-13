// frontend/src/components/vehicles/VehicleCard.test.tsx
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import VehicleCard from './VehicleCard';
import { Vehicle, VehicleImage, ReferenceWrapper } from '../../types/models';
import { vi } from 'vitest';
import '@testing-library/jest-dom';

vi.mock('../../services/api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../services/api')>();
  return {
    ...actual,
    default: 'http://mocked.api.base/api',
  };
});

describe('VehicleCard Component', () => {
  const mockVehicle: Vehicle = {
    id: 101,
    make: 'Honda',
    model: 'Civic',
    year: 2022,
    price: 22000,
    mileage: 12000,
    fuelType: 'Petrol',
    vehicleType: 'Hatchback',
    transmission: 'Manual',
    description: 'A sporty hatchback.',
    images: [
      {
        id: 1,
        imageUrl: 'http://mocked.api.base/api/civic.jpg',
        isPrimary: true,
      },
    ] as VehicleImage[] | ReferenceWrapper<VehicleImage>,
  };

  const renderVehicleCardInRouter = (vehicle: Vehicle) => {
    return render(
      <MemoryRouter>
        <VehicleCard vehicle={vehicle} />
      </MemoryRouter>
    );
  };

  test('renders essential vehicle details', () => {
    renderVehicleCardInRouter(mockVehicle);
    expect(
      screen.getByText(
        `${mockVehicle.year} ${mockVehicle.make} ${mockVehicle.model}`
      )
    ).toBeInTheDocument();
    expect(
      screen.getByText(`€${mockVehicle.price.toLocaleString()}`)
    ).toBeInTheDocument();
    expect(
      screen.getByText(`${mockVehicle.mileage.toLocaleString()} km`)
    ).toBeInTheDocument();
    expect(
      screen.getByText(mockVehicle.fuelType.toString())
    ).toBeInTheDocument();
  });

  test('renders primary image with correct alt text and src', () => {
    renderVehicleCardInRouter(mockVehicle);
    const image = screen.getByRole('img', {
      name: `${mockVehicle.make} ${mockVehicle.model}`,
    });
    expect(image).toBeInTheDocument();
    // Corrected: Removed the extra '/images/' part
    expect(image).toHaveAttribute(
      'src',
      'http://mocked.api.base/api/civic.jpg'
    );
  });

  test('handles missing primary image gracefully', () => {
    const vehicleNoPrimary: Vehicle = {
      ...mockVehicle,
      images: [
        {
          id: 2,
          imageUrl: 'http://mocked.api.base/api/other-civic.jpg',
          isPrimary: false,
        },
      ],
    };
    renderVehicleCardInRouter(vehicleNoPrimary);
    const image = screen.getByRole('img', {
      name: `${mockVehicle.make} ${mockVehicle.model}`,
    });
    expect(image).toHaveAttribute(
      'src',
      'http://mocked.api.base/api/other-civic.jpg'
    );
  });

  test('handles empty images array gracefully', () => {
    const vehicleNoImages: Vehicle = { ...mockVehicle, images: [] };
    renderVehicleCardInRouter(vehicleNoImages);
    // Update this assertion based on your component's actual fallback behavior
    // e.g. expect(screen.getByTestId('no-image-placeholder')).toBeInTheDocument();
  });

  test('"View Details" link navigates to the correct vehicle page', () => {
    renderVehicleCardInRouter(mockVehicle);
    const detailsLink = screen.getByRole('link', { name: /view details/i });
    expect(detailsLink).toBeInTheDocument();
    expect(detailsLink).toHaveAttribute('href', `/vehicles/${mockVehicle.id}`);
  });
});
