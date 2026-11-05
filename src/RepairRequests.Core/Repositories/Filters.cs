using RepairRequests.Core.Models;

namespace RepairRequests.Core.Repositories;

/// <summary>Параметры фильтрации, сортировки и постраничного вывода заявок.</summary>
public sealed class RequestFilter
{
    public string? Search { get; set; }
    public RequestStatus? Status { get; set; }
    public RequestPriority? Priority { get; set; }
    public int? VehicleId { get; set; }
    public int? FaultTypeId { get; set; }
    public int? ClientId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string SortBy { get; set; } = "date_desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class VehicleFilter
{
    public string? Search { get; set; }
    public int? OwnerId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class FaultTypeFilter
{
    public string? Search { get; set; }
    public string? Category { get; set; }
    public bool IncludeInactive { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class UserFilter
{
    public string? Search { get; set; }
    public UserRole? Role { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>Результат постраничной выборки.</summary>
public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }

    public int TotalPages => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}

/// <summary>Показатели для главного экрана (дашборда).</summary>
public sealed record DashboardStats(
    int TotalRequests,
    int OpenRequests,
    int InProgress,
    int Overdue,
    int CompletedThisMonth,
    double AverageRepairHours,
    decimal MonthRevenue);
