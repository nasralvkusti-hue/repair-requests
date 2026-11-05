namespace RepairRequests.Core.Models;

public static class RequestStatusExtensions
{
    /// <summary>Допустимые переходы жизненного цикла заявки.</summary>
    public static IReadOnlyList<RequestStatus> NextStatuses(this RequestStatus status) => status switch
    {
        RequestStatus.New => new[] { RequestStatus.Accepted, RequestStatus.Cancelled },
        RequestStatus.Accepted => new[] { RequestStatus.InProgress, RequestStatus.Cancelled },
        RequestStatus.InProgress => new[] { RequestStatus.Completed, RequestStatus.Cancelled },
        RequestStatus.Completed => new[] { RequestStatus.Closed },
        _ => Array.Empty<RequestStatus>()
    };

    public static bool CanTransitionTo(this RequestStatus from, RequestStatus to) =>
        from.NextStatuses().Contains(to);
}
