import { Navigate } from 'react-router-dom';
import { useContext } from 'react';
import { AuthContext } from '../contexts/AuthContext';

interface ProtectedRouteProps {
  children: React.ReactNode;
  roles?: string[];
}

const ProtectedRoute = ({ children, roles = [] }: ProtectedRouteProps) => {
  const { isAuthenticated, user, loading } = useContext(AuthContext);

  if (loading) {
    return <div>Loading...</div>;
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: window.location.pathname }} />;
  }

  // If roles are specified, check if user has at least one of the required roles
  if (roles.length > 0) {
    // Check both roles array and role string for maximum compatibility
    const hasRequiredRole =
      // Check in roles array (new format)
      (Array.isArray(user?.roles) &&
        user.roles.some((role) => roles.includes(role))) ||
      // Check in singular role property (old format)
      (typeof user?.role === 'string' && roles.includes(user.role));

    if (!hasRequiredRole) {
      console.log(
        'Role check failed. User roles:',
        user?.roles,
        'Required roles:',
        roles
      );
      return <Navigate to="/" />;
    }
  }

  return <>{children}</>;
};

export default ProtectedRoute;
