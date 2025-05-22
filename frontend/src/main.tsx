/**
 * @file main.tsx
 * @summary The main entry point for the Smart Auto Trader React application.
 *
 * @description This file is responsible for initializing the React application. It imports the root
 * `App` component and renders it into the DOM element with the ID 'root'. It also imports
 * global CSS styles from `index.css` and wraps the `App` component in `React.StrictMode`
 * to enable additional checks and warnings during development.
 *
 * @remarks
 * - Uses `createRoot` from `react-dom/client` for concurrent rendering, which is the standard
 *   for React 18 and later.
 * - `StrictMode` helps identify potential problems in an application. It does not render any
 *   visible UI and only activates checks and warnings for its descendants.
 * - The `index.css` file typically contains global styles or resets that apply to the entire application.
 *
 * @dependencies
 * - react: `StrictMode` for development checks.
 * - react-dom/client: `createRoot` for rendering the application.
 * - ./App.tsx: The root `App` component of the application.
 * - ./index.css: Global CSS styles for the application.
 */
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import App from './App.tsx';
import './index.css';

/**
 * @description Initializes and renders the React application.
 * It gets the root DOM element by its ID ('root') and uses `createRoot` to render the
 * main `App` component. The `App` component is wrapped in `StrictMode` for development purposes.
 * @throws {Error} If the DOM element with ID 'root' is not found, `document.getElementById('root')!`
 * will result in `null` and `createRoot(null)` would throw an error. The non-null assertion operator `!`
 * implies that the developer expects this element to always exist.
 */
createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>
);
