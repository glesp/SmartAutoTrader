/* <copyright file="IVehicleRepository.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the IVehicleRepository interface and its implementation, VehicleRepository, which provide methods for managing vehicle-related data in the Smart Auto Trader application.
</summary>
<remarks>
The IVehicleRepository interface defines methods for retrieving, adding, deleting, and searching for vehicles in the system. The VehicleRepository class implements these methods using Entity Framework Core to interact with the database. This repository is typically used in scenarios where vehicle data needs to be accessed or modified, such as inventory management, search functionality, and vehicle details retrieval.
</remarks>
<dependencies>
- System.Linq.Expressions
- Microsoft.EntityFrameworkCore
- SmartAutoTrader.API.Data
- SmartAutoTrader.API.Models
</dependencies>
 */

namespace SmartAutoTrader.API.Repositories
{
    using System.Linq.Expressions;
    using Microsoft.EntityFrameworkCore;
    using SmartAutoTrader.API.Data;
    using SmartAutoTrader.API.Models;

    /// <summary>
    /// Defines methods for managing vehicle-related data in the system.
    /// </summary>
    public interface IVehicleRepository
    {
        /// <summary>
        /// Retrieves a vehicle by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the vehicle.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="Vehicle"/> object if found; otherwise, null.</returns>
        Task<Vehicle?> GetByIdAsync(int id);

        /// <summary>
        /// Retrieves all vehicles in the system.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of all <see cref="Vehicle"/> objects.</returns>
        Task<List<Vehicle>> GetAllAsync();

        /// <summary>
        /// Searches for vehicles that match the specified predicate.
        /// </summary>
        /// <param name="predicate">The predicate to filter vehicles.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see cref="Vehicle"/> objects that match the predicate.</returns>
        Task<List<Vehicle>> SearchAsync(Expression<Func<Vehicle, bool>> predicate);

        /// <summary>
        /// Adds a new vehicle to the database.
        /// </summary>
        /// <param name="vehicle">The <see cref="Vehicle"/> object to add.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task AddAsync(Vehicle vehicle);

        /// <summary>
        /// Deletes a vehicle from the database.
        /// </summary>
        /// <param name="vehicle">The <see cref="Vehicle"/> object to delete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteAsync(Vehicle vehicle);

        /// <summary>
        /// Saves changes made to the database context.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task SaveChangesAsync();
    }

    /// <summary>
    /// Implements the <see cref="IVehicleRepository"/> interface to manage vehicle-related data using Entity Framework Core.
    /// </summary>
    public class VehicleRepository : IVehicleRepository
    {
        private readonly ApplicationDbContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="VehicleRepository"/> class.
        /// </summary>
        /// <param name="context">The database context used to interact with the vehicles table.</param>
        public VehicleRepository(ApplicationDbContext context)
        {
            this.context = context;
        }

        /// <inheritdoc/>
        public Task<Vehicle?> GetByIdAsync(int id)
        {
            return this.context.Vehicles
                .Include(v => v.Images)
                .Include(v => v.Features)
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        /// <inheritdoc/>
        public Task<List<Vehicle>> GetAllAsync()
        {
            return this.context.Vehicles
                .Include(v => v.Images)
                .Include(v => v.Features)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public Task<List<Vehicle>> SearchAsync(Expression<Func<Vehicle, bool>> predicate)
        {
            return this.context.Vehicles
                .Include(v => v.Images)
                .Include(v => v.Features)
                .Where(predicate)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public Task AddAsync(Vehicle vehicle)
        {
            _ = this.context.Vehicles.Add(vehicle);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteAsync(Vehicle vehicle)
        {
            _ = this.context.Vehicles.Remove(vehicle);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task SaveChangesAsync()
        {
            return this.context.SaveChangesAsync();
        }
    }
}