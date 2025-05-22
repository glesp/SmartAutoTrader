/**
 * @file index.ts
 * @summary Entry point for the `contexts` module, re-exporting authentication-related contexts and providers.
 *
 * @description This file serves as the entry point for the `contexts` module, re-exporting the `AuthContext` and `AuthProvider` components.
 * It simplifies imports for authentication-related functionality by providing a single access point for these exports.
 *
 * @remarks
 * - This file uses `export *` syntax to re-export all exports from the `AuthContext` and `AuthProvider` modules.
 * - By consolidating exports in this file, it improves code organization and makes it easier to import authentication-related functionality elsewhere in the application.
 *
 * @dependencies
 * - `./AuthContext`: Provides the `AuthContext` for managing user authentication state and actions.
 * - `./AuthProvider`: Provides the `AuthProvider` component for wrapping the application with authentication state.
 *
 * @example
 * // Importing from the consolidated `contexts` module
 * import { AuthContext, AuthProvider } from './contexts';
 */

export * from './AuthContext';
export * from './AuthProvider';
