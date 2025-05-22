/* <copyright file="InquiryReplyDTO.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the InquiryReplyDto class, which serves as a Data Transfer Object (DTO) for replying to inquiries in the Smart Auto Trader application.
</summary>
<remarks>
The InquiryReplyDto class is used to encapsulate the response content when replying to a user's inquiry. It is typically used in API requests to send replies to inquiries submitted by users. The class is designed to be simple and lightweight, containing only the response message.
</remarks>
<dependencies>
- None
</dependencies>
 */

namespace SmartAutoTrader.API.DTOs
{
    /// <summary>
    /// Represents a Data Transfer Object (DTO) for replying to inquiries.
    /// </summary>
    /// <remarks>
    /// This class is used to encapsulate the response content when replying to a user's inquiry. It is designed for use in API requests.
    /// </remarks>
    public class InquiryReplyDto
    {
        /// <summary>
        /// Gets or sets the response message for the inquiry.
        /// </summary>
        /// <value>A string containing the response message.</value>
        /// <example>"Thank you for your inquiry. The vehicle is still available.".</example>
        public string? Response { get; set; }
    }
}