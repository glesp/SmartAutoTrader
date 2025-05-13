import { jwtDecode, JwtPayload } from 'jwt-decode';

// Define a custom interface for the JWT payload that includes potential role claims
export interface JwtUserPayload extends JwtPayload {
  // Standard claims
  nameid?: string;
  unique_name?: string;
  email?: string;
  // Role claims - could be string or array of strings
  role?: string | string[];
  // .NET standard claims format
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'?:
    | string
    | string[];
}

export const decodeTokenAndExtractRoles = (
  token: string
): (JwtUserPayload & { roles: string[] }) | null => {
  try {
    const decoded: JwtUserPayload = jwtDecode<JwtUserPayload>(token);

    // Extract roles from token claims
    let roles: string[] = [];

    // Case 1: Standard ClaimTypes.Role format from .NET
    if (
      decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
    ) {
      const roleClaim =
        decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      roles = Array.isArray(roleClaim) ? roleClaim : [roleClaim];
    }
    // Case 2: Simple "role" claim
    else if (decoded.role) {
      roles = Array.isArray(decoded.role) ? decoded.role : [decoded.role];
    }

    return { ...decoded, roles };
  } catch (error) {
    console.error('Failed to decode token:', error);
    return null;
  }
};
