import { useContext, useState } from 'react';
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
  Email as EmailIcon,
} from '@mui/icons-material';
import { AuthContext } from '../../contexts/AuthContext';

const Header = () => {
  const { isAuthenticated, logout, user } = useContext(AuthContext);
  const navigate = useNavigate();
  const [drawerOpen, setDrawerOpen] = useState(false);
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));

  const handleLogout = () => {
    logout();
    navigate('/');
    setDrawerOpen(false);
  };

  const toggleDrawer = (open: boolean) => () => {
    setDrawerOpen(open);
  };

  const handleNavigation = (path: string) => {
    navigate(path);
    setDrawerOpen(false);
  };

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
            {isAuthenticated && user?.roles?.includes('Admin') && (
              <ListItem disablePadding>
                <ListItemButton
                  onClick={() => handleNavigation('/admin/inquiries')}
                >
                  <ListItemIcon>
                    <EmailIcon />
                  </ListItemIcon>
                  <ListItemText primary="Admin Inquiries" />
                </ListItemButton>
              </ListItem>
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
            {isAuthenticated ? (
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
                {isAuthenticated && user?.roles?.includes('Admin') && (
                  <Button
                    color="inherit"
                    component={RouterLink}
                    to="/admin/inquiries"
                    sx={{ ml: 2 }}
                    startIcon={<EmailIcon />}
                  >
                    Admin Inquiries
                  </Button>
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
