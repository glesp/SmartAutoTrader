import { Box, Container, Grid, Typography, Link } from '@mui/material'
import {
  Facebook as FacebookIcon,
  Twitter as TwitterIcon,
  Instagram as InstagramIcon,
  Email as EmailIcon,
  Phone as PhoneIcon,
  LocationOn as LocationIcon,
} from '@mui/icons-material'
import { Link as RouterLink } from 'react-router-dom'

const Footer = () => {
  const currentYear = new Date().getFullYear()

  return (
    <Box
      component="footer"
      sx={{ bgcolor: 'primary.dark', color: 'white', py: 6, mt: 'auto' }}
    >
      <Container>
        <Grid container spacing={4}>
          <Grid item xs={12} md={3}>
            <Typography variant="h6" gutterBottom>
              Smart Auto Trader
            </Typography>
            <Typography
              variant="body2"
              color="text.secondary"
              sx={{ color: 'rgba(255, 255, 255, 0.7)' }}
            >
              Find your perfect vehicle with our AI-powered recommendation
              system.
            </Typography>
          </Grid>

          <Grid item xs={12} md={3}>
            <Typography variant="h6" gutterBottom>
              Quick Links
            </Typography>
            <Box component="ul" sx={{ listStyle: 'none', p: 0, m: 0 }}>
              <Box component="li" sx={{ mb: 1 }}>
                <Link
                  component={RouterLink}
                  to="/"
                  color="inherit"
                  underline="hover"
                >
                  Home
                </Link>
              </Box>
              <Box component="li" sx={{ mb: 1 }}>
                <Link
                  component={RouterLink}
                  to="/vehicles"
                  color="inherit"
                  underline="hover"
                >
                  Vehicles
                </Link>
              </Box>
              <Box component="li" sx={{ mb: 1 }}>
                <Link
                  component={RouterLink}
                  to="/login"
                  color="inherit"
                  underline="hover"
                >
                  Login
                </Link>
              </Box>
              <Box component="li" sx={{ mb: 1 }}>
                <Link
                  component={RouterLink}
                  to="/register"
                  color="inherit"
                  underline="hover"
                >
                  Register
                </Link>
              </Box>
            </Box>
          </Grid>

          <Grid item xs={12} md={3}>
            <Typography variant="h6" gutterBottom>
              Contact Us
            </Typography>
            <Box component="ul" sx={{ listStyle: 'none', p: 0, m: 0 }}>
              <Box
                component="li"
                sx={{ mb: 1, display: 'flex', alignItems: 'center' }}
              >
                <EmailIcon fontSize="small" sx={{ mr: 1 }} />
                info@smartautotrader.com
              </Box>
              <Box
                component="li"
                sx={{ mb: 1, display: 'flex', alignItems: 'center' }}
              >
                <PhoneIcon fontSize="small" sx={{ mr: 1 }} />
                (555) 123-4567
              </Box>
              <Box
                component="li"
                sx={{ mb: 1, display: 'flex', alignItems: 'center' }}
              >
                <LocationIcon fontSize="small" sx={{ mr: 1 }} />
                123 Auto Lane, Vehicle City
              </Box>
            </Box>
          </Grid>

          <Grid item xs={12} md={3}>
            <Typography variant="h6" gutterBottom>
              Follow Us
            </Typography>
            <Box sx={{ display: 'flex', gap: 2 }}>
              <Link href="#" color="inherit">
                <FacebookIcon />
              </Link>
              <Link href="#" color="inherit">
                <TwitterIcon />
              </Link>
              <Link href="#" color="inherit">
                <InstagramIcon />
              </Link>
            </Box>
          </Grid>
        </Grid>

        <Box
          sx={{
            borderTop: 1,
            borderColor: 'rgba(255, 255, 255, 0.2)',
            mt: 4,
            pt: 2,
            textAlign: 'center',
          }}
        >
          <Typography
            variant="body2"
            color="text.secondary"
            sx={{ color: 'rgba(255, 255, 255, 0.7)' }}
          >
            &copy; {currentYear} Smart Auto Trader. All rights reserved.
          </Typography>
        </Box>
      </Container>
    </Box>
  )
}

export default Footer
