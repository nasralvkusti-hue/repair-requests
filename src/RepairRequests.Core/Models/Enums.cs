namespace RepairRequests.Core.Models;

/// <summary>Роли пользователей системы (гость — неавторизованный посетитель).</summary>
public enum UserRole
{
    Client = 1,
    Manager = 2,
    Administrator = 3
}

/// <summary>Жизненный цикл заявки на ремонт.</summary>
public enum RequestStatus
{
    New = 1,
    Accepted = 2,
    InProgress = 3,
    Completed = 4,
    Closed = 5,
    Cancelled = 6
}

/// <summary>Приоритет заявки влияет на порядок обработки.</summary>
public enum RequestPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Critical = 4
}

public static class EnumDisplay
{
    public static string Ru(this UserRole role) => role switch
    {
        UserRole.Client => "Клиент",
        UserRole.Manager => "Менеджер",
        UserRole.Administrator => "Администратор",
        _ => role.ToString()
    };

    public static string Ru(this RequestStatus status) => status switch
    {
        RequestStatus.New => "Новая",
        RequestStatus.Accepted => "Принята",
        RequestStatus.InProgress => "В работе",
        RequestStatus.Completed => "Выполнена",
        RequestStatus.Closed => "Закрыта",
        RequestStatus.Cancelled => "Отменена",
        _ => status.ToString()
    };

    public static string Ru(this RequestPriority priority) => priority switch
    {
        RequestPriority.Low => "Низкий",
        RequestPriority.Normal => "Обычный",
        RequestPriority.High => "Высокий",
        RequestPriority.Critical => "Критический",
        _ => priority.ToString()
    };

    public static string Badge(this RequestStatus status) => status switch
    {
        RequestStatus.New => "badge-status badge-new",
        RequestStatus.Accepted => "badge-status badge-accepted",
        RequestStatus.InProgress => "badge-status badge-progress",
        RequestStatus.Completed => "badge-status badge-done",
        RequestStatus.Closed => "badge-status badge-closed",
        RequestStatus.Cancelled => "badge-status badge-cancelled",
        _ => "badge-status"
    };

    public static string Badge(this RequestPriority priority) => priority switch
    {
        RequestPriority.Low => "badge-priority badge-low",
        RequestPriority.Normal => "badge-priority badge-normal",
        RequestPriority.High => "badge-priority badge-high",
        RequestPriority.Critical => "badge-priority badge-critical",
        _ => "badge-priority"
    };

    /// <summary>Статусы, при которых заявка считается незавершённой.</summary>
    public static bool IsOpen(this RequestStatus status) =>
        status is RequestStatus.New or RequestStatus.Accepted or RequestStatus.InProgress;
}
