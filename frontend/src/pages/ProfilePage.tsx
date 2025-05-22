/**
 * @file ProfilePage.tsx
 * @summary Provides the `ProfilePage` component, which displays the user's profile, including their favorite vehicles and inquiries.
 *
 * @description The `ProfilePage` component allows authenticated users to view and manage their profile data. It includes two main sections:
 * a list of favorite vehicles and a list of inquiries sent by the user. The component fetches data from the backend API and displays it in a
 * tabbed interface. Users can mark inquiries as closed and navigate to other parts of the application.
 *
 * @remarks
 * - The component uses Material-UI for layout and styling, including components such as `Container`, `Paper`, `Tabs`, and `Grid`.
 * - React Router is used for navigation, enabling redirection for unauthenticated users and navigation to other pages.
 * - The `AuthContext` is used to check the user's authentication status and retrieve user details.
 * - The `favoriteService` and `inquiryService` are used to fetch and manage user data from the backend API.
 * - Error handling is implemented to gracefully handle API failures and display fallback content.
 *
 * @dependencies
 * - React: `useState`, `useEffect`, `useContext` for managing state and accessing the authentication context.
 * - Material-UI: Components for layout, styling, and tabs.
 * - React Router: `Link`, `Navigate` for navigation and redirection.
 * - `AuthContext`: For managing user authentication and access control.
 * - `favoriteService`: For fetching the user's favorite vehicles.
 * - `inquiryService`: For fetching and managing the user's inquiries.
 * - `VehicleCard`: A reusable component for displaying individual vehicle details.
 *
 * @example
 * <ProfilePage />
 */

import { useState, useEffect, useContext, JSX } from 'react';
import { Link, Navigate } from 'react-router-dom';
import { AuthContext } from '../contexts/AuthContext';
import { favoriteService, inquiryService } from '../services/api';
import VehicleCard from '../components/vehicles/VehicleCard';
import {
  Box,
  Typography,
  Button,
  Grid,
  Paper,
  Tabs,
  Tab,
  Container,
  Chip,
} from '@mui/material';
import { Vehicle, ReferenceWrapper } from '../types/models';

/**
 * @interface Inquiry
 * @summary Represents a user inquiry about a vehicle.
 *
 * @property {number} id - The unique identifier for the inquiry.
 * @property {number} vehicleId - The ID of the vehicle associated with the inquiry.
 * @property {string} subject - The subject of the inquiry.
 * @property {string} message - The message content of the inquiry.
 * @property {string} [response] - The administrator's response to the inquiry.
 * @property {string} dateSent - The date the inquiry was sent.
 * @property {string} [dateReplied] - The date the inquiry was replied to.
 * @property {string} status - The current status of the inquiry (e.g., "New", "Read", "Replied", "Closed").
 * @property {Vehicle} [vehicle] - The vehicle associated with the inquiry.
 */
interface Inquiry {
  id: number;
  vehicleId: number;
  subject: string;
  message: string;
  response?: string;
  dateSent: string;
  dateReplied?: string;
  status: string;
  vehicle?: Vehicle;
}

/**
 * @typedef SerializedData
 * @summary Represents data that may be serialized in ASP.NET reference format.
 *
 * @template T - The type of the data.
 */
type SerializedData<T> = T[] | ReferenceWrapper<T> | undefined | null;

/**
 * @function extractArray
 * @summary Extracts an array from ASP.NET reference-wrapped data.
 *
 * @template T - The type of the data.
 * @param {SerializedData<T>} data - The serialized data to extract.
 * @returns {T[]} The extracted array.
 *
 * @example
 * const vehicles = extractArray<Vehicle>(serializedVehicles);
 */
const extractArray = <T,>(data: SerializedData<T>): T[] => {
  if (!data) return [];

  if (Array.isArray(data)) {
    return data;
  } else if (typeof data === 'object' && data !== null && '$values' in data) {
    return (data as ReferenceWrapper<T>).$values;
  }

  return [];
};

/**
 * @function ProfilePage
 * @summary Renders the user's profile page, including their favorite vehicles and inquiries.
 *
 * @returns {JSX.Element} The rendered profile page component.
 *
 * @remarks
 * - The component includes a tabbed interface for switching between favorite vehicles and inquiries.
 * - It fetches data from the backend API based on the active tab and displays it in a responsive grid layout.
 * - Users can mark inquiries as closed and navigate to other parts of the application.
 * - If the user is not authenticated, they are redirected to the login page.
 *
 * @example
 * <ProfilePage />
 */
const ProfilePage = (): JSX.Element => {
  const {
    user,
    isAuthenticated,
    loading: authLoading,
  } = useContext(AuthContext);
  const [favoriteVehicles, setFavoriteVehicles] = useState<
    SerializedData<Vehicle>
  >([]);
  const [inquiries, setInquiries] = useState<SerializedData<Inquiry>>([]);
  const [activeTab, setActiveTab] = useState('favorites');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    /**
     * @function fetchUserData
     * @summary Fetches the user's data based on the active tab.
     *
     * @throws Will log an error if the API request fails.
     */
    const fetchUserData = async () => {
      if (!isAuthenticated) return;

      setLoading(true);

      try {
        if (activeTab === 'favorites') {
          const favorites = await favoriteService.getFavorites();
          setFavoriteVehicles(favorites);
        } else if (activeTab === 'inquiries') {
          const userInquiries = await inquiryService.getInquiries();
          setInquiries(userInquiries);
        }
      } catch (error) {
        console.error(`Error fetching ${activeTab}:`, error);
      } finally {
        setLoading(false);
      }
    };

    fetchUserData();
  }, [isAuthenticated, activeTab]);

  if (!authLoading && !isAuthenticated) {
    return <Navigate to="/login" state={{ from: '/profile' }} />;
  }

  if (authLoading) {
    return <div className="text-center py-12">Loading profile...</div>;
  }

  const favoritesArray = extractArray<Vehicle>(favoriteVehicles);
  const inquiriesArray = extractArray<Inquiry>(inquiries);

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      <Paper elevation={2} sx={{ overflow: 'hidden', borderRadius: 2 }}>
        {/* Profile header */}
        <Box sx={{ bgcolor: 'primary.main', px: 3, py: 4, color: 'white' }}>
          <Typography variant="h4" fontWeight="bold" mb={1}>
            My Profile
          </Typography>
          <Typography variant="body1">
            {user?.firstName} {user?.lastName} ({user?.username})
          </Typography>
          <Typography variant="body2" sx={{ color: 'primary.light' }}>
            {user?.email}
          </Typography>
        </Box>

        {/* Material-UI Tabs */}
        <Tabs
          value={activeTab}
          onChange={(_e, newValue) => setActiveTab(newValue)}
          sx={{ px: 2, borderBottom: 1, borderColor: 'divider' }}
        >
          <Tab
            label="My Favorites"
            value="favorites"
            sx={{ fontWeight: 500 }}
          />
          <Tab
            label="My Inquiries"
            value="inquiries"
            sx={{ fontWeight: 500 }}
          />
        </Tabs>

        {/* Tab content */}
        <Box sx={{ p: 3 }}>
          {activeTab === 'favorites' && (
            <Grid container spacing={3}>
              {loading ? (
                <Grid item xs={12}>
                  <Box display="flex" justifyContent="center" py={4}>
                    <Typography>Loading your favorites...</Typography>
                  </Box>
                </Grid>
              ) : favoritesArray.length === 0 ? (
                <Grid item xs={12}>
                  <Box textAlign="center" py={4}>
                    <Typography color="text.secondary" mb={2}>
                      You haven't added any vehicles to your favorites yet.
                    </Typography>
                    <Button variant="contained" component={Link} to="/vehicles">
                      Browse Vehicles
                    </Button>
                  </Box>
                </Grid>
              ) : (
                favoritesArray.map((vehicle: Vehicle) => (
                  <Grid item xs={12} sm={6} md={4} lg={3} key={vehicle.id}>
                    <VehicleCard vehicle={vehicle} />
                  </Grid>
                ))
              )}
            </Grid>
          )}

          {activeTab === 'inquiries' && (
            <Grid container spacing={3}>
              {loading ? (
                <Grid item xs={12}>
                  <Box display="flex" justifyContent="center" py={4}>
                    <Typography>Loading your inquiries...</Typography>
                  </Box>
                </Grid>
              ) : inquiriesArray.length === 0 ? (
                <Grid item xs={12}>
                  <Box textAlign="center" py={4}>
                    <Typography color="text.secondary" mb={2}>
                      You haven't sent any inquiries yet.
                    </Typography>
                    <Button variant="contained" component={Link} to="/vehicles">
                      Browse Vehicles
                    </Button>
                  </Box>
                </Grid>
              ) : (
                <Grid item xs={12}>
                  <Box
                    sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}
                  >
                    {inquiriesArray.map((inquiry: Inquiry) => (
                      <Paper
                        key={inquiry.id}
                        elevation={1}
                        sx={{ overflow: 'hidden', borderRadius: 2 }}
                      >
                        <Box
                          sx={{
                            bgcolor: 'grey.50',
                            px: 2,
                            py: 1.5,
                            borderBottom: '1px solid',
                            borderColor: 'divider',
                          }}
                        >
                          <Box
                            sx={{
                              display: 'flex',
                              justifyContent: 'space-between',
                              alignItems: 'center',
                            }}
                          >
                            <Typography variant="subtitle1" fontWeight="medium">
                              {inquiry.subject}
                            </Typography>
                            <Chip
                              label={inquiry.status}
                              size="small"
                              color={
                                inquiry.status === 'New'
                                  ? 'primary'
                                  : inquiry.status === 'Read'
                                    ? 'warning'
                                    : inquiry.status === 'Replied'
                                      ? 'success'
                                      : 'default'
                              }
                              variant="outlined"
                            />
                          </Box>
                          <Typography variant="caption" color="text.secondary">
                            {new Date(inquiry.dateSent).toLocaleDateString()} •
                            {inquiry.vehicle &&
                              ` regarding ${inquiry.vehicle.year} ${inquiry.vehicle.make} ${inquiry.vehicle.model}`}
                          </Typography>
                        </Box>

                        <Box sx={{ p: 2 }}>
                          <Box mb={2}>
                            <Typography
                              variant="caption"
                              color="text.secondary"
                              mb={0.5}
                              display="block"
                            >
                              Your message:
                            </Typography>
                            <Typography variant="body2">
                              {inquiry.message}
                            </Typography>
                          </Box>

                          {inquiry.response && (
                            <Box
                              sx={{
                                bgcolor: 'primary.50',
                                p: 2,
                                borderRadius: 1,
                              }}
                            >
                              <Typography
                                variant="caption"
                                color="primary.dark"
                                mb={0.5}
                                display="block"
                                fontWeight="medium"
                              >
                                Response:
                              </Typography>
                              <Typography variant="body2">
                                {inquiry.response}
                              </Typography>
                              <Typography
                                variant="caption"
                                color="text.secondary"
                                mt={1}
                                display="block"
                              >
                                Replied on{' '}
                                {inquiry.dateReplied &&
                                  new Date(
                                    inquiry.dateReplied
                                  ).toLocaleDateString()}
                              </Typography>
                            </Box>
                          )}

                          {inquiry.status !== 'Closed' && (
                            <Box sx={{ mt: 2, textAlign: 'right' }}>
                              <Button
                                variant="text"
                                size="small"
                                color="inherit"
                                sx={{ color: 'text.secondary' }}
                                onClick={async () => {
                                  try {
                                    await inquiryService.closeInquiry(
                                      inquiry.id
                                    );
                                    setInquiries(
                                      (prev: SerializedData<Inquiry>) => {
                                        const prevArray =
                                          extractArray<Inquiry>(prev);
                                        return prevArray.map((i) =>
                                          i.id === inquiry.id
                                            ? { ...i, status: 'Closed' }
                                            : i
                                        );
                                      }
                                    );
                                  } catch (error) {
                                    console.error(
                                      'Error closing inquiry:',
                                      error
                                    );
                                  }
                                }}
                              >
                                Mark as Closed
                              </Button>
                            </Box>
                          )}
                        </Box>
                      </Paper>
                    ))}
                  </Box>
                </Grid>
              )}
            </Grid>
          )}
        </Box>
      </Paper>
    </Container>
  );
};

export default ProfilePage;
