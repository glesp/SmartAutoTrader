// src/pages/ProfilePage.tsx
import { useState, useEffect, useContext } from 'react'
import { Link, Navigate } from 'react-router-dom'
import { AuthContext } from '../contexts/AuthContext'
import { favoriteService, inquiryService } from '../services/api'
import VehicleCard from '../components/vehicles/VehicleCard'
// Add Material UI imports
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
} from '@mui/material'

// Define VehicleImage interface
interface VehicleImage {
  id: number
  imageUrl: string
  isPrimary: boolean
}

// Define ReferenceWrapper for ASP.NET serialization
interface ReferenceWrapper<T> {
  $id?: string
  $values: T[]
}

// Define Vehicle interface
interface Vehicle {
  id: number
  make: string
  model: string
  year: number
  price: number
  mileage: number
  images: VehicleImage[] | ReferenceWrapper<VehicleImage> | undefined
}

// Define Inquiry interface
interface Inquiry {
  id: number
  vehicleId: number
  subject: string
  message: string
  response?: string
  dateSent: string
  dateReplied?: string
  status: string
  vehicle?: Vehicle
}

// Define what the arrays might look like with ASP.NET serialization
type SerializedData<T> = T[] | ReferenceWrapper<T> | undefined | null

// Helper function to extract arrays from ASP.NET reference format
const extractArray = <T,>(data: SerializedData<T>): T[] => {
  if (!data) return []

  if (Array.isArray(data)) {
    return data
  } else if (typeof data === 'object' && data !== null && '$values' in data) {
    return (data as ReferenceWrapper<T>).$values
  }

  return []
}

const ProfilePage = () => {
  const {
    user,
    isAuthenticated,
    loading: authLoading,
  } = useContext(AuthContext)
  const [favoriteVehicles, setFavoriteVehicles] = useState<
    SerializedData<Vehicle>
  >([])
  const [inquiries, setInquiries] = useState<SerializedData<Inquiry>>([])
  const [activeTab, setActiveTab] = useState('favorites')
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const fetchUserData = async () => {
      if (!isAuthenticated) return

      setLoading(true)

      try {
        if (activeTab === 'favorites') {
          const favorites = await favoriteService.getFavorites()
          setFavoriteVehicles(favorites)
        } else if (activeTab === 'inquiries') {
          const userInquiries = await inquiryService.getInquiries()
          setInquiries(userInquiries)
        }
      } catch (error) {
        console.error(`Error fetching ${activeTab}:`, error)
      } finally {
        setLoading(false)
      }
    }

    fetchUserData()
  }, [isAuthenticated, activeTab])

  // Redirect if not authenticated
  if (!authLoading && !isAuthenticated) {
    return <Navigate to="/login" state={{ from: '/profile' }} />
  }

  if (authLoading) {
    return <div className="text-center py-12">Loading profile...</div>
  }

  // Extract arrays from potentially reference-wrapped data
  const favoritesArray = extractArray<Vehicle>(favoriteVehicles)
  const inquiriesArray = extractArray<Inquiry>(inquiries)

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
          onChange={(e, newValue) => setActiveTab(newValue)}
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
          {/* Favorites tab */}
          {activeTab === 'favorites' && (
            <>
              {loading ? (
                <Box textAlign="center" py={4}>
                  <Typography>Loading your favorites...</Typography>
                </Box>
              ) : favoritesArray.length === 0 ? (
                <Box textAlign="center" py={4}>
                  <Typography color="text.secondary" mb={2}>
                    You haven't added any vehicles to your favorites yet.
                  </Typography>
                  <Button variant="contained" component={Link} to="/vehicles">
                    Browse Vehicles
                  </Button>
                </Box>
              ) : (
                <Grid container spacing={3}>
                  {favoritesArray.map((vehicle: Vehicle) => (
                    <Grid item xs={12} sm={6} md={4} lg={3} key={vehicle.id}>
                      <VehicleCard vehicle={vehicle} />
                    </Grid>
                  ))}
                </Grid>
              )}
            </>
          )}

          {/* Inquiries tab */}
          {activeTab === 'inquiries' && (
            <>
              {loading ? (
                <Box textAlign="center" py={4}>
                  <Typography>Loading your inquiries...</Typography>
                </Box>
              ) : inquiriesArray.length === 0 ? (
                <Box textAlign="center" py={4}>
                  <Typography color="text.secondary" mb={2}>
                    You haven't sent any inquiries yet.
                  </Typography>
                  <Button variant="contained" component={Link} to="/vehicles">
                    Browse Vehicles
                  </Button>
                </Box>
              ) : (
                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
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
                                  await inquiryService.closeInquiry(inquiry.id)
                                  setInquiries(
                                    (prev: SerializedData<Inquiry>) => {
                                      const prevArray =
                                        extractArray<Inquiry>(prev)
                                      return prevArray.map((i) =>
                                        i.id === inquiry.id
                                          ? { ...i, status: 'Closed' }
                                          : i
                                      )
                                    }
                                  )
                                } catch (error) {
                                  console.error('Error closing inquiry:', error)
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
              )}
            </>
          )}
        </Box>
      </Paper>
    </Container>
  )
}

export default ProfilePage
