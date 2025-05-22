/* <copyright file="UserPreference.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the UserPreference class, which represents a user's specific preference in the Smart Auto Trader application.
</summary>
<remarks>
The UserPreference class is used to store and manage user-defined preferences, such as price range, vehicle type, or fuel type. It includes metadata such as the preference type, value, and weight, and establishes a relationship with the User entity.
</remarks>
<dependencies>
- System.ComponentModel.DataAnnotations
</dependencies>
 */

namespace SmartAutoTrader.API.Models;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Represents a user's specific preference.
/// </summary>
/// <remarks>
/// This class tracks user-defined preferences, such as price range or vehicle type, and includes metadata for personalization.
/// </remarks>
public class UserPreference
{
    /// <summary>
    /// Gets or sets the unique identifier for the user preference.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the user associated with this preference.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the user associated with this preference.
    /// </summary>
    /// <remarks>
    /// This property establishes a relationship between the preference and the user entity.
    /// </remarks>
    public User? User { get; set; }

    /// <summary>
    /// Gets or sets the type of the preference.
    /// </summary>
    /// <remarks>
    /// Examples include "PriceRange", "VehicleType", or "FuelType".
    /// </remarks>
    [Required]
    public string PreferenceType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the value of the preference.
    /// </summary>
    /// <remarks>
    /// This property can store simple or complex values, such as JSON strings, depending on the preference type.
    /// </remarks>
    [Required]
    public string Value { get; set; } = null!;

    /// <summary>
    /// Gets or sets the weight of the preference.
    /// </summary>
    /// <remarks>
    /// The weight is used to prioritize preferences during recommendation generation. Defaults to 1.0.
    /// </remarks>
    public float Weight { get; set; } = 1.0f;
}