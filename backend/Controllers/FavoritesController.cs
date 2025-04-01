using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartAutoTrader.API.Data;
using SmartAutoTrader.API.Models;

namespace SmartAutoTrader.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class FavoritesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public FavoritesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/Favorites
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Vehicle>>> GetFavorites()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

        var favorites = await _context.UserFavorites
            .Where(uf => uf.UserId == userId)
            .Include(uf => uf.Vehicle)
            .ThenInclude(v => v.Images)
            .Select(uf => uf.Vehicle)
            .ToListAsync();

        return favorites;
    }

    // POST: api/Favorites
    [HttpPost("{vehicleId}")]
    public async Task<IActionResult> AddFavorite(int vehicleId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

        // Check if vehicle exists
        var vehicle = await _context.Vehicles.FindAsync(vehicleId);
        if (vehicle == null) return NotFound(new { Message = "Vehicle not found" });

        // Check if already favorited
        var existingFavorite = await _context.UserFavorites
            .FirstOrDefaultAsync(uf => uf.UserId == userId && uf.VehicleId == vehicleId);

        if (existingFavorite != null) return BadRequest(new { Message = "Vehicle already in favorites" });

        // Add to favorites
        var favorite = new UserFavorite
        {
            UserId = userId,
            VehicleId = vehicleId,
            DateAdded = DateTime.Now
        };

        _context.UserFavorites.Add(favorite);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Vehicle added to favorites" });
    }

    // DELETE: api/Favorites/5
    [HttpDelete("{vehicleId}")]
    public async Task<IActionResult> RemoveFavorite(int vehicleId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

        var favorite = await _context.UserFavorites
            .FirstOrDefaultAsync(uf => uf.UserId == userId && uf.VehicleId == vehicleId);

        if (favorite == null) return NotFound(new { Message = "Vehicle not in favorites" });

        _context.UserFavorites.Remove(favorite);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Vehicle removed from favorites" });
    }

    // GET: api/Favorites/Check/5
    [HttpGet("Check/{vehicleId}")]
    public async Task<ActionResult<bool>> CheckFavorite(int vehicleId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

        var isFavorite = await _context.UserFavorites
            .AnyAsync(uf => uf.UserId == userId && uf.VehicleId == vehicleId);

        return isFavorite;
    }

    // GET: api/Favorites/Count
    [HttpGet("Count")]
    public async Task<ActionResult<int>> GetFavoritesCount()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

        var count = await _context.UserFavorites
            .CountAsync(uf => uf.UserId == userId);

        return count;
    }
}