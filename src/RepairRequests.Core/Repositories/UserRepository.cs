using Microsoft.EntityFrameworkCore;
using RepairRequests.Core.Data;
using RepairRequests.Core.Models;

namespace RepairRequests.Core.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public UserRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<PagedResult<User>> GetPagedAsync(UserFilter filter)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var query = db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(u => u.FullName.Contains(term)
                                     || u.Email.Contains(term)
                                     || (u.Phone != null && u.Phone.Contains(term)));
        }

        if (filter.Role.HasValue)
            query = query.Where(u => u.Role == filter.Role.Value);

        var total = await query.CountAsync();
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 5, 200);

        var items = await query
            .OrderBy(u => u.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<User>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IReadOnlyList<User>> GetMechanicsAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Users.AsNoTracking()
            .Where(u => (u.Role == UserRole.Manager || u.Role == UserRole.Administrator) && !u.IsBlocked)
            .OrderBy(u => u.FullName)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<User>> GetClientsAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Users.AsNoTracking()
            .Where(u => u.Role == UserRole.Client && !u.IsBlocked)
            .OrderBy(u => u.FullName)
            .ToListAsync();
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var normalized = email.Trim().ToLowerInvariant();
        return await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email.ToLower() == normalized);
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task AddAsync(User user)
    {
        await using var db = await _factory.CreateDbContextAsync();
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var vehicles = user.Vehicles;
        var requests = user.Requests;
        var assigned = user.AssignedRequests;
        user.Vehicles = new List<Vehicle>();
        user.Requests = new List<RepairRequest>();
        user.AssignedRequests = new List<RepairRequest>();

        db.Users.Attach(user);
        db.Entry(user).State = EntityState.Modified;
        await db.SaveChangesAsync();

        user.Vehicles = vehicles;
        user.Requests = requests;
        user.AssignedRequests = assigned;
    }

    public async Task<bool> EmailExistsAsync(string email, int? excludeId = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var normalized = email.Trim().ToLowerInvariant();
        return await db.Users.AnyAsync(u => u.Email.ToLower() == normalized
                                            && (!excludeId.HasValue || u.Id != excludeId.Value));
    }

    public async Task<int> CountAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Users.CountAsync();
    }
}
