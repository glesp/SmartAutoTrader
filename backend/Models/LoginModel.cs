/* <copyright file="LoginModel.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the LoginModel class, which represents the data required for user authentication in the Smart Auto Trader application.
</summary>
<remarks>
The LoginModel class is used to encapsulate the user's email and password during the login process. It is typically used in authentication workflows to validate user credentials and initiate a session.
</remarks>
<dependencies>
- None
</dependencies>
 */

namespace SmartAutoTrader.API.Models
{
    /// <summary>
    /// Represents the data required for user authentication.
    /// </summary>
    /// <remarks>
    /// This class encapsulates the user's email and password, which are used during the login process.
    /// </remarks>
    public class LoginModel
    {
        /// <summary>
        /// Gets or sets the email address of the user.
        /// </summary>
        /// <remarks>
        /// This property is required for identifying the user during authentication.
        /// </remarks>
        public string Email { get; set; } = null!;

        /// <summary>
        /// Gets or sets the password of the user.
        /// </summary>
        /// <remarks>
        /// This property is required for validating the user's credentials during authentication.
        /// </remarks>
        public string Password { get; set; } = null!;
    }
}