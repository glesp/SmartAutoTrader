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
import AdminInquiriesPage from './pages/AdminInquiriesPage'; // Import AdminInquiriesPage

function App() {
  return (
    <ThemeProvider theme={theme}>
      <CssBaseline /> {/* This normalizes CSS across browsers */}
      <BrowserRouter>
        <AuthProvider>
          <div className="app">
            <Header />
            <Container
              component="main"
              maxWidth={false}
              disableGutters
              sx={{ py: 4, px: { xs: 2, md: 4 } }}
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
                  path="/admin/inquiries"
                  element={
                    <ProtectedRoute roles={['Admin'] as string[]}>
                      <AdminInquiriesPage />
                    </ProtectedRoute>
                  }
                />
              </Routes>
            </Container>
            <Footer />
          </div>
        </AuthProvider>
      </BrowserRouter>
    </ThemeProvider>
  );
}

export default App;
