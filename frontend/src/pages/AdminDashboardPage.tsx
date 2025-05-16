import React from 'react';
import { Container, Typography, Paper, Grid, Button, Box } from '@mui/material';
import { Link as RouterLink } from 'react-router-dom';
import AddCircleOutlineIcon from '@mui/icons-material/AddCircleOutline';
import ListAltIcon from '@mui/icons-material/ListAlt';
import EmailIcon from '@mui/icons-material/Email';
import BarChartIcon from '@mui/icons-material/BarChart'; // Example for future stats

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
