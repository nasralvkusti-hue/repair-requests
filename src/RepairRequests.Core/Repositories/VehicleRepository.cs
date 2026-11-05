using Microsoft.EntityFrameworkCore;
using RepairRequests.Core.Data;
using RepairRequests.Core.Models;

namespace RepairRequests.Core.Repositories;

public class VehicleRepository : IVehicleRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public VehicleRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<PagedResult<Vehicle>> GetPagedAsync(VehicleFilter filter)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var query = db.Vehicles
            .AsNoTracking()
            .Include(v => v.Owner)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(v =>
                v.LicensePlate.Contains(term) ||
                v.Brand.Contains(term) ||
                v.Model.Contains(term) ||
                (v.Owner != null && v.Owner.FullName.Contains(term)));
        }

        if (filter.OwnerId.HasValue)
            query = query.Where(v => v.OwnerId == filter.OwnerId.Value);

        var total = await query.CountAsync();
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 5, 200);

        var items = await query
            .OrderBy(v => v.Brand).ThenBy(v => v.Model)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Vehicle>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IReadOnlyList<Vehicle>> GetAllAsync(int? ownerId = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var query = db.Vehicles.AsNoTracking().Include(v => v.Owner).AsQueryable();
        if (ownerId.HasValue)
            query = query.Where(v => v.OwnerId == ownerId.Value);

        return await query.OrderBy(v => v.Brand).ThenBy(v => v.Model).ToListAsync();
    }

    public async Task<Vehicle?> GetByIdAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Vehicles.AsNoTracking().Include(v => v.Owner).FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task AddAsync(Vehicle vehicle)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var owner = vehicle.Owner;
        vehicle.Owner = null;
        await db.Vehicles.AddAsync(vehicle);
        await db.SaveChangesAsync();
        vehicle.Owner = owner;
    }

    public async Task UpdateAsync(Vehicle vehicle)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var owner = vehicle.Owner;
        vehicle.Owner = null;
        db.Vehicles.Attach(vehicle);
        db.Entry(vehicle).State = EntityState.Modified;
        await db.SaveChangesAsync();
        vehicle.Owner = owner;
    }

    public async Task DeleteAsync(Vehicle vehicle)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var entity = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicle.Id);
        if (entity is null)
            return;

        db.Vehicles.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task<bool> HasRequestsAsync(int vehicleId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.RepairRequests.AnyAsync(r => r.VehicleId == vehicleId);
    }

    public async Task<bool> LicensePlateExistsAsync(string licensePlate, int? excludeId = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var normalized = licensePlate.Trim().ToUpperInvariant();
        return await db.Vehicles.AnyAsync(v => v.LicensePlate == normalized
                                               && (!excludeId.HasValue || v.Id != excludeId.Value));
    }
}
