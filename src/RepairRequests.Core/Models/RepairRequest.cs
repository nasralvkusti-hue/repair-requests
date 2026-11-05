using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RepairRequests.Core.Models;

/// <summary>Заявка на ремонт — центральная сущность системы (модуль Б).</summary>
public class RepairRequest
{
    public int Id { get; set; }

    [Required]
    [StringLength(24)]
    [Display(Name = "Номер заявки")]
    public string Number { get; set; } = string.Empty;

    [Display(Name = "Транспортное средство")]
    public int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    [Display(Name = "Неисправность")]
    public int FaultTypeId { get; set; }
    public FaultType? FaultType { get; set; }

    [Display(Name = "Клиент")]
    public int ClientId { get; set; }
    public User? Client { get; set; }

    [Display(Name = "Механик")]
    public int? AssignedMechanicId { get; set; }
    public User? AssignedMechanic { get; set; }

    [Required(ErrorMessage = "Опишите неисправность")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "Описание должно содержать от 10 до 1000 символов")]
    [Display(Name = "Описание")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Приоритет")]
    public RequestPriority Priority { get; set; } = RequestPriority.Normal;

    [Display(Name = "Статус")]
    public RequestStatus Status { get; set; } = RequestStatus.New;

    [Display(Name = "Создана")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Display(Name = "Принята")]
    public DateTime? AcceptedAt { get; set; }

    [Display(Name = "Начата")]
    public DateTime? StartedAt { get; set; }

    [Display(Name = "Выполнена")]
    public DateTime? CompletedAt { get; set; }

    [Display(Name = "Закрыта")]
    public DateTime? ClosedAt { get; set; }

    [Display(Name = "Плановое завершение")]
    [DataType(DataType.Date)]
    public DateTime? PlannedCompletion { get; set; }

    [Range(0, 10_000_000, ErrorMessage = "Стоимость работ должна быть неотрицательной")]
    [Display(Name = "Стоимость работ, ₽")]
    public decimal LaborCost { get; set; }

    [Range(0, 10_000_000, ErrorMessage = "Стоимость запчастей должна быть неотрицательной")]
    [Display(Name = "Стоимость запчастей, ₽")]
    public decimal PartsCost { get; set; }

    [Range(0, 10_000_000)]
    [Display(Name = "Стоимость запчастей в валюте")]
    public decimal? PartsCostCurrency { get; set; }

    [StringLength(3)]
    [Display(Name = "Валюта")]
    public string? PartsCurrency { get; set; }

    [Display(Name = "Курс валюты")]
    public decimal? ExchangeRate { get; set; }

    [StringLength(1000)]
    [Display(Name = "Комментарий механика")]
    public string? Comment { get; set; }

    public ICollection<RequestStatusHistory> History { get; set; } = new List<RequestStatusHistory>();

    [NotMapped]
    public decimal TotalCost => LaborCost + PartsCost;

    /// <summary>Длительность непосредственно ремонта (от начала работ до выполнения), часы.</summary>
    [NotMapped]
    public double? RepairHours => StartedAt.HasValue && CompletedAt.HasValue
        ? Math.Max(0, (CompletedAt.Value - StartedAt.Value).TotalHours)
        : null;

    /// <summary>Ожидание в очереди до начала работ, часы.</summary>
    [NotMapped]
    public double? WaitingHours => StartedAt.HasValue
        ? Math.Max(0, (StartedAt.Value - CreatedAt).TotalHours)
        : null;

    /// <summary>Полный простой: от создания до завершения (или до текущего момента), часы.</summary>
    [NotMapped]
    public double TotalDowntimeHours
    {
        get
        {
            var end = CompletedAt ?? ClosedAt ?? DateTime.Now;
            return Math.Max(0, (end - CreatedAt).TotalHours);
        }
    }

    [NotMapped]
    public bool IsOverdue => PlannedCompletion.HasValue
        && Status.IsOpen()
        && PlannedCompletion.Value.Date < DateTime.Today;
}
