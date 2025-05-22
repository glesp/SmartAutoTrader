/**
 * @file Header.tsx
 * @summary Defines the `Header` component, which serves as the header and navigation bar for the Smart Auto Trader application.
 *
 * @description The `Header` component provides a responsive navigation bar that includes links to various sections of the application,
 * user authentication actions (login, logout, register), and admin-specific actions. It adapts to different screen sizes by displaying
 * a drawer-based menu on mobile devices and a horizontal menu on larger screens.
 *
 * @remarks
 * - The component uses Material-UI for layout and styling, including `AppBar`, `Toolbar`, `Drawer`, and other components.
 * - React Router is used for navigation, enabling seamless routing between different pages.
 * - The `AuthContext` is used to manage user authentication state and roles.
 * - The component dynamically adjusts its layout and available options based on the user's authentication status and roles.
 *
 * @dependencies
 * - Material-UI components: `AppBar`, `Toolbar`, `Drawer`, `Button`, `Typography`, `IconButton`, `List`, `ListItem`, etc.
 * - Material-UI icons: `MenuIcon`, `HomeIcon`, `CarIcon`, `PersonIcon`, `LogoutIcon`, `StarIcon`, `AdminPanelSettingsIcon`, etc.
 * - React Router: `Link` and `useNavigate` for navigation.
 * - Context: `AuthContext` for authentication state.
 */

import { AuthContext } from '../../contexts/AuthContext';
import { JSX, useContext, useState } from 'react';
import { Link as RouterLink, useNavigate } from 'react-router-dom';
import {
  AppBar,
  Toolbar,
  Typography,
  Button,
  Box,
  Container,
  IconButton,
  Drawer,
  List,
  ListItem,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Divider,
  useTheme,
  useMediaQuery,
} from '@mui/material';
import {
  Star as StarIcon,
  DirectionsCar as CarIcon,
  Person as PersonIcon,
  ExitToApp as LogoutIcon,
  Menu as MenuIcon,
  Home as HomeIcon,
} from '@mui/icons-material';
import AdminPanelSettingsIcon from '@mui/icons-material/AdminPanelSettings';
import AddCircleOutlineIcon from '@mui/icons-material/AddCircleOutline';

/**
 * @function Header
 * @summary Renders the header and navigation bar for the Smart Auto Trader application.
 *
 * @returns {JSX.Element} The rendered header component.
 *
 * @remarks
 * - The header includes a logo, navigation links, and user-specific actions (e.g., login, logout, profile).
 * - On mobile devices, a drawer-based menu is used for navigation.
 * - Admin-specific actions are conditionally displayed based on the user's roles.
 * - The component uses the `AuthContext` to determine the user's authentication state and roles.
 *
 * @example
 * <Header />
 */
const Header = (): JSX.Element => {
  const { isAuthenticated, user, logout } = useContext(AuthContext);
  const navigate = useNavigate();
  const [drawerOpen, setDrawerOpen] = useState(false);
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));

  /**
   * @function handleLogout
   * @summary Logs the user out and navigates to the home page.
   *
   * @returns {void}
   *
   * @remarks
   * - This function calls the `logout` method from the `AuthContext` to clear the user's session.
   * - It also closes the navigation drawer if it is open.
   */
  const handleLogout = (): void => {
    logout();
    navigate('/');
    setDrawerOpen(false);
  };

  /**
   * @function toggleDrawer
   * @summary Toggles the state of the navigation drawer.
   *
   * @param {boolean} open - Whether the drawer should be opened or closed.
   * @returns {() => void} A function to toggle the drawer state.
   */
  const toggleDrawer = (open: boolean) => (): void => {
    setDrawerOpen(open);
  };

  /**
   * @function handleNavigation
   * @summary Navigates to a specified path and closes the drawer.
   *
   * @param {string} path - The path to navigate to.
   * @returns {void}
   */
  const handleNavigation = (path: string): void => {
    navigate(path);
    setDrawerOpen(false);
  };

  /**
   * @constant drawerContent
   * @summary Defines the content of the navigation drawer for mobile devices.
   *
   * @remarks
   * - The drawer includes links to various sections of the application.
   * - Links are conditionally rendered based on the user's authentication state and roles.
   */
  const drawerContent = (
    <Box
      sx={{ width: 250 }}
      role="presentation"
      onClick={toggleDrawer(false)}
      onKeyDown={toggleDrawer(false)}
    >
      <Box sx={{ p: 2 }}>
        <Typography variant="h6" sx={{ display: 'flex', alignItems: 'center' }}>
          <CarIcon sx={{ mr: 1 }} />
          Smart Auto Trader
        </Typography>
      </Box>
      <Divider />
      <List>
        <ListItem disablePadding>
          <ListItemButton onClick={() => handleNavigation('/')}>
            <ListItemIcon>
              <HomeIcon />
            </ListItemIcon>
            <ListItemText primary="Home" />
          </ListItemButton>
        </ListItem>
        <ListItem disablePadding>
          <ListItemButton onClick={() => handleNavigation('/vehicles')}>
            <ListItemIcon>
              <CarIcon />
            </ListItemIcon>
            <ListItemText primary="Vehicles" />
          </ListItemButton>
        </ListItem>
      </List>
      <Divider />
      <List>
        {isAuthenticated ? (
          <>
            <ListItem disablePadding>
              <ListItemButton onClick={() => handleNavigation('/profile')}>
                <ListItemIcon>
                  <PersonIcon />
                </ListItemIcon>
                <ListItemText primary="My Profile" />
              </ListItemButton>
            </ListItem>
            <ListItem disablePadding>
              <ListItemButton
                onClick={() => handleNavigation('/recommendations')}
              >
                <ListItemIcon>
                  <StarIcon />
                </ListItemIcon>
                <ListItemText primary="Your Recommendations" />
              </ListItemButton>
            </ListItem>
            {/* Mobile navigation items */}
            {user?.roles?.includes('Admin') && (
              <>
                <ListItem disablePadding>
                  <ListItemButton
                    onClick={() => handleNavigation('/admin/vehicles/create')}
                  >
                    <ListItemIcon>
                      <AddCircleOutlineIcon /> {/* Or your preferred icon */}
                    </ListItemIcon>
                    <ListItemText primary="Create Vehicle" />
                  </ListItemButton>
                </ListItem>
                <ListItem disablePadding>
                  <ListItemButton
                    onClick={() => handleNavigation('/admin/inquiries')}
                  >
                    <ListItemIcon>
                      <AdminPanelSettingsIcon /> {/* Or your preferred icon */}
                    </ListItemIcon>
                    <ListItemText primary="Admin Inquiries" />
                  </ListItemButton>
                </ListItem>
              </>
            )}
            <ListItem disablePadding>
              <ListItemButton onClick={handleLogout}>
                <ListItemIcon>
                  <LogoutIcon />
                </ListItemIcon>
                <ListItemText primary="Log Out" />
              </ListItemButton>
            </ListItem>
          </>
        ) : (
          <>
            <ListItem disablePadding>
              <ListItemButton onClick={() => handleNavigation('/login')}>
                <ListItemIcon>
                  <LogoutIcon sx={{ transform: 'rotate(180deg)' }} />
                </ListItemIcon>
                <ListItemText primary="Log In" />
              </ListItemButton>
            </ListItem>
            <ListItem disablePadding>
              <ListItemButton onClick={() => handleNavigation('/register')}>
                <ListItemIcon>
                  <PersonIcon />
                </ListItemIcon>
                <ListItemText primary="Register" />
              </ListItemButton>
            </ListItem>
          </>
        )}
      </List>
    </Box>
  );

  return (
    <AppBar position="static">
      <Container>
        <Toolbar>
          {/* Mobile Menu Button */}
          {isMobile && (
            <IconButton
              color="inherit"
              aria-label="open drawer"
              edge="start"
              onClick={toggleDrawer(true)}
              sx={{ mr: 2 }}
            >
              <MenuIcon />
            </IconButton>
          )}

          {/* Logo */}
          <Typography
            variant="h6"
            component={RouterLink}
            to="/"
            sx={{
              display: 'flex',
              alignItems: 'center',
              textDecoration: 'none',
              color: 'inherit',
              flexGrow: 1,
            }}
          >
            <CarIcon sx={{ mr: 1 }} />
            Smart Auto Trader
          </Typography>

          {/* Desktop Navigation Links */}
          <Box sx={{ display: { xs: 'none', md: 'flex' } }}>
            <Button color="inherit" component={RouterLink} to="/">
              Home
            </Button>
            <Button color="inherit" component={RouterLink} to="/vehicles">
              Vehicles
            </Button>

            {/* Auth Actions */}
            {isAuthenticated && user ? (
              <>
                <Button
                  color="inherit"
                  component={RouterLink}
                  to="/profile"
                  startIcon={<PersonIcon />}
                >
                  My Profile
                </Button>
                <Button
                  color="inherit"
                  component={RouterLink}
                  to="/recommendations"
                  startIcon={<StarIcon />}
                >
                  Your Recommendations
                </Button>
                {user.roles?.includes('Admin') && (
                  <>
                    <Button
                      component={RouterLink}
                      to="/admin/vehicles/create"
                      color="inherit"
                      startIcon={<AddCircleOutlineIcon />}
                    >
                      Create Vehicle
                    </Button>
                    <Button
                      component={RouterLink}
                      to="/admin/inquiries"
                      color="inherit"
                      startIcon={<AdminPanelSettingsIcon />}
                    >
                      Admin Inquiries
                    </Button>
                  </>
                )}
                <Button
                  color="inherit"
                  onClick={handleLogout}
                  startIcon={<LogoutIcon />}
                >
                  Log Out
                </Button>
              </>
            ) : (
              <>
                <Button color="inherit" component={RouterLink} to="/login">
                  Log In
                </Button>
                <Button
                  variant="contained"
                  color="secondary"
                  component={RouterLink}
                  to="/register"
                  sx={{ ml: 1 }}
                >
                  Register
                </Button>
              </>
            )}
          </Box>
        </Toolbar>
      </Container>

      {/* Mobile Navigation Drawer */}
      <Drawer anchor="left" open={drawerOpen} onClose={toggleDrawer(false)}>
        {drawerContent}
      </Drawer>
    </AppBar>
  );
};

export default Header;
