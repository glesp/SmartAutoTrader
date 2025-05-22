/**
 * @file tokenUtils.ts
 * @summary Provides utility functions for decoding JWT tokens and extracting user information, particularly roles.
 *
 * @description This module uses the `jwt-decode` library to parse JWT tokens. It defines a custom
 * payload interface (`JwtUserPayload`) to accommodate common claims, including standard .NET role claims.
 * The primary exported function, `decodeTokenAndExtractRoles`, decodes a token string and normalizes
 * role information into a consistent `roles` array within the returned payload object.
 *
 * @remarks
 * - Relies on the `jwt-decode` library for the core decoding functionality.
 * - Handles different ways roles might be represented in a JWT (e.g., a single string, an array of strings,
 *   or the standard .NET Core `http://schemas.microsoft.com/ws/2008/06/identity/claims/role` claim type).
 * - Includes error handling for token decoding, returning `null` if decoding fails.
 *
 * @dependencies
 * - jwt-decode: A library for decoding JWTs. (`jwtDecode`, `JwtPayload` types)
 */
import { jwtDecode, JwtPayload } from 'jwt-decode';

/**
 * @interface JwtUserPayload
 * @summary Extends the standard `JwtPayload` to include common user-related claims and role information.
 * @description This interface defines expected properties within a decoded JWT, such as user identifiers,
 * email, and role claims. It specifically accounts for roles being represented as a single string,
 * an array of strings, or via the .NET standard role claim schema.
 *
 * @property {string} [nameid] - Optional: The user's unique identifier (often a GUID or database ID). Corresponds to `ClaimTypes.NameIdentifier`.
 * @property {string} [unique_name] - Optional: The user's unique name, typically the username. Corresponds to `ClaimTypes.Name`.
 * @property {string} [email] - Optional: The user's email address. Corresponds to `ClaimTypes.Email`.
 * @property {string | string[]} [role] - Optional: User roles, can be a single role string or an array of role strings.
 * @property {string | string[]} [http://schemas.microsoft.com/ws/2008/06/identity/claims/role] - Optional: User roles in the standard .NET Core claim format. Can be a single role string or an array of role strings.
 */
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

/**
 * @summary Decodes a JWT token string and extracts user roles into a standardized array.
 * @param {string} token - The JWT token string to decode.
 * @returns {(JwtUserPayload & { roles: string[] }) | null} An object containing the decoded payload
 *          and an additional `roles` array. Returns `null` if the token is invalid or decoding fails.
 * @remarks
 * This function attempts to extract roles from two common claim types:
 * 1. `http://schemas.microsoft.com/ws/2008/06/identity/claims/role` (standard .NET Core claim)
 * 2. A simple `role` claim.
 * If roles are found, they are normalized into an array of strings and added to the returned object
 * as a `roles` property. Errors during decoding are caught, logged to the console, and result in `null` being returned.
 * @example
 * const token = "your.jwt.token.here";
 * const decodedData = decodeTokenAndExtractRoles(token);
 * if (decodedData) {
 *   console.log("User ID:", decodedData.nameid);
 *   console.log("Username:", decodedData.unique_name);
 *   console.log("Roles:", decodedData.roles); // e.g., ['Admin', 'User']
 * } else {
 *   console.error("Invalid token.");
 * }
 */
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
