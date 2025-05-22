/* <copyright file="UserRole.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the UserRole class, which represents the relationship between users and roles in the Smart Auto Trader application.
</summary>
<remarks>
The UserRole class is used to establish a many-to-many relationship between users and roles. It includes foreign keys for the User and Role entities and navigation properties to access the related data.
</remarks>
<dependencies>
- None
</dependencies>
 */

namespace SmartAutoTrader.API.Models;

/// <summary>
/// Represents the relationship between a user and a role.
/// </summary>
/// <remarks>
/// This class establishes a many-to-many relationship between users and roles in the application.
/// </remarks>
public class UserRole
{
    /// <summary>
    /// Gets or sets the unique identifier of the user associated with this relationship.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the role associated with this relationship.
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>
    /// Gets or sets the user associated with this relationship.
    /// </summary>
    /// <remarks>
    /// This navigation property establishes a link to the User entity.
    /// </remarks>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the role associated with this relationship.
    /// </summary>
    /// <remarks>
    /// This navigation property establishes a link to the Role entity.
    /// </remarks>
    public virtual Role Role { get; set; } = null!;
}