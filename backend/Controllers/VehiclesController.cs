/* <copyright file="VehiclesController.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the VehiclesController class, which provides API endpoints for managing vehicle data in the Smart Auto Trader application.
</summary>
<remarks>
The VehiclesController class allows users to perform CRUD operations on vehicles, retrieve filtered and paginated vehicle listings, and manage vehicle images. It supports advanced filtering, sorting, and pagination for vehicle searches. The controller uses dependency injection for the ApplicationDbContext, IBlobStorageService, and ILogger to interact with the database, manage image uploads, and log application events. Certain endpoints are restricted to administrators using role-based authorization.
</remarks>
<dependencies>
- System
- System.Collections.Generic
- System.Globalization
- System.IO
- System.Linq
- System.Threading.Tasks
- Microsoft.AspNetCore.Authorization
- Microsoft.AspNetCore.Http
- Microsoft.AspNetCore.Mvc
- Microsoft.EntityFrameworkCore
- Microsoft.Extensions.Logging
- SmartAutoTrader.API.Data
- SmartAutoTrader.API.DTOs
- SmartAutoTrader.API.Enums
- SmartAutoTrader.API.Helpers
- SmartAutoTrader.API.Models
- SmartAutoTrader.API.Services
</dependencies>
 */

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
    using SmartAutoTrader.API.DTOs;
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

        /// <summary>
        /// Retrieves a specific vehicle by its ID.
        /// </summary>
        /// <param name="id">The ID of the vehicle to retrieve.</param>
        /// <returns>The vehicle with the specified ID, if it exists.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the vehicle does not exist.</exception>
        /// <remarks>
        /// This method retrieves a specific vehicle by its ID, including related images and features. If the user is authenticated, the vehicle is added to their browsing history.
        /// </remarks>
        /// <example>
        /// GET /api/Vehicles/5.
        /// </example>
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

        /// <summary>
        /// Creates a new vehicle. Restricted to administrators.
        /// </summary>
        /// <param name="createDto">The DTO containing the details of the vehicle to create.</param>
        /// <returns>The created vehicle.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if the user is not an administrator.</exception>
        /// <exception cref="ArgumentException">Thrown if the provided data is invalid.</exception>
        /// <remarks>
        /// This method validates the input data, creates a new vehicle, and saves it to the database.
        /// </remarks>
        /// <example>
        /// POST /api/Vehicles
        /// Body: { "make": "Toyota", "model": "Camry", "year": 2020, "price": 25000, "fuelType": "Petrol" }.
        /// </example>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Vehicle>> PostVehicle([FromBody] CreateVehicleDto createDto)
        {
            if (!this.ModelState.IsValid)
            {
                return this.BadRequest(this.ModelState);
            }

            if (!Enum.TryParse<FuelType>(createDto.FuelType, true, out FuelType parsedFuelType))
            {
                this.ModelState.AddModelError(nameof(createDto.FuelType), $"Invalid fuel type: {createDto.FuelType}.");
            }

            if (!Enum.TryParse<TransmissionType>(createDto.Transmission, true, out TransmissionType parsedTransmissionType))
            {
                this.ModelState.AddModelError(nameof(createDto.Transmission), $"Invalid transmission type: {createDto.Transmission}.");
            }

            if (!Enum.TryParse<VehicleType>(createDto.VehicleType, true, out VehicleType parsedVehicleType))
            {
                this.ModelState.AddModelError(nameof(createDto.VehicleType), $"Invalid vehicle type: {createDto.VehicleType}.");
            }

            if (!this.ModelState.IsValid)
            {
                return this.BadRequest(this.ModelState);
            }

            var newVehicle = new Vehicle
            {
                Make = createDto.Make,
                Model = createDto.Model,
                Year = createDto.Year,
                Price = createDto.Price,
                Mileage = createDto.Mileage ?? 0,
                FuelType = parsedFuelType,
                Transmission = parsedTransmissionType,
                VehicleType = parsedVehicleType,
                EngineSize = createDto.EngineSize ?? 0,
                HorsePower = createDto.HorsePower ?? 0,
                Country = createDto.Country,
                Description = createDto.Description,
                DateListed = DateTime.UtcNow,
                Status = VehicleStatus.Available,
                Images = new List<VehicleImage>(),
                Features = createDto.VehicleFeatures.Select(f => new VehicleFeature { Name = f.Name }).ToList(), // Changed from createDto.Features
            };

            this._context.Vehicles.Add(newVehicle);
            await this._context.SaveChangesAsync();

            return this.CreatedAtAction(nameof(this.GetVehicle), new { id = newVehicle.Id }, newVehicle);
        }

        /// <summary>
        /// Updates an existing vehicle. Restricted to administrators.
        /// </summary>
        /// <param name="id">The ID of the vehicle to update.</param>
        /// <param name="updateDto">The DTO containing the updated details of the vehicle.</param>
        /// <returns>No content if the update is successful.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the vehicle does not exist.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if the user is not an administrator.</exception>
        /// <remarks>
        /// This method validates the input data, updates the specified vehicle, and saves the changes to the database.
        /// </remarks>
        /// <example>
        /// PUT /api/Vehicles/5
        /// Body: { "make": "Toyota", "model": "Corolla", "year": 2021, "price": 22000 }.
        /// </example>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutVehicle(int id, [FromBody] UpdateVehicleDto updateDto)
        {
            // We use the id from the route, no need to check against an id in updateDto
            // if (id != updateDto.Id) // If you were to add Id to UpdateVehicleDto
            // {
            //     return BadRequest("Mismatched ID in route and body.");
            // }
            var existingVehicle = await this._context.Vehicles
                                    .Include(v => v.Features)
                                    .FirstOrDefaultAsync(v => v.Id == id);

            if (existingVehicle == null)
            {
                return this.NotFound($"Vehicle with ID {id} not found.");
            }

            // Validate and parse enums
            if (!Enum.TryParse<FuelType>(updateDto.FuelType, true, out var parsedFuelType))
            {
                this.ModelState.AddModelError(nameof(updateDto.FuelType), $"Invalid fuel type: {updateDto.FuelType}. Valid options are: {string.Join(", ", Enum.GetNames(typeof(FuelType)))}");
            }

            if (!Enum.TryParse<TransmissionType>(updateDto.Transmission, true, out var parsedTransmissionType))
            {
                this.ModelState.AddModelError(nameof(updateDto.Transmission), $"Invalid transmission type: {updateDto.Transmission}. Valid options are: {string.Join(", ", Enum.GetNames(typeof(TransmissionType)))}");
            }

            if (!Enum.TryParse<VehicleType>(updateDto.VehicleType, true, out var parsedVehicleType))
            {
                this.ModelState.AddModelError(nameof(updateDto.VehicleType), $"Invalid vehicle type: {updateDto.VehicleType}. Valid options are: {string.Join(", ", Enum.GetNames(typeof(VehicleType)))}");
            }

            if (!this.ModelState.IsValid)
            {
                return this.BadRequest(this.ModelState);
            }

            // Update properties
            existingVehicle.Make = updateDto.Make;
            existingVehicle.Model = updateDto.Model;
            existingVehicle.Year = updateDto.Year;
            existingVehicle.Price = updateDto.Price;
            existingVehicle.Mileage = updateDto.Mileage ?? existingVehicle.Mileage; // Keep existing if null
            existingVehicle.FuelType = parsedFuelType;
            existingVehicle.Transmission = parsedTransmissionType;
            existingVehicle.VehicleType = parsedVehicleType;
            existingVehicle.EngineSize = updateDto.EngineSize ?? existingVehicle.EngineSize; // Keep existing if null
            existingVehicle.HorsePower = updateDto.HorsePower ?? existingVehicle.HorsePower; // Keep existing if null
            existingVehicle.Country = updateDto.Country;
            existingVehicle.Description = updateDto.Description;

            // existingVehicle.DateModified = DateTime.UtcNow; // If you add a DateModified property

            // Handle Features update: Remove existing and add new ones
            if (existingVehicle.Features != null)
            {
                this._context.VehicleFeatures.RemoveRange(existingVehicle.Features); // EF Core tracks these removals
                existingVehicle.Features.Clear(); // Clear the collection on the entity
            }
            else
            {
                existingVehicle.Features = new List<VehicleFeature>();
            }

            if (updateDto.Features != null && updateDto.Features.Any())
            {
                foreach (var featureDto in updateDto.Features)
                {
                    existingVehicle.Features.Add(new VehicleFeature { Name = featureDto.Name, VehicleId = existingVehicle.Id });
                }
            }

            this._context.Entry(existingVehicle).State = EntityState.Modified;

            try
            {
                await this._context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!this.VehicleExists(id))
                {
                    return this.NotFound();
                }
                else
                {
                    throw;
                }
            }

            return this.NoContent(); // Or return Ok(existingVehicle) if you want to send back the updated entity
        }

        /// <summary>
        /// Deletes a specific vehicle. Restricted to administrators.
        /// </summary>
        /// <param name="id">The ID of the vehicle to delete.</param>
        /// <returns>No content if the deletion is successful.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the vehicle does not exist.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if the user is not an administrator.</exception>
        /// <remarks>
        /// This method deletes the specified vehicle from the database.
        /// </remarks>
        /// <example>
        /// DELETE /api/Vehicles/5.
        /// </example>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
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

        /// <summary>
        /// Uploads an image for a specific vehicle. Restricted to administrators.
        /// </summary>
        /// <param name="vehicleId">The ID of the vehicle to upload the image for.</param>
        /// <param name="imageFile">The image file to upload.</param>
        /// <returns>The details of the uploaded image.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the vehicle does not exist.</exception>
        /// <exception cref="ArgumentException">Thrown if the image file is invalid.</exception>
        /// <remarks>
        /// This method validates the image file, uploads it to blob storage, and associates it with the specified vehicle.
        /// </remarks>
        /// <example>
        /// POST /api/Vehicles/5/images.
        /// </example>
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

        /// <summary>
        /// Deletes a specific image for a vehicle. Restricted to administrators.
        /// </summary>
        /// <param name="vehicleId">The ID of the vehicle the image belongs to.</param>
        /// <param name="imageId">The ID of the image to delete.</param>
        /// <returns>No content if the deletion is successful.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the image does not exist.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if the user is not an administrator.</exception>
        /// <remarks>
        /// This method deletes the specified image from blob storage and removes its record from the database.
        /// </remarks>
        /// <example>
        /// DELETE /api/Vehicles/5/images/10.
        /// </example>
        [HttpDelete("{vehicleId}/images/{imageId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteVehicleImage(int vehicleId, int imageId)
        {
            var image = await this._context.VehicleImages.FirstOrDefaultAsync(img => img.Id == imageId && img.VehicleId == vehicleId);
            if (image == null)
            {
                return this.NotFound();
            }

            // Extract blob name from ImageUrl
            string? imageUrl = image.ImageUrl;
            if (string.IsNullOrEmpty(imageUrl))
            {
                return this.BadRequest("Image URL is missing.");
            }

            try
            {
                // Example: https://account.blob.core.windows.net/vehicle-images/vehicles/123/abc.jpg
                // or http://127.0.0.1:10000/devstoreaccount1/vehicle-images/vehicles/123/abc.jpg
                var uri = new Uri(imageUrl);

                // Blob name is everything after the container name
                // Find "vehicle-images/" in the path and take the rest
                var idx = uri.AbsolutePath.IndexOf("vehicle-images/", StringComparison.OrdinalIgnoreCase);
                if (idx < 0)
                {
                    return this.BadRequest("Could not determine blob name from URL.");
                }

                string blobName = uri.AbsolutePath.Substring(idx).TrimStart('/');

                await this._blobStorageService.DeleteBlobAsync(blobName);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Failed to delete blob for image {ImageId}", imageId);

                // Continue to remove DB record even if blob deletion fails
            }

            bool wasPrimary = image.IsPrimary;

            this._context.VehicleImages.Remove(image);
            await this._context.SaveChangesAsync();

            // If deleted image was primary, set another as primary if any remain
            if (wasPrimary)
            {
                var otherImage = await this._context.VehicleImages
                    .Where(img => img.VehicleId == vehicleId)
                    .OrderBy(img => img.Id)
                    .FirstOrDefaultAsync();

                if (otherImage != null)
                {
                    otherImage.IsPrimary = true;
                    await this._context.SaveChangesAsync();
                }
            }

            return this.NoContent();
        }

        /// <summary>
        /// Sets a specific image as the primary image for a vehicle. Restricted to administrators.
        /// </summary>
        /// <param name="vehicleId">The ID of the vehicle the image belongs to.</param>
        /// <param name="imageId">The ID of the image to set as primary.</param>
        /// <returns>No content if the operation is successful.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the image does not exist.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if the user is not an administrator.</exception>
        /// <remarks>
        /// This method sets the specified image as the primary image for the vehicle and updates the database.
        /// </remarks>
        /// <example>
        /// PUT /api/Vehicles/5/images/10/primary.
        /// </example>
        [HttpPut("{vehicleId}/images/{imageId}/primary")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetPrimaryVehicleImage(int vehicleId, int imageId)
        {
            var vehicle = await this._context.Vehicles
                .Include(v => v.Images)
                .FirstOrDefaultAsync(v => v.Id == vehicleId);

            if (vehicle == null)
            {
                return this.NotFound();
            }

            var targetImage = vehicle.Images?.FirstOrDefault(img => img.Id == imageId);
            if (targetImage == null)
            {
                return this.NotFound();
            }

            foreach (var img in vehicle.Images)
            {
                img.IsPrimary = false;
            }

            targetImage.IsPrimary = true;

            await this._context.SaveChangesAsync();

            return this.NoContent();
        }

        private bool VehicleExists(int id)
        {
            return this._context.Vehicles.Any(e => e.Id == id);
        }

        /// <summary>
        /// Retrieves a list of available makes.
        /// </summary>
        /// <returns>A list of distinct vehicle makes.</returns>
        /// <remarks>
        /// This endpoint returns a list of available vehicle makes for use in filtering vehicle searches.
        /// </remarks>
        /// <example>
        /// GET /api/Vehicles/available-makes.
        /// </example>
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

        /// <summary>
        /// Retrieves a list of available models for a specific make.
        /// </summary>
        /// <param name="make">The make to filter models by.</param>
        /// <returns>A list of distinct models for the specified make.</returns>
        /// <remarks>
        /// This endpoint returns a list of available vehicle models for a given make, to assist in filtering vehicle searches.
        /// </remarks>
        /// <example>
        /// GET /api/Vehicles/available-models?make=Toyota.
        /// </example>
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

        /// <summary>
        /// Retrieves the range of available years for vehicles.
        /// </summary>
        /// <returns>An object containing the minimum and maximum available years.</returns>
        /// <remarks>
        /// This endpoint returns the range of years for which vehicles are available, to assist in filtering vehicle searches by year.
        /// </remarks>
        /// <example>
        /// GET /api/Vehicles/year-range.
        /// </example>
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

        /// <summary>
        /// Retrieves the range of available engine sizes for vehicles.
        /// </summary>
        /// <returns>An object containing the minimum and maximum available engine sizes.</returns>
        /// <remarks>
        /// This endpoint returns the range of engine sizes for which vehicles are available, to assist in filtering vehicle searches by engine size.
        /// </remarks>
        /// <example>
        /// GET /api/Vehicles/engine-size-range.
        /// </example>
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

        /// <summary>
        /// Retrieves the range of available horsepower ratings for vehicles.
        /// </summary>
        /// <returns>An object containing the minimum and maximum available horsepower ratings.</returns>
        /// <remarks>
        /// This endpoint returns the range of horsepower ratings for which vehicles are available, to assist in filtering vehicle searches by horsepower.
        /// </remarks>
        /// <example>
        /// GET /api/Vehicles/horsepower-range.
        /// </example>
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