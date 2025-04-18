import js from '@eslint/js';
import globals from 'globals';
import reactHooks from 'eslint-plugin-react-hooks';
import reactRefresh from 'eslint-plugin-react-refresh';
import tseslint from 'typescript-eslint';
import eslintPluginPrettierRecommended from 'eslint-plugin-prettier/recommended';

export default tseslint.config(
  // --- Global Ignores ---
  { ignores: ['dist'] }, // Ignore build directory

  // --- Shared Configurations  ---
  js.configs.recommended,
  ...tseslint.configs.recommended,
  eslintPluginPrettierRecommended, // ** Prettier integration (Disables conflicting ESLint style rules & enables Prettier plugin) **

  // --- Configuration Specific to TS/TSX files ---
  {
    files: ['**/*.{ts,tsx}'], // Target TS and TSX files
    languageOptions: {
      ecmaVersion: 2020,
      globals: globals.browser, // Set browser environment globals
    },
    plugins: {
      // Enable specific plugins
      'react-hooks': reactHooks,
      'react-refresh': reactRefresh,
      // 'prettier' plugin is automatically enabled by eslintPluginPrettierRecommended
    },
    rules: {
      // --- React Hooks Rules ---
      ...reactHooks.configs.recommended.rules, // Apply recommended React Hooks rules

      // --- React Refresh Rule ---
      'react-refresh/only-export-components': [
        'warn',
        { allowConstantExport: true },
      ],

      'react/prop-types': 'off', // Turns off prop-types rule (common in TypeScript projects)
      '@typescript-eslint/no-unused-vars': 'warn', // Warns about unused variables (helps clean up code)
    },
  }
);
