// <copyright file="IVehicleRepository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SmartAutoTrader.API.Repositories
{
    using System.Linq.Expressions;
    using Microsoft.EntityFrameworkCore;
    using SmartAutoTrader.API.Data;
    using SmartAutoTrader.API.Models;

    public interface IVehicleRepository
    {
        Task<Vehicle?> GetByIdAsync(int id);

        Task<List<Vehicle>> GetAllAsync();

        Task<List<Vehicle>> SearchAsync(Expression<Func<Vehicle, bool>> predicate);

        Task AddAsync(Vehicle vehicle);

        Task DeleteAsync(Vehicle vehicle);

        Task SaveChangesAsync();
    }

    public class VehicleRepository(ApplicationDbContext context) : IVehicleRepository
    {
        private readonly ApplicationDbContext context = context;

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