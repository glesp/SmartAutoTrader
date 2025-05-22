/* <copyright file="FavoritesController.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the FavoritesController class, which provides API endpoints for managing a user's favorite vehicles in the Smart Auto Trader application.
</summary>
<remarks>
The FavoritesController class enables users to perform CRUD operations on their favorite vehicles, including adding, removing, checking, and retrieving favorites. It uses dependency injection for the ApplicationDbContext to interact with the database. The controller is secured with the [Authorize] attribute, ensuring only authenticated users can access its endpoints.
</remarks>
<dependencies>
- Microsoft.AspNetCore.Authorization
- Microsoft.AspNetCore.Mvc
- Microsoft.EntityFrameworkCore
- SmartAutoTrader.API.Data
- SmartAutoTrader.API.Helpers
- SmartAutoTrader.API.Models
</dependencies>
 */

namespace SmartAutoTrader.API.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using SmartAutoTrader.API.Data;
    using SmartAutoTrader.API.Helpers;
    using SmartAutoTrader.API.Models;

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FavoritesController(ApplicationDbContext context) : ControllerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FavoritesController"/> class.
        /// </summary>
        /// <param name="context">The database context for accessing user favorites and vehicles.</param>
        private readonly ApplicationDbContext context = context;

        /// <summary>
        /// Retrieves the list of favorite vehicles for the authenticated user.
        /// </summary>
        /// <returns>A list of vehicles marked as favorites by the user.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if the user is not authenticated.</exception>
        /// <remarks>
        /// This method queries the database for vehicles favorited by the authenticated user and includes related vehicle images.
        /// </remarks>
        /// <example>
        /// GET /api/Favorites.
        /// </example>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Vehicle>>> GetFavorites()
        {
            int? userId = ClaimsHelper.GetUserIdFromClaims(this.User);
            if (userId is null)
            {
                return this.Unauthorized();
            }

            List<Vehicle> favorites = await this.context.UserFavorites
                .Where(uf => uf.UserId == userId)
                .Include(uf => uf.Vehicle)
                .ThenInclude(v => v!.Images)
                .Select(uf => uf.Vehicle!)
                .ToListAsync();

            return favorites;
        }

        /// <summary>
        /// Adds a vehicle to the authenticated user's favorites.
        /// </summary>
        /// <param name="vehicleId">The ID of the vehicle to add to favorites.</param>
        /// <returns>A success message if the vehicle is added to favorites.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if the user is not authenticated.</exception>
        /// <exception cref="KeyNotFoundException">Thrown if the vehicle does not exist.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the vehicle is already in the user's favorites.</exception>
        /// <remarks>
        /// This method checks if the vehicle exists and is not already in the user's favorites before adding it.
        /// </remarks>
        /// <example>
        /// POST /api/Favorites/123.
        /// </example>
        [HttpPost("{vehicleId}")]
        public async Task<IActionResult> AddFavorite(int vehicleId)
        {
            int? userId = ClaimsHelper.GetUserIdFromClaims(this.User);
            if (userId is null)
            {
                return this.Unauthorized();
            }

            // Check if vehicle exists
            Vehicle? vehicle = await this.context.Vehicles.FindAsync(vehicleId);
            if (vehicle == null)
            {
                return this.NotFound(new { Message = "Vehicle not found" });
            }

            // Check if already favorited
            UserFavorite? existingFavorite = await this.context.UserFavorites
                .FirstOrDefaultAsync(uf => uf.UserId == userId && uf.VehicleId == vehicleId);

            if (existingFavorite != null)
            {
                return this.BadRequest(new { Message = "Vehicle already in favorites" });
            }

            // Add to favorites
            UserFavorite favorite = new()
            {
                UserId = userId.Value,
                VehicleId = vehicleId,
                DateAdded = DateTime.Now,
            };

            _ = this.context.UserFavorites.Add(favorite);
            _ = await this.context.SaveChangesAsync();

            return this.Ok(new { Message = "Vehicle added to favorites" });
        }

        /// <summary>
        /// Removes a vehicle from the authenticated user's favorites.
        /// </summary>
        /// <param name="vehicleId">The ID of the vehicle to remove from favorites.</param>
        /// <returns>A success message if the vehicle is removed from favorites.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if the user is not authenticated.</exception>
        /// <exception cref="KeyNotFoundException">Thrown if the vehicle is not in the user's favorites.</exception>
        /// <remarks>
        /// This method checks if the vehicle is in the user's favorites before removing it.
        /// </remarks>
        /// <example>
        /// DELETE /api/Favorites/123.
        /// </example>
        [HttpDelete("{vehicleId}")]
        public async Task<IActionResult> RemoveFavorite(int vehicleId)
        {
            int? userId = ClaimsHelper.GetUserIdFromClaims(this.User);
            if (userId is null)
            {
                return this.Unauthorized();
            }

            UserFavorite? favorite = await this.context.UserFavorites
                .FirstOrDefaultAsync(uf => uf.UserId == userId && uf.VehicleId == vehicleId);

            if (favorite == null)
            {
                return this.NotFound(new { Message = "Vehicle not in favorites" });
            }

            _ = this.context.UserFavorites.Remove(favorite);
            _ = await this.context.SaveChangesAsync();

            return this.Ok(new { Message = "Vehicle removed from favorites" });
        }

        /// <summary>
        /// Checks if a specific vehicle is in the authenticated user's favorites.
        /// </summary>
        /// <param name="vehicleId">The ID of the vehicle to check.</param>
        /// <returns>A boolean indicating whether the vehicle is in the user's favorites.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if the user is not authenticated.</exception>
        /// <remarks>
        /// This method queries the database to determine if the specified vehicle is in the user's favorites.
        /// </remarks>
        /// <example>
        /// GET /api/Favorites/Check/123.
        /// </example>
        [HttpGet("Check/{vehicleId}")]
        public async Task<ActionResult<bool>> CheckFavorite(int vehicleId)
        {
            int? userId = ClaimsHelper.GetUserIdFromClaims(this.User);
            if (userId is null)
            {
                return this.Unauthorized();
            }

            bool isFavorite = await this.context.UserFavorites
                .AnyAsync(uf => uf.UserId == userId && uf.VehicleId == vehicleId);

            return isFavorite;
        }

        /// <summary>
        /// Retrieves the count of favorite vehicles for the authenticated user.
        /// </summary>
        /// <returns>The total number of vehicles in the user's favorites.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if the user is not authenticated.</exception>
        /// <remarks>
        /// This method counts the number of vehicles in the user's favorites and returns the total.
        /// </remarks>
        /// <example>
        /// GET /api/Favorites/Count.
        /// </example>
        [HttpGet("Count")]
        public async Task<ActionResult<int>> GetFavoritesCount()
        {
            int? userId = ClaimsHelper.GetUserIdFromClaims(this.User);
            if (userId is null)
            {
                return this.Unauthorized();
            }

            int count = await this.context.UserFavorites
                .CountAsync(uf => uf.UserId == userId);

            return count;
        }
    }
}