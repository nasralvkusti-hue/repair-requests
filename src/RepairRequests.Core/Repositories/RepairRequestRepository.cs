using Microsoft.EntityFrameworkCore;
using RepairRequests.Core.Data;
using RepairRequests.Core.Models;

namespace RepairRequests.Core.Repositories;

public class RepairRequestRepository : IRepairRequestRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public RepairRequestRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<PagedResult<RepairRequest>> GetPagedAsync(RequestFilter filter)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var query = BuildQuery(db, filter);

        var total = await query.CountAsync();
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 5, 200);

        query = ApplySorting(query, filter.SortBy);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<RepairRequest>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    private static IQueryable<RepairRequest> BuildQuery(AppDbContext db, RequestFilter filter)
    {
        var query = db.RepairRequests
            .AsNoTracking()
            .Include(r => r.Vehicle)
            .Include(r => r.FaultType)
            .Include(r => r.Client)
            .Include(r => r.AssignedMechanic)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(r =>
                r.Number.Contains(term) ||
                r.Description.Contains(term) ||
                (r.Vehicle != null && (r.Vehicle.LicensePlate.Contains(term) || r.Vehicle.Brand.Contains(term) || r.Vehicle.Model.Contains(term))) ||
                (r.Client != null && r.Client.FullName.Contains(term)) ||
                (r.FaultType != null && r.FaultType.Name.Contains(term)));
        }

        if (filter.Status.HasValue)
            query = query.Where(r => r.Status == filter.Status.Value);

        if (filter.Priority.HasValue)
            query = query.Where(r => r.Priority == filter.Priority.Value);

        if (filter.VehicleId.HasValue)
            query = query.Where(r => r.VehicleId == filter.VehicleId.Value);

        if (filter.FaultTypeId.HasValue)
            query = query.Where(r => r.FaultTypeId == filter.FaultTypeId.Value);

        if (filter.ClientId.HasValue)
            query = query.Where(r => r.ClientId == filter.ClientId.Value);

        if (filter.DateFrom.HasValue)
        {
            var from = filter.DateFrom.Value.Date;
            query = query.Where(r => r.CreatedAt >= from);
        }

        if (filter.DateTo.HasValue)
        {
            var to = filter.DateTo.Value.Date.AddDays(1);
            query = query.Where(r => r.CreatedAt < to);
        }

        return query;
    }

    private static IQueryable<RepairRequest> ApplySorting(IQueryable<RepairRequest> query, string sortBy) =>
        sortBy switch
        {
            "date_asc" => query.OrderBy(r => r.CreatedAt).ThenBy(r => r.Id),
            "number" => query.OrderBy(r => r.Number),
            "priority" => query
                .OrderByDescending(r => r.Priority == RequestPriority.Critical ? 4
                    : r.Priority == RequestPriority.High ? 3
                    : r.Priority == RequestPriority.Normal ? 2 : 1)
                .ThenByDescending(r => r.CreatedAt),
            "status" => query
                .OrderBy(r => r.Status == RequestStatus.New ? 1
                    : r.Status == RequestStatus.Accepted ? 2
                    : r.Status == RequestStatus.InProgress ? 3
                    : r.Status == RequestStatus.Completed ? 4
                    : r.Status == RequestStatus.Closed ? 5 : 6)
                .ThenByDescending(r => r.CreatedAt),
            "cost" => query.OrderByDescending(r => r.LaborCost + r.PartsCost),
            _ => query.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
        };

    public async Task<RepairRequest?> GetByIdAsync(int id, bool asTracking = false)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var query = db.RepairRequests
            .Include(r => r.Vehicle).ThenInclude(v => v!.Owner)
            .Include(r => r.FaultType)
            .Include(r => r.Client)
            .Include(r => r.AssignedMechanic)
            .Include(r => r.History).ThenInclude(h => h.ChangedBy)
            .AsQueryable();

        if (!asTracking)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IReadOnlyList<RepairRequest>> GetRecentAsync(int count, int? clientId = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var query = db.RepairRequests
            .AsNoTracking()
            .Include(r => r.Vehicle)
            .Include(r => r.FaultType)
            .Include(r => r.Client)
            .Include(r => r.AssignedMechanic)
            .AsQueryable();

        if (clientId.HasValue)
            query = query.Where(r => r.ClientId == clientId.Value);

        return await query.OrderByDescending(r => r.CreatedAt).Take(count).ToListAsync();
    }

    public async Task<IReadOnlyList<RepairRequest>> GetForPeriodAsync(DateTime from, DateTime to, int? clientId = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var start = from.Date;
        var end = to.Date.AddDays(1);

        var query = db.RepairRequests
            .AsNoTracking()
            .Include(r => r.Vehicle)
            .Include(r => r.FaultType)
            .Include(r => r.Client)
            .Include(r => r.AssignedMechanic)
            .Where(r => r.CreatedAt >= start && r.CreatedAt < end);

        if (clientId.HasValue)
            query = query.Where(r => r.ClientId == clientId.Value);

        return await query.OrderBy(r => r.CreatedAt).ToListAsync();
    }

    public async Task<IReadOnlyList<RepairRequest>> GetCompletedInPeriodAsync(DateTime from, DateTime to, int? clientId = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var start = from.Date;
        var end = to.Date.AddDays(1);

        var query = db.RepairRequests
            .AsNoTracking()
            .Include(r => r.Vehicle)
            .Include(r => r.FaultType)
            .Include(r => r.Client)
            .Include(r => r.AssignedMechanic)
            .Where(r => r.CompletedAt >= start && r.CompletedAt < end
                        && (r.Status == RequestStatus.Completed || r.Status == RequestStatus.Closed));

        if (clientId.HasValue)
            query = query.Where(r => r.ClientId == clientId.Value);

        return await query.OrderBy(r => r.CompletedAt).ToListAsync();
    }

    public async Task<DashboardStats> GetDashboardStatsAsync(int? clientId = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var query = db.RepairRequests.AsNoTracking().AsQueryable();

        if (clientId.HasValue)
            query = query.Where(r => r.ClientId == clientId.Value);

        var total = await query.CountAsync();
        var open = await query.CountAsync(r => r.Status == RequestStatus.New || r.Status == RequestStatus.Accepted);
        var inProgress = await query.CountAsync(r => r.Status == RequestStatus.InProgress);
        var today = DateTime.Today;
        var overdue = await query.CountAsync(r => r.PlannedCompletion != null
                                                   && r.PlannedCompletion < today
                                                   && (r.Status == RequestStatus.New || r.Status == RequestStatus.Accepted || r.Status == RequestStatus.InProgress));
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var completedThisMonth = await query.CountAsync(r => r.CompletedAt >= monthStart
                                                             && (r.Status == RequestStatus.Completed || r.Status == RequestStatus.Closed));
        var revenue = await query
            .Where(r => r.CompletedAt >= monthStart && (r.Status == RequestStatus.Completed || r.Status == RequestStatus.Closed))
            .SumAsync(r => (decimal?)(r.LaborCost + r.PartsCost)) ?? 0m;

        var durations = await query
            .Where(r => r.StartedAt != null && r.CompletedAt != null)
            .Select(r => new { r.StartedAt, r.CompletedAt })
            .ToListAsync();

        var average = durations.Count == 0
            ? 0
            : durations.Average(d => (d.CompletedAt!.Value - d.StartedAt!.Value).TotalHours);

        return new DashboardStats(total, open, inProgress, overdue, completedThisMonth, Math.Round(average, 1), revenue);
    }

    public async Task<string> GenerateNumberAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        var year = DateTime.Now.Year;
        var prefix = $"ЗР-{year}-";

        var last = await db.RepairRequests
            .AsNoTracking()
            .Where(r => r.Number.StartsWith(prefix))
            .OrderByDescending(r => r.Number)
            .Select(r => r.Number)
            .FirstOrDefaultAsync();

        var next = 1;
        if (last is not null && int.TryParse(last[^4..], out var parsed))
            next = parsed + 1;

        return prefix + next.ToString("D4");
    }

    public async Task AddAsync(RepairRequest request)
    {
        await using var db = await _factory.CreateDbContextAsync();
        DetachNavigations(request);
        await db.RepairRequests.AddAsync(request);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(RepairRequest request)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var newHistory = request.History.Where(h => h.Id == 0).ToList();
        var existingHistory = request.History.Where(h => h.Id != 0).ToList();

        DetachNavigations(request);
        request.History = new List<RequestStatusHistory>();

        db.RepairRequests.Attach(request);
        db.Entry(request).State = EntityState.Modified;

        foreach (var history in existingHistory)
        {
            history.Request = null;
            history.ChangedBy = null;
            db.Entry(history).State = EntityState.Unchanged;
        }

        foreach (var history in newHistory)
        {
            history.Id = 0;
            history.RequestId = request.Id;
            history.Request = null;
            history.ChangedBy = null;
            db.RequestStatusHistory.Add(history);
        }

        await db.SaveChangesAsync();

        foreach (var history in newHistory)
            request.History.Add(history);
    }

    public async Task DeleteAsync(RepairRequest request)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var entity = await db.RepairRequests.FirstOrDefaultAsync(r => r.Id == request.Id);
        if (entity is null)
            return;

        db.RepairRequests.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task<bool> HasRequestsForVehicleAsync(int vehicleId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.RepairRequests.AnyAsync(r => r.VehicleId == vehicleId);
    }

    public async Task<bool> HasRequestsForFaultTypeAsync(int faultTypeId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.RepairRequests.AnyAsync(r => r.FaultTypeId == faultTypeId);
    }

    /// <summary>
    /// Отвязывает навигационные свойства, чтобы EF не пытался обновлять связанные
    /// справочники и пользователей вместе с заявкой.
    /// </summary>
    private static void DetachNavigations(RepairRequest request)
    {
        request.Vehicle = null;
        request.FaultType = null;
        request.Client = null;
        request.AssignedMechanic = null;
        foreach (var history in request.History)
        {
            history.Request = null;
            history.ChangedBy = null;
        }
    }
}
