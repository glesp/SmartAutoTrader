/* <copyright file="TransmissionType.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the TransmissionType enumeration, which represents the various types of vehicle transmissions in the Smart Auto Trader application.
</summary>
<remarks>
The TransmissionType enumeration is used to classify vehicles based on their transmission type. It includes common transmission types such as Manual, Automatic, and Semi-Automatic. This enumeration is typically used in vehicle-related data models and APIs to filter or categorize vehicles by their transmission type.
</remarks>
<dependencies>
- None
</dependencies>
 */

namespace SmartAutoTrader.API.Enums
{
    /// <summary>
    /// Represents the types of vehicle transmissions.
    /// </summary>
    /// <remarks>
    /// This enumeration is used to classify vehicles based on their transmission type. It supports filtering and categorization in vehicle-related operations.
    /// </remarks>
    /// <example>
    /// TransmissionType transmission = TransmissionType.Automatic;
    /// Console.WriteLine($"Selected transmission type: {transmission}").
    /// </example>
    public enum TransmissionType
    {
        /// <summary>
        /// Represents vehicles with a manual transmission.
        /// </summary>
        Manual,

        /// <summary>
        /// Represents vehicles with an automatic transmission.
        /// </summary>
        Automatic,

        /// <summary>
        /// Represents vehicles with a semi-automatic transmission.
        /// </summary>
        SemiAutomatic,
    }
}