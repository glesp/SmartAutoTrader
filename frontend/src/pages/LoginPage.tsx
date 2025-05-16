import { useState, useContext } from 'react';
import { Link as RouterLink, useNavigate, useLocation } from 'react-router-dom';
import { AuthContext } from '../contexts/AuthContext';
import {
  Container,
  Box,
  Typography,
  TextField,
  Button,
  Paper,
  Link,
  CircularProgress,
  Alert,
} from '@mui/material';

const LoginPage = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const { login } = useContext(AuthContext);
  const navigate = useNavigate();
  const location = useLocation();

  // Get redirect path from location state or default to home
  const from = (location.state as { from?: string })?.from || '/';

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!email || !password) {
      setError('Please enter your email and password');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      await login(email, password);
      navigate(from, { replace: true });
    } catch (err: unknown) {
      const errorMessage =
        err instanceof Error
          ? err.message
          : 'Failed to login. Please check your credentials.';
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Container component="main" maxWidth="xs" sx={{ mt: 8, mb: 4 }}>
      <Paper
        elevation={3}
        sx={{
          padding: 4,
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
        }}
      >
        <Typography component="h1" variant="h5" sx={{ mb: 3 }}>
          Log In
        </Typography>

        {error && (
          <Alert severity="error" sx={{ width: '100%', mb: 2 }}>
            {error}
          </Alert>
        )}

        <Box component="form" onSubmit={handleSubmit} sx={{ width: '100%' }}>
          <TextField
            margin="normal"
            required
            fullWidth
            id="email"
            label="Email Address"
            name="email"
            autoComplete="email"
            autoFocus
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            disabled={loading}
          />
          <TextField
            margin="normal"
            required
            fullWidth
            name="password"
            label="Password"
            type="password"
            id="password"
            autoComplete="current-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            disabled={loading}
          />
          <Box sx={{ display: 'flex', justifyContent: 'flex-end', my: 1 }}>
            <Link component={RouterLink} to="/forgot-password" variant="body2">
              Forgot Password?
            </Link>
          </Box>
          <Button
            type="submit"
            fullWidth
            variant="contained"
            disabled={loading}
            sx={{ mt: 3, mb: 2, py: 1.5 }}
          >
            {loading ? (
              <CircularProgress size={24} color="inherit" />
            ) : (
              'Log In'
            )}
          </Button>
        </Box>

        <Typography variant="body2" color="text.secondary" align="center">
          Don't have an account?{' '}
          <Link component={RouterLink} to="/register" variant="body2">
            Register
          </Link>
        </Typography>
      </Paper>
    </Container>
  );
};

export default LoginPage;
