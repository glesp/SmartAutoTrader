/**
 * @file App.tsx
 * @summary The main application component that sets up routing, theming, and global context providers.
 *
 * @description This file defines the root `App` component for the Smart Auto Trader frontend.
 * It initializes the `ThemeProvider` with a custom theme, wraps the application in an `AuthProvider`
 * for managing authentication state, and sets up client-side routing using `BrowserRouter` and `Routes`
 * from `react-router-dom`. It defines all the primary routes for the application, including public pages,
 * authenticated user pages, and admin-specific pages protected by `ProtectedRoute`.
 *
 * @remarks
 * - **Theming**: Uses Material-UI's `ThemeProvider` to apply a consistent custom theme across the application.
 * - **Authentication**: Leverages `AuthProvider` to make authentication state and functions available globally.
 * - **Routing**: Implements a standard React Router setup with `BrowserRouter`, `Routes`, and `Route` components.
 *   Admin routes are protected using the `ProtectedRoute` component, which checks for appropriate user roles.
 * - **Layout**: A basic flexbox layout is used to ensure the `Footer` stays at the bottom of the viewport,
 *   even on pages with little content. The main content area uses a Material-UI `Container`.
 * - **Global Styles**: `CssBaseline` from Material-UI is used to apply baseline CSS normalizations.
 *
 * @dependencies
 * - react-router-dom: `BrowserRouter`, `Routes`, `Route` for client-side routing.
 * - @mui/material: `Container`, `CssBaseline`, `ThemeProvider` for UI structure and theming.
 * - ./pages/HomePage: Component for the home page.
 * - ./pages/VehicleListingPage: Component for displaying a list of vehicles.
 * - ./pages/VehicleDetailPage: Component for displaying details of a single vehicle.
 * - ./pages/LoginPage: Component for user login.
 * - ./pages/RegisterPage: Component for user registration.
 * - ./pages/ProfilePage: Component for user profile management.
 * - ./components/layout/Header: The main application header component.
 * - ./components/layout/Footer: The main application footer component.
 * - ./contexts/AuthContext: `AuthProvider` for managing authentication state. (Note: actual import is `./contexts`)
 * - ./pages/RecommendationsPage: Component for displaying vehicle recommendations.
 * - ./theme: Custom Material-UI theme configuration.
 * - ./pages/NewInquiryPage: Component for creating a new vehicle inquiry.
 * - ./components/ProtectedRoute: Component for protecting routes based on authentication and roles.
 * - ./pages/AdminDashboardPage: Component for the admin dashboard.
 * - ./pages/AdminInquiriesPage: Component for admin management of inquiries.
 * - ./pages/AdminCreateVehiclePage: Component for admins to create new vehicle listings.
 * - ./pages/AdminEditVehiclePage: Component for admins to edit existing vehicle listings.
 */
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

/**
 * @function App
 * @summary The root component of the Smart Auto Trader application.
 * @description This component sets up the overall application structure, including theme provisioning,
 * authentication context, routing, and a basic layout with a header, main content area, and footer.
 * It defines all the routes for navigating through different pages of the application.
 * @returns {JSX.Element} The rendered application structure.
 * @remarks
 * The `App` component uses `ThemeProvider` to apply the custom Material-UI theme, `AuthProvider`
 * to manage global authentication state, and `BrowserRouter` to enable client-side navigation.
 * Routes are defined using the `Routes` and `Route` components from `react-router-dom`.
 * `CssBaseline` is included for consistent styling across browsers.
 * The main content area is wrapped in a Material-UI `Container` and styled to ensure
 * the footer remains at the bottom of the page.
 * @example
 * // Typically rendered by ReactDOM.render in main.tsx or index.tsx
 * ReactDOM.createRoot(document.getElementById('root')!).render(
 *   <React.StrictMode>
 *     <App />
 *   </React.StrictMode>
 * );
 */
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
