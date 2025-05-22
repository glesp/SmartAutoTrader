/**
 * @file ProtectedRoute.tsx
 * @summary Defines the `ProtectedRoute` component, which restricts access to certain routes based on user authentication and roles.
 *
 * @description The `ProtectedRoute` component ensures that only authenticated users with the required roles can access specific routes.
 * If the user is not authenticated, they are redirected to the login page. If the user lacks the required roles, they are redirected to the home page.
 * The component also handles loading states while authentication data is being fetched.
 *
 * @remarks
 * - The component uses React Router's `Navigate` for redirection.
 * - It leverages the `AuthContext` to access the user's authentication state and roles.
 * - The component supports both array-based and string-based role formats for backward compatibility.
 * - It gracefully handles edge cases such as missing user data or undefined roles.
 *
 * @dependencies
 * - React Router: `Navigate` for redirection.
 * - Context: `AuthContext` for user authentication and role management.
 */

import { Navigate } from 'react-router-dom';
import { JSX, useContext } from 'react';
import { AuthContext } from '../contexts/AuthContext';

/**
 * @interface ProtectedRouteProps
 * @summary Defines the props for the `ProtectedRoute` component.
 *
 * @property {React.ReactNode} children - The child components to render if access is granted.
 * @property {string[]} [roles] - An optional array of roles required to access the route.
 */
interface ProtectedRouteProps {
  children: React.ReactNode;
  roles?: string[];
}

/**
 * @function ProtectedRoute
 * @summary Restricts access to a route based on user authentication and roles.
 *
 * @param {ProtectedRouteProps} props - The props for the component.
 * @returns {JSX.Element} The rendered child components if access is granted, or a redirection component otherwise.
 *
 * @throws Will redirect to the login page if the user is not authenticated.
 * @throws Will redirect to the home page if the user lacks the required roles.
 *
 * @remarks
 * - If `roles` are specified, the component checks if the user has at least one of the required roles.
 * - The component supports both array-based (`roles`) and string-based (`role`) formats for user roles.
 * - While authentication data is being fetched, a loading message is displayed.
 *
 * @example
 * <ProtectedRoute roles={['Admin']}>
 *   <AdminDashboard />
 * </ProtectedRoute>
 */
const ProtectedRoute = ({
  children,
  roles = [],
}: ProtectedRouteProps): JSX.Element => {
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
