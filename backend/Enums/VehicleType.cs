/* <copyright file="VehicleType.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the VehicleType enumeration, which represents the various types of vehicles in the Smart Auto Trader application.
</summary>
<remarks>
The VehicleType enumeration is used to classify vehicles based on their body type. It includes common vehicle types such as Sedan, SUV, Hatchback, and Pickup. This enumeration is typically used in vehicle-related data models and APIs to filter or categorize vehicles by their type.
</remarks>
<dependencies>
- None
</dependencies>
 */

namespace SmartAutoTrader.API.Enums
{
    /// <summary>
    /// Represents the types of vehicles in the system.
    /// </summary>
    /// <remarks>
    /// This enumeration is used to classify vehicles based on their body type. It supports filtering and categorization in vehicle-related operations.
    /// </remarks>
    /// <example>
    /// VehicleType type = VehicleType.SUV;
    /// Console.WriteLine($"Selected vehicle type: {type}").
    /// </example>
    public enum VehicleType
    {
        /// <summary>
        /// Represents a sedan, a passenger car with a separate trunk.
        /// </summary>
        Sedan,

        /// <summary>
        /// Represents a sport utility vehicle (SUV), typically larger and designed for off-road use.
        /// </summary>
        SUV,

        /// <summary>
        /// Represents a hatchback, a car with a rear door that swings upward to provide access to the cargo area.
        /// </summary>
        Hatchback,

        /// <summary>
        /// Represents an estate car, also known as a station wagon, with an extended rear cargo area.
        /// </summary>
        Estate,

        /// <summary>
        /// Represents a coupe, a car with a fixed roof and two doors.
        /// </summary>
        Coupe,

        /// <summary>
        /// Represents a convertible, a car with a roof that can be retracted or removed.
        /// </summary>
        Convertible,

        /// <summary>
        /// Represents a pickup truck, a vehicle with an open cargo area in the rear.
        /// </summary>
        Pickup,

        /// <summary>
        /// Represents a van, a vehicle designed for transporting goods or people.
        /// </summary>
        Van,
    }
}