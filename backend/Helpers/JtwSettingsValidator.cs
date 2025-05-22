/* <copyright file="JtwSettingsValidator.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the JwtSettingsValidator class, which provides utility methods for validating JWT-related configuration settings in the Smart Auto Trader application.
</summary>
<remarks>
The JwtSettingsValidator class is a static helper class designed to ensure that all required JWT configuration keys are present and valid in the application's configuration. It throws exceptions if any required keys are missing or invalid, helping to prevent runtime errors related to JWT authentication.
</remarks>
<dependencies>
- Microsoft.Extensions.Configuration
- System
</dependencies>
 */

namespace SmartAutoTrader.API.Helpers
{
    /// <summary>
    /// Provides utility methods for validating JWT-related configuration settings.
    /// </summary>
    /// <remarks>
    /// This static class ensures that all required JWT configuration keys are present and valid in the application's configuration. It is typically used during application startup to validate the configuration.
    /// </remarks>
    public static class JwtSettingsValidator
    {
        /// <summary>
        /// Validates that all required JWT configuration keys are present and non-empty.
        /// </summary>
        /// <param name="config">The application's configuration object.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if any required JWT configuration key is missing or has an empty value.
        /// </exception>
        /// <remarks>
        /// This method checks for the presence of the following keys in the configuration:
        /// - Jwt:Key
        /// - Jwt:Issuer
        /// - Jwt:Audience
        /// If any of these keys are missing or have an empty value, an exception is thrown.
        /// </remarks>
        /// <example>
        /// <code>
        /// IConfiguration config = new ConfigurationBuilder()
        ///     .AddJsonFile("appsettings.json")
        ///     .Build();
        ///
        /// JwtSettingsValidator.ValidateJwtSettings(config);
        /// </code>
        /// </example>
        public static void ValidateJwtSettings(IConfiguration config)
        {
            string[] requiredKeys =
            [
                "Jwt:Key",
                "Jwt:Issuer",
                "Jwt:Audience"
            ];

            foreach (string key in requiredKeys)
            {
                string? value = config[key];
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new InvalidOperationException($"Missing JWT configuration key: '{key}'");
                }
            }
        }
    }
}