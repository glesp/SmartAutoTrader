import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { Container, CssBaseline, ThemeProvider } from '@mui/material';
import HomePage from './pages/HomePage';
import VehicleListingPage from './pages/VehicleListingPage';
import VehicleDetailPage from './pages/VehicleDetailPage';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import ProfilePage from './pages/ProfilePage';
import Header from './components/layout/Header';
import Footer from './components/layout/Footer';
import { AuthProvider } from './contexts';
import RecommendationsPage from './pages/RecommendationsPage';
import theme from './theme'; // Import the custom theme
import NewInquiryPage from './pages/NewInquiryPage'; // Import the new page
import ProtectedRoute from './components/ProtectedRoute'; // Import ProtectedRoute
import AdminDashboardPage from './pages/AdminDashboardPage'; // Import the new dashboard page
import AdminInquiriesPage from './pages/AdminInquiriesPage'; // Import AdminInquiriesPage
import AdminCreateVehiclePage from './pages/AdminCreateVehiclePage'; // Import the new page
import AdminEditVehiclePage from './pages/AdminEditVehiclePage'; // Import the AdminEditVehiclePage

function App() {
  return (
    <ThemeProvider theme={theme}>
      <AuthProvider>
        <BrowserRouter>
          <CssBaseline />
          <div
            style={{
              display: 'flex',
              flexDirection: 'column',
              minHeight: '100vh',
            }}
          >
            <Header />
            <Container
              component="main"
              maxWidth="xl" // Or your preferred max width
              sx={{
                flexGrow: 1,
                py: { xs: 2, sm: 3, md: 4 }, // Responsive padding
                // Adjust mt based on your Header's height to prevent overlap
                mt: { xs: '56px', sm: '64px', md: '64px' }, // Example values
              }}
            >
              <Routes>
                <Route path="/" element={<HomePage />} />
                <Route path="/vehicles" element={<VehicleListingPage />} />
                <Route path="/vehicles/:id" element={<VehicleDetailPage />} />
                <Route path="/login" element={<LoginPage />} />
                <Route path="/register" element={<RegisterPage />} />
                <Route path="/profile" element={<ProfilePage />} />
                <Route
                  path="/recommendations"
                  element={<RecommendationsPage />}
                />
                <Route
                  path="/recommendations/:userId"
                  element={<RecommendationsPage />}
                />
                <Route path="/inquiries/new" element={<NewInquiryPage />} />
                <Route
                  path="/admin/dashboard"
                  element={
                    <ProtectedRoute roles={['Admin']}>
                      <AdminDashboardPage />
                    </ProtectedRoute>
                  }
                />
                <Route
                  path="/admin/inquiries"
                  element={
                    <ProtectedRoute roles={['Admin'] as string[]}>
                      <AdminInquiriesPage />
                    </ProtectedRoute>
                  }
                />
                <Route
                  path="/admin/vehicles/create"
                  element={
                    <ProtectedRoute roles={['Admin']}>
                      <AdminCreateVehiclePage />
                    </ProtectedRoute>
                  }
                />
                <Route
                  path="/admin/vehicles/edit/:vehicleId"
                  element={
                    <ProtectedRoute roles={['Admin']}>
                      <AdminEditVehiclePage />
                    </ProtectedRoute>
                  }
                />
              </Routes>
            </Container>
            <Footer />
          </div>
        </BrowserRouter>
      </AuthProvider>
    </ThemeProvider>
  );
}

export default App;
