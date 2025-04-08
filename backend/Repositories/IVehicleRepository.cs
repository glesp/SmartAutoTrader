using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SmartAutoTrader.API.Data;
using SmartAutoTrader.API.Models;

namespace SmartAutoTrader.API.Repositories;

public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(int id);
    Task<List<Vehicle>> GetAllAsync();
    Task<List<Vehicle>> SearchAsync(Expression<Func<Vehicle, bool>> predicate);
    Task AddAsync(Vehicle vehicle);
    Task DeleteAsync(Vehicle vehicle);
    Task SaveChangesAsync();
}


public class VehicleRepository : IVehicleRepository
{
    private readonly ApplicationDbContext _context;

    public VehicleRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Vehicle?> GetByIdAsync(int id)
        => _context.Vehicles
            .Include(v => v.Images)
            .Include(v => v.Features)
            .FirstOrDefaultAsync(v => v.Id == id);

    public Task<List<Vehicle>> GetAllAsync()
        => _context.Vehicles
            .Include(v => v.Images)
            .Include(v => v.Features)
            .ToListAsync();

    public Task<List<Vehicle>> SearchAsync(Expression<Func<Vehicle, bool>> predicate)
    {
        return _context.Vehicles
            .Include(v => v.Images)
            .Include(v => v.Features)
            .Where(predicate)
            .ToListAsync();
    }

    public Task AddAsync(Vehicle vehicle)
    {
        _context.Vehicles.Add(vehicle);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Vehicle vehicle)
    {
        _context.Vehicles.Remove(vehicle);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync()
        => _context.SaveChangesAsync();
}