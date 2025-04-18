// src/pages/VehicleListingPage.tsx
import { useState, useEffect } from 'react';
import { vehicleService } from '../services/api';
import VehicleCard from '../components/vehicles/VehicleCard';
import VehicleFilters from '../components/vehicles/VehicleFilters';
import { Vehicle } from '../types/models';
import {
  Grid,
  Box,
  Typography,
  Container,
  Paper,
  Pagination,
  CircularProgress,
  Divider,
} from '@mui/material';

interface FilterState {
  make?: string;
  model?: string;
  minYear?: number;
  maxYear?: number;
  minPrice?: number;
  maxPrice?: number;
  fuelType?: string;
  transmission?: string;
  vehicleType?: string;
  sortBy: string;
  ascending: boolean;
}

const VehicleListingPage = () => {
  const [vehicles, setVehicles] = useState<Vehicle[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [filters, setFilters] = useState<FilterState>({
    sortBy: 'DateListed',
    ascending: false,
  });

  useEffect(() => {
    const loadVehicles = async () => {
      setLoading(true);
      try {
        const response = await vehicleService.getVehicles({
          ...filters,
          pageNumber: page,
          pageSize: 8,
        });

        setVehicles(response);

        // Axios response headers need to be accessed differently
        // If your API isn't returning headers correctly, you can adjust this:
        const totalCount = 20; // Default value or calculate from total vehicles
        const calculatedTotalPages = Math.ceil(totalCount / 8);
        setTotalPages(calculatedTotalPages || 1);
      } catch (error) {
        console.error('Error loading vehicles:', error);
      } finally {
        setLoading(false);
      }
    };

    loadVehicles();
  }, [filters, page]);

  const handleFilterChange = (newFilters: Partial<FilterState>) => {
    setFilters((prev) => ({ ...prev, ...newFilters }));
    setPage(1); // Reset to first page when filters change
  };

  const handlePageChange = (
    event: React.ChangeEvent<unknown>,
    newPage: number
  ) => {
    setPage(newPage);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      <Typography variant="h3" component="h1" fontWeight="bold" gutterBottom>
        Available Vehicles
      </Typography>
      <Divider sx={{ mb: 4 }} />

      {/* Main Grid container for layout */}
      <Grid container spacing={3}>
        {/* Filters section - 3 columns on md+ screens, full width on smaller screens */}
        <Grid item xs={12} md={3}>
          <Box
            component={Paper}
            elevation={1}
            sx={{
              p: 2,
              borderRadius: 2,
              position: { md: 'sticky' },
              top: { md: '24px' },
            }}
          >
            <VehicleFilters
              filters={filters}
              onFilterChange={handleFilterChange}
            />
          </Box>
        </Grid>

        {/* Vehicle results section - 9 columns on md+ screens, full width on smaller screens */}
        <Grid item xs={12} md={9}>
          {loading ? (
            <Box
              display="flex"
              justifyContent="center"
              alignItems="center"
              height={300}
            >
              <CircularProgress />
              <Typography sx={{ ml: 2 }}>Loading vehicles...</Typography>
            </Box>
          ) : vehicles.length === 0 ? (
            <Paper
              elevation={0}
              sx={{
                p: 4,
                textAlign: 'center',
                bgcolor: 'grey.50',
                borderRadius: 2,
              }}
            >
              <Typography variant="h6" fontWeight="medium" gutterBottom>
                No vehicles found
              </Typography>
              <Typography color="text.secondary">
                Try adjusting your filters to see more results.
              </Typography>
            </Paper>
          ) : (
            <>
              {/* Responsive grid for vehicle cards */}
              <Grid container spacing={3}>
                {vehicles.map((vehicle) => (
                  <Grid item xs={12} sm={6} md={4} lg={4} key={vehicle.id}>
                    <VehicleCard vehicle={vehicle} />
                  </Grid>
                ))}
              </Grid>

              {/* Pagination */}
              <Box display="flex" justifyContent="center" mt={6}>
                <Pagination
                  count={totalPages}
                  page={page}
                  onChange={handlePageChange}
                  color="primary"
                  size="large"
                  showFirstButton
                  showLastButton
                />
              </Box>
            </>
          )}
        </Grid>
      </Grid>
    </Container>
  );
};

export default VehicleListingPage;
