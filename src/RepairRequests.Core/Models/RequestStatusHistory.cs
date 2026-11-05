namespace RepairRequests.Core.Models;

/// <summary>История изменения статусов заявки (аудит жизненного цикла).</summary>
public class RequestStatusHistory
{
    public int Id { get; set; }

    public int RequestId { get; set; }
    public RepairRequest? Request { get; set; }

    public RequestStatus Status { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.Now;

    public int? ChangedById { get; set; }
    public User? ChangedBy { get; set; }

    public string? Comment { get; set; }
}
