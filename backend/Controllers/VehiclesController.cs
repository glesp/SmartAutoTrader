// <copyright file="VehiclesController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SmartAutoTrader.API.Controllers
{
    using System.Globalization;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using SmartAutoTrader.API.Data;
    using SmartAutoTrader.API.Helpers;
    using SmartAutoTrader.API.Models;

    [Route("api/[controller]")]
    [ApiController]
    public class VehiclesController(ApplicationDbContext context) : ControllerBase
    {
        private readonly ApplicationDbContext context = context;

        // GET: api/Vehicles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Vehicle>>> GetVehicles(
            [FromQuery] string? make = null,
            [FromQuery] string? model = null,
            [FromQuery] int? minYear = null,
            [FromQuery] int? maxYear = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] FuelType? fuelType = null,
            [FromQuery] TransmissionType? transmission = null,
            [FromQuery] VehicleType? vehicleType = null,
            [FromQuery] int? minMileage = null,
            [FromQuery] int? maxMileage = null,
            [FromQuery] string sortBy = "DateListed",
            [FromQuery] bool ascending = false,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            IQueryable<Vehicle> query = this.context.Vehicles
                .Include(v => v.Images)
                .Include(v => v.Features)
                .Where(v => v.Status == VehicleStatus.Available);

            // Apply filters
            if (!string.IsNullOrEmpty(make))
            {
                query = query.Where(v => v.Make.Contains(make));
            }

            if (!string.IsNullOrEmpty(model))
            {
                query = query.Where(v => v.Model.Contains(model));
            }

            if (minYear.HasValue)
            {
                query = query.Where(v => v.Year >= minYear.Value);
            }

            if (maxYear.HasValue)
            {
                query = query.Where(v => v.Year <= maxYear.Value);
            }

            if (minPrice.HasValue)
            {
                query = query.Where(v => v.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(v => v.Price <= maxPrice.Value);
            }

            if (fuelType.HasValue)
            {
                query = query.Where(v => v.FuelType == fuelType.Value);
            }

            if (transmission.HasValue)
            {
                query = query.Where(v => v.Transmission == transmission.Value);
            }

            if (vehicleType.HasValue)
            {
                query = query.Where(v => v.VehicleType == vehicleType.Value);
            }

            if (minMileage.HasValue)
            {
                query = query.Where(v => v.Mileage >= minMileage.Value);
            }

            if (maxMileage.HasValue)
            {
                query = query.Where(v => v.Mileage <= maxMileage.Value);
            }

            // Apply sorting
            query = sortBy.ToLower(CultureInfo.CurrentCulture) switch
            {
                "price" => ascending ? query.OrderBy(v => v.Price) : query.OrderByDescending(v => v.Price),
                "year" => ascending ? query.OrderBy(v => v.Year) : query.OrderByDescending(v => v.Year),
                "mileage" => ascending ? query.OrderBy(v => v.Mileage) : query.OrderByDescending(v => v.Mileage),
                "make" => ascending ? query.OrderBy(v => v.Make) : query.OrderByDescending(v => v.Make),
                "model" => ascending ? query.OrderBy(v => v.Model) : query.OrderByDescending(v => v.Model),
                _ => ascending ? query.OrderBy(v => v.DateListed) : query.OrderByDescending(v => v.DateListed),
            };

            // Apply pagination
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            List<Vehicle> vehicles = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Add pagination headers
            this.Response.Headers.Append("X-Total-Count", totalItems.ToString());
            this.Response.Headers.Append("X-Total-Pages", totalPages.ToString());

            return vehicles;
        }

        // GET: api/Vehicles/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Vehicle>> GetVehicle(int id)
        {
            Vehicle? vehicle = await this.context.Vehicles
                .Include(v => v.Images)
                .Include(v => v.Features)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (vehicle is null)
            {
                return this.NotFound();
            }

            // Optional history tracking for authenticated users
            if (this.User.Identity?.IsAuthenticated == true)
            {
                int? userId = ClaimsHelper.GetUserIdFromClaims(this.User);
                if (userId is null)
                {
                    return this.Unauthorized();
                }

                BrowsingHistory history = new()
                {
                    UserId = userId.Value,
                    VehicleId = id,
                    ViewDate = DateTime.Now,
                    ViewDurationSeconds = 0,
                };

                _ = this.context.BrowsingHistory.Add(history);
                _ = await this.context.SaveChangesAsync();
            }

            return vehicle;
        }

        // PUT: api/Vehicles/5
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutVehicle(int id, Vehicle vehicle)
        {
            if (id != vehicle.Id)
            {
                return this.BadRequest();
            }

            this.context.Entry(vehicle).State = EntityState.Modified;

            try
            {
                _ = await this.context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!this.VehicleExists(id))
                {
                    return this.NotFound();
                }

                throw;
            }

            return this.NoContent();
        }

        // DELETE: api/Vehicles/5
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteVehicle(int id)
        {
            Vehicle? vehicle = await this.context.Vehicles.FindAsync(id);
            if (vehicle == null)
            {
                return this.NotFound();
            }

            _ = this.context.Vehicles.Remove(vehicle);
            _ = await this.context.SaveChangesAsync();

            return this.NoContent();
        }

        private bool VehicleExists(int id)
        {
            return this.context.Vehicles.Any(e => e.Id == id);
        }

        [HttpGet("available-makes")]
        public IActionResult GetAvailableMakes()
        {
            List<string> makes =
            [
                .. this.context.Vehicles
                    .Select(v => v.Make)
                    .Distinct()
                    .OrderBy(m => m)
            ];

            return this.Ok(makes);
        }

        [HttpGet("available-models")]
        public IActionResult GetAvailableModels([FromQuery] string make)
        {
            List<string> models =
            [
                .. this.context.Vehicles
                    .Where(v => v.Make == make)
                    .Select(v => v.Model)
                    .Distinct()
                    .OrderBy(m => m)
            ];

            return this.Ok(models);
        }

        [HttpGet("year-range")]
        public IActionResult GetYearRange()
        {
            int minYear = this.context.Vehicles.Min(v => v.Year);
            int maxYear = this.context.Vehicles.Max(v => v.Year);

            return this.Ok(new { min = minYear, max = maxYear });
        }
    }
}