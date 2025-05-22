/* <copyright file="VehicleStatus.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the VehicleStatus enumeration, which represents the various statuses of a vehicle in the Smart Auto Trader application.
</summary>
<remarks>
The VehicleStatus enumeration is used to track and manage the availability of vehicles in the system. It includes statuses such as Available, Reserved, and Sold. This enumeration is typically used in vehicle-related data models and APIs to filter or categorize vehicles based on their current status.
</remarks>
<dependencies>
- None
</dependencies>
 */

namespace SmartAutoTrader.API.Enums
{
    /// <summary>
    /// Represents the statuses of a vehicle in the system.
    /// </summary>
    /// <remarks>
    /// This enumeration is used to classify vehicles based on their availability status. It supports filtering and categorization in vehicle-related operations.
    /// </remarks>
    /// <example>
    /// VehicleStatus status = VehicleStatus.Available;
    /// Console.WriteLine($"Current vehicle status: {status}").
    /// </example>
    public enum VehicleStatus
    {
        /// <summary>
        /// Indicates that the vehicle is available for purchase or reservation.
        /// </summary>
        Available,

        /// <summary>
        /// Indicates that the vehicle has been reserved by a user but not yet sold.
        /// </summary>
        Reserved,

        /// <summary>
        /// Indicates that the vehicle has been sold and is no longer available.
        /// </summary>
        Sold,
    }
}