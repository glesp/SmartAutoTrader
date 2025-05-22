/* <copyright file="RegisterModel.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the RegisterModel class, which represents the data required for user registration in the Smart Auto Trader application.
</summary>
<remarks>
The RegisterModel class is used to encapsulate user-provided information, such as username, email, password, and personal details, during the registration process. It ensures that all required fields are provided and properly validated before creating a new user account.
</remarks>
<dependencies>
- System.ComponentModel.DataAnnotations
</dependencies>
 */

namespace SmartAutoTrader.API.Models
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Represents the data required for user registration.
    /// </summary>
    /// <remarks>
    /// This class encapsulates user-provided information, such as username, email, password, and personal details, for creating a new user account.
    /// </remarks>
    public class RegisterModel
    {
        /// <summary>
        /// Gets or sets the username of the user.
        /// </summary>
        [Required]
        public string Username { get; set; } = default!;

        /// <summary>
        /// Gets or sets the email address of the user.
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; } = default!;

        /// <summary>
        /// Gets or sets the password of the user.
        /// </summary>
        [Required]
        public string Password { get; set; } = default!;

        /// <summary>
        /// Gets or sets the first name of the user.
        /// </summary>
        [Required]
        public string FirstName { get; set; } = default!;

        /// <summary>
        /// Gets or sets the last name of the user.
        /// </summary>
        [Required]
        public string LastName { get; set; } = default!;

        /// <summary>
        /// Gets or sets the phone number of the user.
        /// </summary>
        [Required]
        public string PhoneNumber { get; set; } = default!;
    }
}