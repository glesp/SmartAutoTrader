/* <copyright file="ClaimsHelper.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the ClaimsHelper class, which provides utility methods for extracting user-related information from claims in the Smart Auto Trader application.
</summary>
<remarks>
The ClaimsHelper class is a static helper class designed to simplify the process of retrieving user-specific data, such as the user ID, from the claims associated with a ClaimsPrincipal object. It is typically used in authentication and authorization workflows to identify the currently logged-in user.
</remarks>
<dependencies>
- System.Security.Claims
</dependencies>
 */

namespace SmartAutoTrader.API.Helpers
{
    using System.Security.Claims;

    /// <summary>
    /// Provides utility methods for working with claims in the Smart Auto Trader application.
    /// </summary>
    /// <remarks>
    /// This static class is designed to simplify the extraction of user-related information, such as the user ID, from a ClaimsPrincipal object. It is commonly used in authentication and authorization scenarios.
    /// </remarks>
    public static class ClaimsHelper
    {
        /// <summary>
        /// Retrieves the user ID from the claims of the specified <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <param name="user">The <see cref="ClaimsPrincipal"/> object representing the authenticated user.</param>
        /// <returns>
        /// An integer representing the user ID if it exists and is valid; otherwise, null.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="user"/> parameter is null.</exception>
        /// <remarks>
        /// This method looks for the claim with the type <see cref="ClaimTypes.NameIdentifier"/> and attempts to parse its value as an integer. If the claim is not found or the value cannot be parsed, the method returns null.
        /// </remarks>
        /// <example>
        /// <code>
        /// ClaimsPrincipal user = HttpContext.User;
        /// int? userId = ClaimsHelper.GetUserIdFromClaims(user);
        /// if (userId.HasValue)
        /// {
        ///     Console.WriteLine($"User ID: {userId.Value}");
        /// }
        /// else
        /// {
        ///     Console.WriteLine("User ID not found in claims.");
        /// }
        /// </code>
        /// </example>
        public static int? GetUserIdFromClaims(ClaimsPrincipal user)
        {
            Claim? claim = user.FindFirst(ClaimTypes.NameIdentifier);
            return int.TryParse(claim?.Value, out int userId) ? userId : null;
        }
    }
}