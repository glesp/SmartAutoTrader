/* <copyright file="InquiryCreateDTO.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the InquiryCreateDto class, which serves as a Data Transfer Object (DTO) for creating new inquiries in the Smart Auto Trader application.
</summary>
<remarks>
The InquiryCreateDto class is used to encapsulate data required for creating a new inquiry about a vehicle. It includes validation attributes to ensure that the provided data meets the application's requirements, such as requiring a subject and message. This DTO is typically used in API requests to submit inquiries from users.
</remarks>
<dependencies>
- System.ComponentModel.DataAnnotations
</dependencies>
 */

namespace SmartAutoTrader.API.DTOs
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Represents a Data Transfer Object (DTO) for creating new inquiries.
    /// </summary>
    /// <remarks>
    /// This class is used to encapsulate the data required for creating a new inquiry about a vehicle. It includes validation rules for each property and is designed for use in API requests.
    /// </remarks>
    public class InquiryCreateDto
    {
        /// <summary>
        /// Gets or sets the ID of the vehicle associated with the inquiry.
        /// </summary>
        /// <value>An integer representing the ID of the vehicle.</value>
        /// <example>123.</example>
        public int VehicleId { get; set; }

        /// <summary>
        /// Gets or sets the subject of the inquiry.
        /// </summary>
        /// <value>A string representing the subject of the inquiry.</value>
        /// <example>"Is this vehicle still available?".</example>
        /// <exception cref="ValidationException">Thrown if the subject is not provided.</exception>
        [Required(ErrorMessage = "Subject is required.")]
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the message content of the inquiry.
        /// </summary>
        /// <value>A string representing the message content of the inquiry.</value>
        /// <example>"I am interested in this vehicle. Can you provide more details about its condition?".</example>
        /// <exception cref="ValidationException">Thrown if the message is not provided.</exception>
        [Required(ErrorMessage = "Message is required.")]
        public string Message { get; set; } = string.Empty;
    }
}