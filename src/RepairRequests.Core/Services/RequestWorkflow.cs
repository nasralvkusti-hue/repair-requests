using RepairRequests.Core.Models;

namespace RepairRequests.Core.Services;

/// <summary>
/// Бизнес-правила жизненного цикла заявки: допустимые переходы и отметки времени.
/// </summary>
public static class RequestWorkflow
{
    public static IReadOnlyList<RequestStatus> NextStatuses(RequestStatus status) => status.NextStatuses();

    public static bool CanTransition(RequestStatus from, RequestStatus to) => from.CanTransitionTo(to);

    /// <summary>Применяет переход, проставляет соответствующую отметку времени и пишет историю.</summary>
    public static void Apply(RepairRequest request, RequestStatus target, int changedById, string? comment = null)
    {
        if (!CanTransition(request.Status, target))
            throw new InvalidOperationException(
                $"Переход «{request.Status.Ru()}» → «{target.Ru()}» недопустим.");

        if (target == RequestStatus.Cancelled && string.IsNullOrWhiteSpace(comment))
            throw new InvalidOperationException("Для отмены заявки необходимо указать причину.");

        var now = DateTime.Now;
        request.Status = target;

        switch (target)
        {
            case RequestStatus.Accepted:
                request.AcceptedAt = now;
                break;
            case RequestStatus.InProgress:
                request.StartedAt = now;
                break;
            case RequestStatus.Completed:
                request.CompletedAt = now;
                break;
            case RequestStatus.Closed:
                request.ClosedAt = now;
                break;
            case RequestStatus.Cancelled:
                request.ClosedAt = now;
                break;
        }

        request.History.Add(new RequestStatusHistory
        {
            Status = target,
            ChangedAt = now,
            ChangedById = changedById,
            Comment = comment
        });
    }

    public static string ActionName(RequestStatus target) => target switch
    {
        RequestStatus.Accepted => "Принять заявку",
        RequestStatus.InProgress => "Начать работы",
        RequestStatus.Completed => "Завершить ремонт",
        RequestStatus.Closed => "Закрыть заявку",
        RequestStatus.Cancelled => "Отменить заявку",
        _ => target.Ru()
    };
}
