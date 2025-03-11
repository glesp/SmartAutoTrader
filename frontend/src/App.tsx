import { BrowserRouter, Routes, Route } from 'react-router-dom'
import {
  Container,
  CssBaseline,
  ThemeProvider,
  createTheme,
} from '@mui/material'
import HomePage from './pages/HomePage'
import VehicleListingPage from './pages/VehicleListingPage'
import VehicleDetailPage from './pages/VehicleDetailPage'
import LoginPage from './pages/LoginPage'
import RegisterPage from './pages/RegisterPage'
import ProfilePage from './pages/ProfilePage'
import Header from './components/layout/Header'
import Footer from './components/layout/Footer'
import { AuthProvider } from './contexts'

// Create a theme instance
const theme = createTheme({
  palette: {
    primary: {
      main: '#1976d2', // Blue color
    },
    secondary: {
      main: '#dc004e', // Pink color
    },
  },
})

function App() {
  return (
    <ThemeProvider theme={theme}>
      <CssBaseline /> {/* This normalizes CSS across browsers */}
      <BrowserRouter>
        <AuthProvider>
          <div className="app">
            <Header />
            <Container component="main" sx={{ py: 4 }}>
              <Routes>
                <Route path="/" element={<HomePage />} />
                <Route path="/vehicles" element={<VehicleListingPage />} />
                <Route path="/vehicles/:id" element={<VehicleDetailPage />} />
                <Route path="/login" element={<LoginPage />} />
                <Route path="/register" element={<RegisterPage />} />
                <Route path="/profile" element={<ProfilePage />} />
              </Routes>
            </Container>
            <Footer />
          </div>
        </AuthProvider>
      </BrowserRouter>
    </ThemeProvider>
  )
}

export default App
