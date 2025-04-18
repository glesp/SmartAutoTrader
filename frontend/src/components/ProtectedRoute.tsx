import { Navigate } from 'react-router-dom';
import { useContext } from 'react';
import { AuthContext } from '../contexts/AuthContext';

const ProtectedRoute = ({ children, roles = [] }) => {
  const { isAuthenticated, user, loading } = useContext(AuthContext);

  if (loading) {
    return <div>Loading...</div>;
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: window.location.pathname }} />;
  }

  // Check roles if specified
  if (
    roles.length > 0 &&
    (!user?.roles || !roles.some((role) => user.roles.includes(role)))
  ) {
    return <Navigate to="/" />;
  }

  return children;
};

export default ProtectedRoute;
