using System.ComponentModel.DataAnnotations;

namespace RepairRequests.Core.Models;

/// <summary>Справочник неисправностей с нормативной трудоёмкостью (модуль А).</summary>
public class FaultType
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Укажите наименование неисправности")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "Наименование должно содержать от 3 до 120 символов")]
    [Display(Name = "Наименование")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите категорию")]
    [StringLength(60)]
    [Display(Name = "Категория")]
    public string Category { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Описание")]
    public string? Description { get; set; }

    [Range(0.1, 200, ErrorMessage = "Норматив должен быть в диапазоне 0,1–200 ч")]
    [Display(Name = "Норматив, ч")]
    public decimal NormHours { get; set; } = 1m;

    [Range(0, 1_000_000, ErrorMessage = "Стоимость работ должна быть неотрицательной")]
    [Display(Name = "Стоимость работ, ₽")]
    public decimal BasePrice { get; set; }

    [Display(Name = "Активна")]
    public bool IsActive { get; set; } = true;

    public ICollection<RepairRequest> Requests { get; set; } = new List<RepairRequest>();
}
