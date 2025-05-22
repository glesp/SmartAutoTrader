/* <copyright file="Inquiry.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the Inquiry class, which represents a user inquiry about a vehicle in the Smart Auto Trader application.
</summary>
<remarks>
The Inquiry class is used to track and manage user inquiries about specific vehicles. It includes metadata such as the subject, message, response, and timestamps for when the inquiry was sent and replied to. This class is typically used for facilitating communication between users and the application or vehicle sellers.
</remarks>
<dependencies>
- System.ComponentModel.DataAnnotations
- SmartAutoTrader.API.Enums
</dependencies>
 */

namespace SmartAutoTrader.API.Models
{
    using System.ComponentModel.DataAnnotations;
    using SmartAutoTrader.API.Enums;

    /// <summary>
    /// Represents a user inquiry about a vehicle.
    /// </summary>
    /// <remarks>
    /// This class encapsulates details about a user's inquiry, including the subject, message, response, and associated metadata.
    /// </remarks>
    public class Inquiry
    {
        /// <summary>
        /// Gets or sets the unique identifier for the inquiry.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the user who submitted the inquiry.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the user who submitted the inquiry.
        /// </summary>
        /// <remarks>
        /// This property establishes a relationship between the inquiry and the user entity.
        /// </remarks>
        public User? User { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the vehicle associated with the inquiry.
        /// </summary>
        public int VehicleId { get; set; }

        /// <summary>
        /// Gets or sets the vehicle associated with the inquiry.
        /// </summary>
        /// <remarks>
        /// This property establishes a relationship between the inquiry and the vehicle entity.
        /// </remarks>
        public Vehicle? Vehicle { get; set; }

        /// <summary>
        /// Gets or sets the subject of the inquiry.
        /// </summary>
        [Required]
        public string Subject { get; set; } = null!;

        /// <summary>
        /// Gets or sets the message content of the inquiry.
        /// </summary>
        [Required]
        public string Message { get; set; } = null!;

        /// <summary>
        /// Gets or sets the response to the inquiry, if any.
        /// </summary>
        public string? Response { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the inquiry was sent.
        /// </summary>
        /// <remarks>
        /// Defaults to the current date and time when the inquiry is created.
        /// </remarks>
        public DateTime DateSent { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the date and time when the inquiry was replied to, if applicable.
        /// </summary>
        public DateTime? DateReplied { get; set; }

        /// <summary>
        /// Gets or sets the status of the inquiry.
        /// </summary>
        /// <remarks>
        /// Defaults to <see cref="InquiryStatus.New"/> when the inquiry is created.
        /// </remarks>
        public InquiryStatus Status { get; set; } = InquiryStatus.New;
    }
}