/* <copyright file="InquiryStatus.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the InquiryStatus enumeration, which represents the various statuses of an inquiry in the Smart Auto Trader application.
</summary>
<remarks>
The InquiryStatus enumeration is used to track the state of inquiries submitted by users. It includes statuses such as New, Read, Replied, and Closed. This enumeration is typically used in inquiry-related data models and APIs to manage and filter inquiries based on their status.
</remarks>
<dependencies>
- None
</dependencies>
 */

namespace SmartAutoTrader.API.Enums
{
    /// <summary>
    /// Represents the statuses of an inquiry in the system.
    /// </summary>
    /// <remarks>
    /// This enumeration is used to track and manage the lifecycle of inquiries submitted by users. It supports filtering and categorization of inquiries based on their current status.
    /// </remarks>
    /// <example>
    /// InquiryStatus status = InquiryStatus.New;
    /// Console.WriteLine($"Current inquiry status: {status}").
    /// </example>
    public enum InquiryStatus
    {
        /// <summary>
        /// Indicates that the inquiry is new and has not been read yet.
        /// </summary>
        New,

        /// <summary>
        /// Indicates that the inquiry has been read but not yet replied to.
        /// </summary>
        Read,

        /// <summary>
        /// Indicates that a reply has been sent for the inquiry.
        /// </summary>
        Replied,

        /// <summary>
        /// Indicates that the inquiry has been closed and requires no further action.
        /// </summary>
        Closed,
    }
}