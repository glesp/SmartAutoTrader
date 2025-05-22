/* <copyright file="BrowsingHistory.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the BrowsingHistory class, which represents a user's browsing history for vehicles in the Smart Auto Trader application.
</summary>
<remarks>
The BrowsingHistory class is used to track and store information about the vehicles a user has viewed, including the viewing date, duration, and associated user and vehicle details. This class is typically used for analytics, user behavior tracking, and personalized recommendations.
</remarks>
<dependencies>
- System.ComponentModel.DataAnnotations
</dependencies>
 */

namespace SmartAutoTrader.API.Models
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Represents a record of a user's browsing history for vehicles.
    /// </summary>
    /// <remarks>
    /// This class stores details about a user's interaction with a specific vehicle, including the viewing date and duration. It is linked to both the user and the vehicle entities.
    /// </remarks>
    public class BrowsingHistory
    {
        /// <summary>
        /// Gets or sets the unique identifier for the browsing history record.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the user associated with this browsing history record.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the user associated with this browsing history record.
        /// </summary>
        /// <remarks>
        /// This property establishes a relationship between the browsing history and the user entity.
        /// </remarks>
        public User? User { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the vehicle associated with this browsing history record.
        /// </summary>
        public int VehicleId { get; set; }

        /// <summary>
        /// Gets or sets the vehicle associated with this browsing history record.
        /// </summary>
        /// <remarks>
        /// This property establishes a relationship between the browsing history and the vehicle entity.
        /// </remarks>
        public Vehicle? Vehicle { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the vehicle was viewed.
        /// </summary>
        /// <remarks>
        /// Defaults to the current date and time when the record is created.
        /// </remarks>
        public DateTime ViewDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the duration (in seconds) for which the vehicle was viewed.
        /// </summary>
        public int ViewDurationSeconds { get; set; }
    }
}