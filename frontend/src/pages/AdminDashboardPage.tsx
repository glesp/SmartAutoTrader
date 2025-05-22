/**
 * @file AdminDashboardPage.tsx
 * @summary Provides the `AdminDashboardPage` component, which serves as the main dashboard for administrators.
 *
 * @description The `AdminDashboardPage` component is a central hub for administrators, providing quick access to key management features such as vehicle creation,
 * viewing all vehicles, and managing user inquiries. It also includes placeholders for future features like site statistics and analytics. The component is styled
 * using Material-UI and is designed to be responsive and user-friendly.
 *
 * @remarks
 * - The component uses Material-UI for layout and styling, including components such as `Container`, `Paper`, `Grid`, and `Button`.
 * - React Router is used for navigation, enabling seamless routing to different admin pages.
 * - The dashboard is modular, with each section represented as a card for better organization and scalability.
 * - Placeholder sections are included for future enhancements, such as analytics and site statistics.
 *
 * @dependencies
 * - React: For creating the functional component.
 * - Material-UI: Components for layout, styling, and buttons.
 * - React Router: `RouterLink` for navigation between pages.
 * - Material-UI icons: Icons for visual representation of actions (e.g., `AddCircleOutlineIcon`, `ListAltIcon`, `EmailIcon`, `BarChartIcon`).
 *
 * @example
 * <AdminDashboardPage />
 */

import React from 'react';
import { Container, Typography, Paper, Grid, Button, Box } from '@mui/material';
import { Link as RouterLink } from 'react-router-dom';
import AddCircleOutlineIcon from '@mui/icons-material/AddCircleOutline';
import ListAltIcon from '@mui/icons-material/ListAlt';
import EmailIcon from '@mui/icons-material/Email';
import BarChartIcon from '@mui/icons-material/BarChart';

/**
 * @function AdminDashboardPage
 * @summary Renders the admin dashboard page, providing access to key administrative features.
 *
 * @returns {JSX.Element} The rendered admin dashboard page component.
 *
 * @remarks
 * - The dashboard includes cards for vehicle management, user inquiries, and placeholders for future features.
 * - Each card is styled with Material-UI and includes buttons for navigation to specific admin pages.
 * - The component is designed to be responsive, adapting to different screen sizes.
 *
 * @example
 * <AdminDashboardPage />
 */
const AdminDashboardPage: React.FC = () => {
  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      <Typography
        variant="h4"
        component="h1"
        gutterBottom
        sx={{ mb: 4, fontWeight: 'bold', textAlign: 'center' }}
      >
        Admin Dashboard
      </Typography>

      <Grid container spacing={3} justifyContent="center">
        {/* Card for Vehicle Management */}
        <Grid item xs={12} sm={8} md={5} lg={4}>
          <Paper
            elevation={3}
            sx={{
              p: 3,
              display: 'flex',
              flexDirection: 'column',
              height: '100%',
              borderRadius: 2,
            }}
          >
            <Typography
              variant="h6"
              gutterBottom
              sx={{ textAlign: 'center', mb: 2 }}
            >
              Vehicle Management
            </Typography>
            <Box
              sx={{
                flexGrow: 1,
                display: 'flex',
                flexDirection: 'column',
                justifyContent: 'center',
                gap: 2,
              }}
            >
              <Button
                variant="contained"
                color="primary"
                startIcon={<AddCircleOutlineIcon />}
                component={RouterLink}
                to="/admin/vehicles/create"
                fullWidth
                size="large"
              >
                Create New Vehicle
              </Button>
              <Button
                variant="outlined"
                color="primary"
                startIcon={<ListAltIcon />}
                component={RouterLink}
                to="/vehicles" // Links to the public vehicle listing page
                fullWidth
                size="large"
              >
                View All Vehicles
              </Button>
            </Box>
          </Paper>
        </Grid>

        {/* Card for Inquiries */}
        <Grid item xs={12} sm={8} md={5} lg={4}>
          <Paper
            elevation={3}
            sx={{
              p: 3,
              display: 'flex',
              flexDirection: 'column',
              height: '100%',
              borderRadius: 2,
            }}
          >
            <Typography
              variant="h6"
              gutterBottom
              sx={{ textAlign: 'center', mb: 2 }}
            >
              User Inquiries
            </Typography>
            <Box
              sx={{
                flexGrow: 1,
                display: 'flex',
                flexDirection: 'column',
                justifyContent: 'center',
              }}
            >
              <Button
                variant="contained"
                color="secondary"
                startIcon={<EmailIcon />}
                component={RouterLink}
                to="/admin/inquiries"
                fullWidth
                size="large"
              >
                Manage Inquiries
              </Button>
            </Box>
          </Paper>
        </Grid>

        {/* Placeholder for future sections */}
        <Grid item xs={12} sm={8} md={5} lg={4}>
          <Paper
            elevation={3}
            sx={{
              p: 3,
              display: 'flex',
              flexDirection: 'column',
              height: '100%',
              borderRadius: 2,
              opacity: 0.6,
              alignItems: 'center',
              justifyContent: 'center',
            }}
          >
            <Typography
              variant="h6"
              gutterBottom
              sx={{ textAlign: 'center', mb: 1 }}
            >
              Site Statistics
            </Typography>
            <BarChartIcon
              sx={{ fontSize: 48, color: 'text.secondary', mb: 1 }}
            />
            <Typography color="text.secondary" sx={{ textAlign: 'center' }}>
              (Analytics Coming Soon)
            </Typography>
          </Paper>
        </Grid>
      </Grid>
    </Container>
  );
};

export default AdminDashboardPage;
