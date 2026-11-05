using Microsoft.EntityFrameworkCore;
using RepairRequests.Core.Data;
using RepairRequests.Core.Models;

namespace RepairRequests.Core.Repositories;

public class FaultTypeRepository : IFaultTypeRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public FaultTypeRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<PagedResult<FaultType>> GetPagedAsync(FaultTypeFilter filter)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var query = db.FaultTypes.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(f => f.Name.Contains(term) || f.Category.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(filter.Category))
            query = query.Where(f => f.Category == filter.Category);

        if (!filter.IncludeInactive)
            query = query.Where(f => f.IsActive);

        var total = await query.CountAsync();
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 5, 200);

        var items = await query
            .OrderBy(f => f.Category).ThenBy(f => f.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<FaultType>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IReadOnlyList<FaultType>> GetActiveAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.FaultTypes.AsNoTracking()
            .Where(f => f.IsActive)
            .OrderBy(f => f.Category).ThenBy(f => f.Name)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<string>> GetCategoriesAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.FaultTypes.AsNoTracking()
            .Select(f => f.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    public async Task<FaultType?> GetByIdAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.FaultTypes.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task AddAsync(FaultType faultType)
    {
        await using var db = await _factory.CreateDbContextAsync();
        await db.FaultTypes.AddAsync(faultType);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(FaultType faultType)
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.FaultTypes.Attach(faultType);
        db.Entry(faultType).State = EntityState.Modified;
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(FaultType faultType)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var entity = await db.FaultTypes.FirstOrDefaultAsync(f => f.Id == faultType.Id);
        if (entity is null)
            return;

        db.FaultTypes.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task<bool> HasRequestsAsync(int faultTypeId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.RepairRequests.AnyAsync(r => r.FaultTypeId == faultTypeId);
    }

    public async Task<bool> NameExistsAsync(string name, int? excludeId = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var normalized = name.Trim();
        return await db.FaultTypes.AnyAsync(f => f.Name == normalized
                                                 && (!excludeId.HasValue || f.Id != excludeId.Value));
    }
}
