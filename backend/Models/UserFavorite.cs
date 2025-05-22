/* <copyright file="UserFavorite.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the UserFavorite class, which represents a user's favorite vehicle in the Smart Auto Trader application.
</summary>
<remarks>
The UserFavorite class is used to track vehicles that a user has marked as favorites. It includes metadata such as the date the vehicle was added to the favorites list and establishes relationships with the User and Vehicle entities.
</remarks>
<dependencies>
- System.ComponentModel.DataAnnotations
</dependencies>
 */

namespace SmartAutoTrader.API.Models;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Represents a user's favorite vehicle.
/// </summary>
/// <remarks>
/// This class tracks vehicles that a user has marked as favorites and includes metadata such as the date added.
/// </remarks>
public class UserFavorite
{
    /// <summary>
    /// Gets or sets the unique identifier for the favorite record.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the user who marked the vehicle as a favorite.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the user who marked the vehicle as a favorite.
    /// </summary>
    /// <remarks>
    /// This property establishes a relationship between the favorite record and the user entity.
    /// </remarks>
    public User? User { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the vehicle marked as a favorite.
    /// </summary>
    public int VehicleId { get; set; }

    /// <summary>
    /// Gets or sets the vehicle marked as a favorite.
    /// </summary>
    /// <remarks>
    /// This property establishes a relationship between the favorite record and the vehicle entity.
    /// </remarks>
    public Vehicle? Vehicle { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the vehicle was added to the favorites list.
    /// </summary>
    /// <remarks>
    /// Defaults to the current date and time when the record is created.
    /// </remarks>
    public DateTime DateAdded { get; set; } = DateTime.Now;
}