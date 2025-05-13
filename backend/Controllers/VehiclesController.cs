namespace SmartAutoTrader.API.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using SmartAutoTrader.API.Data;
    using SmartAutoTrader.API.Enums;
    using SmartAutoTrader.API.Helpers;
    using SmartAutoTrader.API.Models;
    using SmartAutoTrader.API.Services;

    [Route("api/[controller]")]
    [ApiController]
    public class VehiclesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IBlobStorageService _blobStorageService;
        private readonly ILogger<VehiclesController> _logger;

        public VehiclesController(
            ApplicationDbContext context,
            IBlobStorageService blobStorageService,
            ILogger<VehiclesController> logger)
        {
            this._context = context;
            this._blobStorageService = blobStorageService;
            this._logger = logger;
        }

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
            [FromQuery] double? minEngineSize = null,
            [FromQuery] double? maxEngineSize = null,
            [FromQuery] int? minHorsepower = null,
            [FromQuery] int? maxHorsepower = null,
            [FromQuery] string sortBy = "DateListed",
            [FromQuery] bool ascending = false,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            IQueryable<Vehicle> query = this._context.Vehicles
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

            if (minEngineSize.HasValue)
            {
                query = query.Where(v => v.EngineSize >= minEngineSize.Value);
            }

            if (maxEngineSize.HasValue)
            {
                query = query.Where(v => v.EngineSize <= maxEngineSize.Value);
            }

            if (minHorsepower.HasValue)
            {
                query = query.Where(v => v.HorsePower >= minHorsepower.Value);
            }

            if (maxHorsepower.HasValue)
            {
                query = query.Where(v => v.HorsePower <= maxHorsepower.Value);
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
            Vehicle? vehicle = await this._context.Vehicles
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

                _ = this._context.BrowsingHistory.Add(history);
                _ = await this._context.SaveChangesAsync();
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

            this._context.Entry(vehicle).State = EntityState.Modified;

            try
            {
                _ = await this._context.SaveChangesAsync();
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

        [HttpPost("{vehicleId}/images")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadVehicleImage(int vehicleId, IFormFile imageFile)
        {
            this._logger.LogInformation("Attempting to upload image for vehicle ID: {VehicleId}", vehicleId);

            // Validate imageFile
            if (imageFile == null || imageFile.Length == 0)
            {
                this._logger.LogWarning("Image file is null or empty for vehicle ID: {VehicleId}", vehicleId);
                return this.BadRequest("Image file is required.");
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                this._logger.LogWarning("Invalid image file extension '{Extension}' for vehicle ID: {VehicleId}", extension, vehicleId);
                return this.BadRequest("Invalid file type. Only .jpg, .jpeg, .png are allowed.");
            }

            long maxFileSize = 5 * 1024 * 1024; // 5MB
            if (imageFile.Length > maxFileSize)
            {
                this._logger.LogWarning("Image file size {FileSize} exceeds maximum of {MaxFileSize} for vehicle ID: {VehicleId}", imageFile.Length, maxFileSize, vehicleId);
                return this.BadRequest($"File size exceeds the limit of {maxFileSize / (1024 * 1024)}MB.");
            }

            try
            {
                var vehicle = await this._context.Vehicles
                    .Include(v => v.Images)
                    .FirstOrDefaultAsync(v => v.Id == vehicleId);

                if (vehicle == null)
                {
                    this._logger.LogWarning("Vehicle not found with ID: {VehicleId} for image upload", vehicleId);
                    return this.NotFound($"Vehicle with ID {vehicleId} not found.");
                }

                var blobName = $"vehicles/{vehicleId}/{Guid.NewGuid()}{extension}";
                string imageUrl;

                await using (var stream = imageFile.OpenReadStream())
                {
                    imageUrl = await this._blobStorageService.UploadFileToBlobAsync(blobName, imageFile.ContentType, stream);
                }

                this._logger.LogInformation("Image uploaded to blob storage for vehicle ID: {VehicleId}. URL: {ImageUrl}", vehicleId, imageUrl);

                // Database logic
                var existingPrimaryImage = vehicle.Images?.FirstOrDefault(img => img.IsPrimary);
                if (existingPrimaryImage != null)
                {
                    existingPrimaryImage.IsPrimary = false;
                }

                var newVehicleImage = new VehicleImage
                {
                    ImageUrl = imageUrl,
                    IsPrimary = true,
                    VehicleId = vehicleId,
                };

                this._context.VehicleImages.Add(newVehicleImage);
                await this._context.SaveChangesAsync();

                this._logger.LogInformation("New vehicle image record saved to database for vehicle ID: {VehicleId}. Image ID: {ImageId}", vehicleId, newVehicleImage.Id);

                return this.Ok(new
                {
                    vehicleId,
                    imageId = newVehicleImage.Id,
                    imageUrl = newVehicleImage.ImageUrl,
                    isPrimary = newVehicleImage.IsPrimary,
                });
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error uploading image for vehicle ID: {VehicleId}", vehicleId);
                return this.StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while uploading the image.");
            }
        }

        // DELETE: api/Vehicles/5
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteVehicle(int id)
        {
            Vehicle? vehicle = await this._context.Vehicles.FindAsync(id);
            if (vehicle == null)
            {
                return this.NotFound();
            }

            _ = this._context.Vehicles.Remove(vehicle);
            _ = await this._context.SaveChangesAsync();

            return this.NoContent();
        }

        private bool VehicleExists(int id)
        {
            return this._context.Vehicles.Any(e => e.Id == id);
        }

        [HttpGet("available-makes")]
        public IActionResult GetAvailableMakes()
        {
            List<string> makes = this._context.Vehicles
                .Select(v => v.Make)
                .Distinct()
                .OrderBy(m => m)
                .ToList();

            return this.Ok(makes);
        }

        [HttpGet("available-models")]
        public IActionResult GetAvailableModels([FromQuery] string make)
        {
            List<string> models = this._context.Vehicles
                .Where(v => v.Make == make)
                .Select(v => v.Model)
                .Distinct()
                .OrderBy(m => m)
                .ToList();

            return this.Ok(models);
        }

        [HttpGet("year-range")]
        public IActionResult GetYearRange()
        {
            if (!this._context.Vehicles.Any())
            {
                return this.Ok(new { min = 0, max = 0 });
            }

            int minYear = this._context.Vehicles.Min(v => v.Year);
            int maxYear = this._context.Vehicles.Max(v => v.Year);

            return this.Ok(new { min = minYear, max = maxYear });
        }

        [HttpGet("engine-size-range")]
        public IActionResult GetEngineSizeRange()
        {
            if (!this._context.Vehicles.Any())
            {
                return this.Ok(new { min = 0.0, max = 0.0 });
            }

            double minEngineSize = this._context.Vehicles.Min(v => v.EngineSize);
            double maxEngineSize = this._context.Vehicles.Max(v => v.EngineSize);

            return this.Ok(new { min = minEngineSize, max = maxEngineSize });
        }

        [HttpGet("horsepower-range")]
        public IActionResult GetHorsepowerRange()
        {
            if (!this._context.Vehicles.Any())
            {
                return this.Ok(new { min = 0, max = 0 });
            }

            int minHorsepower = this._context.Vehicles.Min(v => v.HorsePower);
            int maxHorsepower = this._context.Vehicles.Max(v => v.HorsePower);

            return this.Ok(new { min = minHorsepower, max = maxHorsepower });
        }
    }
}