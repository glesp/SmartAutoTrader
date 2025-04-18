// <copyright file="User.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SmartAutoTrader.API.Models
{
    using System.ComponentModel.DataAnnotations;

    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string? Username { get; set; }

        [Required]
        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        public string? PasswordHash { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? PhoneNumber { get; set; }

        public DateTime DateRegistered { get; set; } = DateTime.Now;

        // Navigation properties
        public ICollection<UserFavorite>? Favorites { get; set; }

        public ICollection<UserPreference>? Preferences { get; set; }

        public ICollection<BrowsingHistory>? BrowsingHistory { get; set; }

        public ICollection<Inquiry>? SentInquiries { get; set; }
    }
}