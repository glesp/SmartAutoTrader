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
        private readonly ApplicationDbContext context = context;

        // GET: api/Favorites
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

        // POST: api/Favorites
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

        // DELETE: api/Favorites/5
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

        // GET: api/Favorites/Check/5
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

        // GET: api/Favorites/Count
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