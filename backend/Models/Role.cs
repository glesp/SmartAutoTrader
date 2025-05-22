/* <copyright file="Role.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the Role class, which represents a user role in the Smart Auto Trader application.
</summary>
<remarks>
The Role class is used to manage user roles within the application. It includes metadata such as the role name and establishes relationships with users through the UserRole join table. This class is typically used for role-based access control and user management.
</remarks>
<dependencies>
- System.ComponentModel.DataAnnotations
</dependencies>
 */

namespace SmartAutoTrader.API.Models;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Represents a user role in the application.
/// </summary>
/// <remarks>
/// This class defines a role that can be assigned to users, such as "Admin" or "Customer". It also establishes relationships with users through the UserRole join table.
/// </remarks>
public class Role
{
    /// <summary>
    /// Gets or sets the unique identifier for the role.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the role.
    /// </summary>
    /// <remarks>
    /// The name must be unique and is limited to 50 characters.
    /// </remarks>
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the collection of user-role relationships associated with this role.
    /// </summary>
    /// <remarks>
    /// This navigation property establishes a many-to-many relationship between roles and users through the UserRole join table.
    /// </remarks>
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}