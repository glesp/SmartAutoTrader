/**
 * @file Footer.tsx
 * @summary Defines the `Footer` component, which serves as the footer section for the Smart Auto Trader application.
 *
 * @description The `Footer` component provides a consistent footer layout across the application. It includes branding, quick links, contact information, and social media links.
 * The component is styled using Material-UI and is designed to be responsive, adapting to different screen sizes.
 *
 * @remarks
 * - The component uses Material-UI's `Box`, `Container`, `Grid`, `Typography`, and `Link` components for layout and styling.
 * - Icons from Material-UI are used for social media links and contact information.
 * - React Router's `Link` component is used for internal navigation.
 * - The footer is designed to be accessible and visually appealing, with proper semantic HTML and ARIA attributes.
 *
 * @dependencies
 * - Material-UI components: `Box`, `Container`, `Grid`, `Typography`, `Link`
 * - Material-UI icons: `FacebookIcon`, `TwitterIcon`, `InstagramIcon`, `EmailIcon`, `PhoneIcon`, `LocationIcon`
 * - React Router: `Link` for internal navigation
 */

import { Box, Container, Grid, Typography, Link } from '@mui/material';
import {
  Facebook as FacebookIcon,
  Twitter as TwitterIcon,
  Instagram as InstagramIcon,
  Email as EmailIcon,
  Phone as PhoneIcon,
  LocationOn as LocationIcon,
} from '@mui/icons-material';
import { Link as RouterLink } from 'react-router-dom';
import { JSX } from 'react';

/**
 * @function Footer
 * @summary Renders the footer section of the Smart Auto Trader application.
 *
 * @returns {JSX.Element} The rendered footer component.
 *
 * @remarks
 * - The footer includes four main sections: branding, quick links, contact information, and social media links.
 * - It dynamically displays the current year in the copyright notice.
 * - The component is styled to ensure readability and responsiveness across devices.
 *
 * @example
 * <Footer />
 */
const Footer = (): JSX.Element => {
  const currentYear = new Date().getFullYear();

  return (
    <Box
      component="footer"
      sx={{ bgcolor: 'primary.dark', color: 'white', py: 6, mt: 'auto' }}
    >
      <Container>
        <Grid container spacing={4}>
          {/* Branding Section */}
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

          {/* Quick Links Section */}
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

          {/* Contact Information Section */}
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

          {/* Social Media Links Section */}
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

        {/* Copyright Section */}
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
  );
};

export default Footer;
