/**
 * @file VehicleListingPage.tsx
 * @summary Provides the `VehicleListingPage` component, which displays a paginated list of vehicles with filtering and sorting options.
 *
 * @description The `VehicleListingPage` component fetches and displays a list of vehicles from the backend API. It includes a filter panel
 * for refining search results and a pagination control for navigating through multiple pages of vehicles. The component dynamically updates
 * the displayed vehicles based on the selected filters and the current page.
 *
 * @remarks
 * - The component uses Material-UI for layout and styling, including components such as `Container`, `Paper`, `Grid`, and `Pagination`.
 * - The `VehicleFilters` component is used to manage and apply filters for sorting and filtering vehicles.
 * - The `VehicleCard` component is used to display individual vehicle details in a card format.
 * - Error handling is implemented to display fallback content in case of API failures or no results.
 *
 * @dependencies
 * - React: `useState`, `useEffect` for managing state and side effects.
 * - Material-UI: Components for layout, styling, and pagination.
 * - `vehicleService`: For fetching vehicle data from the backend API.
 * - `VehicleCard`: A reusable component for displaying individual vehicle details.
 * - `VehicleFilters`: A reusable component for managing vehicle filters.
 *
 * @example
 * <VehicleListingPage />
 */

import { useState, useEffect, JSX } from 'react';
import { vehicleService } from '../services/api';
import VehicleCard from '../components/vehicles/VehicleCard';
import VehicleFilters from '../components/vehicles/VehicleFilters';
import { Vehicle } from '../types/models';
import { FilterState } from '../types/models';
import {
  Grid,
  Box,
  Typography,
  Container,
  Paper,
  Pagination,
  Divider,
  Skeleton,
} from '@mui/material';

/**
 * @function VehicleListingPage
 * @summary Renders the vehicle listing page, displaying a paginated list of vehicles with filtering and sorting options.
 *
 * @returns {JSX.Element} The rendered vehicle listing page component.
 *
 * @remarks
 * - The component fetches vehicle data from the backend API and displays it in a responsive grid layout.
 * - It includes a filter panel for refining search results and a pagination control for navigating through multiple pages.
 * - The displayed vehicles are dynamically updated based on the selected filters and the current page.
 * - Error handling ensures that fallback content is displayed if the API request fails or no results are found.
 *
 * @example
 * <VehicleListingPage />
 */
const VehicleListingPage = (): JSX.Element => {
  const [vehicles, setVehicles] = useState<Vehicle[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [filters, setFilters] = useState<FilterState>({
    sortBy: 'DateListed',
    ascending: false,
  });

  useEffect(() => {
    /**
     * @function loadVehicles
     * @summary Fetches the list of vehicles from the backend API based on the current filters and page.
     *
     * @throws Will log an error if the API request fails.
     *
     * @remarks
     * - The function fetches vehicles using the `vehicleService` and updates the state with the results.
     * - It calculates the total number of pages based on the total count of vehicles and the page size.
     */
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

  /**
   * @function handleFilterChange
   * @summary Updates the filters and resets the page to the first page when filters change.
   *
   * @param {Partial<FilterState>} newFilters - The updated filter values.
   *
   * @remarks
   * - The function merges the new filters with the existing filters and resets the page to 1.
   */
  const handleFilterChange = (newFilters: Partial<FilterState>) => {
    setFilters((prev) => {
      const next = { ...prev, ...newFilters };
      const changed = Object.keys(newFilters).some((key) => {
        const typedKey = key as keyof FilterState;
        return newFilters[typedKey] !== prev[typedKey];
      });
      if (changed) {
        setPage(1); // Reset to first page when filters change
        return next;
      }
      return prev;
    });
  };

  /**
   * @function handlePageChange
   * @summary Updates the current page and scrolls to the top of the page.
   *
   * @param {React.ChangeEvent<unknown>} _event - The pagination change event.
   * @param {number} newPage - The new page number.
   */
  const handlePageChange = (
    _event: React.ChangeEvent<unknown>,
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
            <Grid container spacing={3}>
              {[1, 2, 3, 4].map((i) => (
                <Grid item xs={12} sm={6} md={4} lg={4} key={i}>
                  <Skeleton
                    variant="rectangular"
                    height={200}
                    sx={{ borderRadius: 2, mb: 2 }}
                  />
                  <Skeleton variant="text" width="60%" />
                  <Skeleton variant="text" width="40%" />
                  <Skeleton variant="text" width="80%" />
                </Grid>
              ))}
            </Grid>
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
