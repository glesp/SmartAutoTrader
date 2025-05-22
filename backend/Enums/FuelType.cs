/* <copyright file="FuelType.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the FuelType enumeration, which represents the various types of fuel used by vehicles in the Smart Auto Trader application.
</summary>
<remarks>
The FuelType enumeration is used to categorize vehicles based on their fuel type. It includes common fuel types such as Petrol, Diesel, Electric, and Hybrid, as well as Plugin hybrids. This enumeration is typically used in vehicle-related data models and APIs to filter or classify vehicles by fuel type.
</remarks>
<dependencies>
- None
</dependencies>
 */

namespace SmartAutoTrader.API.Enums
{
    /// <summary>
    /// Represents the types of fuel used by vehicles.
    /// </summary>
    /// <remarks>
    /// This enumeration is used to classify vehicles based on their fuel type. It supports filtering and categorization in vehicle-related operations.
    /// </remarks>
    /// <example>
    /// FuelType fuel = FuelType.Electric;
    /// Console.WriteLine($"Selected fuel type: {fuel}").
    /// </example>
    public enum FuelType
    {
        /// <summary>
        /// Represents vehicles that use petrol as their fuel type.
        /// </summary>
        Petrol,

        /// <summary>
        /// Represents vehicles that use diesel as their fuel type.
        /// </summary>
        Diesel,

        /// <summary>
        /// Represents vehicles that are fully electric and use electricity as their fuel type.
        /// </summary>
        Electric,

        /// <summary>
        /// Represents vehicles that use a combination of fuel and electric power (hybrid vehicles).
        /// </summary>
        Hybrid,

        /// <summary>
        /// Represents vehicles that use a combination of fuel and electric power and can be plugged in to recharge (plugin hybrids).
        /// </summary>
        Plugin,
    }
}