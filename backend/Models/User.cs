/* <copyright file="User.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the User class, which represents a user in the Smart Auto Trader application.
</summary>
<remarks>
The User class is used to manage user information, including authentication details, personal information, and relationships with other entities such as roles, preferences, and browsing history. This class is typically used for user management, authentication, and personalization features.
</remarks>
<dependencies>
- System.ComponentModel.DataAnnotations
</dependencies>
 */

namespace SmartAutoTrader.API.Models
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Represents a user in the application.
    /// </summary>
    /// <remarks>
    /// This class encapsulates user information, including authentication details, personal information, and relationships with other entities such as roles, preferences, and browsing history.
    /// </remarks>
    public class User
    {
        /// <summary>
        /// Gets or sets the unique identifier for the user.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the username of the user.
        /// </summary>
        [Required]
        public string? Username { get; set; }

        /// <summary>
        /// Gets or sets the email address of the user.
        /// </summary>
        [Required]
        [EmailAddress]
        public string? Email { get; set; }

        /// <summary>
        /// Gets or sets the hashed password of the user.
        /// </summary>
        [Required]
        public string? PasswordHash { get; set; }

        /// <summary>
        /// Gets or sets the first name of the user.
        /// </summary>
        public string? FirstName { get; set; }

        /// <summary>
        /// Gets or sets the last name of the user.
        /// </summary>
        public string? LastName { get; set; }

        /// <summary>
        /// Gets or sets the phone number of the user.
        /// </summary>
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the user registered.
        /// </summary>
        /// <remarks>
        /// Defaults to the current date and time when the user is created.
        /// </remarks>
        public DateTime DateRegistered { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the collection of roles associated with the user.
        /// </summary>
        /// <remarks>
        /// This navigation property establishes a many-to-many relationship between users and roles through the UserRole join table.
        /// </remarks>
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

        /// <summary>
        /// Gets or sets the collection of favorite vehicles associated with the user.
        /// </summary>
        public ICollection<UserFavorite>? Favorites { get; set; }

        /// <summary>
        /// Gets or sets the collection of user preferences.
        /// </summary>
        public ICollection<UserPreference>? Preferences { get; set; }

        /// <summary>
        /// Gets or sets the browsing history of the user.
        /// </summary>
        public ICollection<BrowsingHistory>? BrowsingHistory { get; set; }

        /// <summary>
        /// Gets or sets the collection of inquiries sent by the user.
        /// </summary>
        public ICollection<Inquiry>? SentInquiries { get; set; }
    }
}