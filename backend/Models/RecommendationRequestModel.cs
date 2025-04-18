// <copyright file="RecommendationRequestModel.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SmartAutoTrader.API.Models
{
    using SmartAutoTrader.API.Enums;

    public class RecommendationRequestModel
    {
        public decimal? MinPrice { get; set; }

        public decimal? MaxPrice { get; set; }

        public int? MinYear { get; set; }

        public int? MaxYear { get; set; }

        public List<FuelType>? FuelTypes { get; set; }

        public List<VehicleType>? VehicleTypes { get; set; }

        public List<string>? Makes { get; set; }

        public List<string>? Features { get; set; }

        public string? TextPrompt { get; set; } // Added text prompt

        public int? MaxResults { get; set; }
    }
}