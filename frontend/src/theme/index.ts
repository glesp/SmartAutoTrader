import { createTheme, responsiveFontSizes } from '@mui/material/styles';

// Define custom colors
const primaryColor = '#1976d2'; // Strong blue
const secondaryColor = '#f50057'; // Vibrant magenta/pink
const darkTextColor = '#212121';
const lightTextColor = '#757575';

// Create the base theme
let theme = createTheme({
  palette: {
    primary: {
      light: '#4791db',
      main: primaryColor,
      dark: '#115293',
      contrastText: '#fff',
    },
    secondary: {
      light: '#ff4081',
      main: secondaryColor,
      dark: '#c51162',
      contrastText: '#fff',
    },
    text: {
      primary: darkTextColor,
      secondary: lightTextColor,
    },
    background: {
      default: '#ffffff',
      paper: '#ffffff',
    },
  },
  typography: {
    fontFamily: [
      'Inter',
      'Roboto',
      '"Helvetica Neue"',
      'Arial',
      'sans-serif',
    ].join(','),
    h1: {
      fontWeight: 700,
      fontSize: '2.5rem',
    },
    h2: {
      fontWeight: 600,
      fontSize: '2rem',
    },
    h3: {
      fontWeight: 600,
      fontSize: '1.75rem',
    },
    h4: {
      fontWeight: 600,
      fontSize: '1.5rem',
    },
    h5: {
      fontWeight: 500,
      fontSize: '1.25rem',
    },
    h6: {
      fontWeight: 500,
      fontSize: '1rem',
    },
    button: {
      textTransform: 'none',
      fontWeight: 500,
    },
    subtitle1: {
      fontSize: '1rem',
      color: lightTextColor,
    },
    subtitle2: {
      fontSize: '0.875rem',
      fontWeight: 500,
    },
  },
  shape: {
    borderRadius: 8,
  },
  components: {
    // --- Button Overrides ---
    MuiButton: {
      styleOverrides: {
        root: {
          borderRadius: 8,
          padding: '8px 16px',
          '&.MuiButton-containedPrimary:hover': {
            backgroundColor: '#0d5ca3',
          },
        },
        containedPrimary: {
          boxShadow: '0 4px 6px rgba(25, 118, 210, 0.25)',
        },
        outlinedPrimary: {
          borderWidth: 2,
          '&:hover': {
            borderWidth: 2,
          },
        },
      },
    },

    // --- Card Overrides + Added Margin ---
    MuiCard: {
      styleOverrides: {
        root: ({ theme }) => ({
          boxShadow: '0 4px 10px rgba(0, 0, 0, 0.05)',
          borderRadius: 12,
          marginBottom: theme.spacing(3), // Default bottom margin
        }),
      },
    },
    MuiCardContent: {
      styleOverrides: {
        root: ({ theme }) => ({
          padding: theme.spacing(2), // Default padding inside cards
          '&:last-child': {
            paddingBottom: theme.spacing(2), // Avoid extra padding when actions follow
          },
        }),
      },
    },
    MuiCardActions: {
      styleOverrides: {
        root: ({ theme }) => ({
          padding: theme.spacing(1, 2), // Default padding for actions area
        }),
      },
    },

    // --- Paper Overrides ---
    MuiPaper: {
      styleOverrides: {
        rounded: {
          borderRadius: 12,
        },
        elevation1: {
          boxShadow: '0 2px 8px rgba(0, 0, 0, 0.08)',
        },
      },
    },

    // --- AppBar Overrides ---
    MuiAppBar: {
      styleOverrides: {
        root: {
          boxShadow: '0 2px 10px rgba(0, 0, 0, 0.05)',
        },
      },
    },
    // Consider Toolbar padding based on specific layout (sx might be better here)
    // MuiToolbar: {
    //     styleOverrides: {
    //         root: ({ theme }) => ({
    //             paddingLeft: theme.spacing(2),
    //             paddingRight: theme.spacing(2),
    //         }),
    //     },
    // },

    // --- TextField Overrides ---
    MuiTextField: {
      styleOverrides: {
        root: {
          '& .MuiOutlinedInput-root': {
            borderRadius: 8,
          },
        },
      },
    },

    // --- Chip Overrides ---
    MuiChip: {
      styleOverrides: {
        root: {
          borderRadius: 16,
        },
      },
    },

    // --- Typography Spacing ---
    MuiTypography: {
      styleOverrides: {
        h1: ({ theme }) => ({ marginBottom: theme.spacing(3) }),
        h2: ({ theme }) => ({ marginBottom: theme.spacing(3) }),
        h3: ({ theme }) => ({ marginBottom: theme.spacing(2.5) }),
        h4: ({ theme }) => ({ marginBottom: theme.spacing(2) }),
        h5: ({ theme }) => ({ marginBottom: theme.spacing(1.5) }),
        h6: ({ theme }) => ({ marginBottom: theme.spacing(1) }),
        // paragraph: ({ theme }) => ({ marginBottom: theme.spacing(2) }), // Optional: default paragraph spacing
      },
    },

    // --- Container Padding ---
    MuiContainer: {
      styleOverrides: {
        root: ({ theme }) => ({
          paddingTop: theme.spacing(3),
          paddingBottom: theme.spacing(3),
          [theme.breakpoints.up('md')]: {
            paddingTop: theme.spacing(4),
            paddingBottom: theme.spacing(4),
          },
        }),
      },
    },

    // --- ListItemButton Hover Effect ---
    MuiListItemButton: {
      styleOverrides: {
        root: ({ theme }) => ({
          '&:hover': {
            backgroundColor: theme.palette.action.hover,
          },
        }),
      },
    },
  },
});

// Apply responsive font sizes
theme = responsiveFontSizes(theme);

export default theme;
