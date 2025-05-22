/**
 * @file index.tsx
 * @summary Entry point for the `Chat` module, exporting the `ChatInterface` component.
 *
 * @description This file serves as the entry point for the `Chat` module, re-exporting the `ChatInterface` component for use in other parts of the application.
 * It provides both named and default exports for flexibility in importing the component.
 *
 * @remarks
 * - This file simplifies imports for the `ChatInterface` component by providing a single access point.
 * - The default export ensures compatibility with various import styles, while the named export allows for more explicit imports if needed.
 *
 * @dependencies
 * - `ChatInterface` from `./ChatInterface`: The main chat interface component for the Smart Auto Assistant.
 */

import ChatInterface from './ChatInterface';

/**
 * @summary Named export for the `ChatInterface` component.
 *
 * @remarks
 * This export allows for explicit imports of the `ChatInterface` component in other modules.
 *
 * @example
 * // Importing using the named export
 * import { ChatInterface } from './chat';
 */
export { ChatInterface };

/**
 * @summary Default export for the `ChatInterface` component.
 *
 * @remarks
 * This export allows for default imports of the `ChatInterface` component in other modules.
 *
 * @example
 * // Importing using the default export
 * import ChatInterface from './chat';
 */
export default ChatInterface;
