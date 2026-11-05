using System.ComponentModel.DataAnnotations;

namespace RepairRequests.Core.Models;

/// <summary>Транспортное средство клиента — элемент справочника (модуль А).</summary>
public class Vehicle
{
    public int Id { get; set; }

    [Display(Name = "Владелец")]
    public int OwnerId { get; set; }
    public User? Owner { get; set; }

    [Required(ErrorMessage = "Укажите марку")]
    [StringLength(60, MinimumLength = 2, ErrorMessage = "Марка должна содержать от 2 до 60 символов")]
    [Display(Name = "Марка")]
    public string Brand { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите модель")]
    [StringLength(60, MinimumLength = 1, ErrorMessage = "Модель должна содержать от 1 до 60 символов")]
    [Display(Name = "Модель")]
    public string Model { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите государственный номер")]
    [RegularExpression(@"^[АВЕКМНОРСТУХA-Z]\d{3}[АВЕКМНОРСТУХA-Z]{2}\d{2,3}$",
        ErrorMessage = "Формат госномера: А123ВС77")]
    [StringLength(12)]
    [Display(Name = "Государственный номер")]
    public string LicensePlate { get; set; } = string.Empty;

    [StringLength(17, MinimumLength = 17, ErrorMessage = "VIN должен содержать 17 символов")]
    [RegularExpression(@"^[A-HJ-NPR-Z0-9]{17}$", ErrorMessage = "VIN содержит недопустимые символы")]
    [Display(Name = "VIN")]
    public string? Vin { get; set; }

    [Range(1950, 2100, ErrorMessage = "Год выпуска должен быть в диапазоне 1950–2100")]
    [Display(Name = "Год выпуска")]
    public int Year { get; set; } = DateTime.Now.Year;

    [Range(0, 5_000_000, ErrorMessage = "Пробег должен быть неотрицательным")]
    [Display(Name = "Пробег, км")]
    public int Mileage { get; set; }

    [Display(Name = "Дата добавления")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RepairRequest> Requests { get; set; } = new List<RepairRequest>();

    public string DisplayName => $"{Brand} {Model} ({LicensePlate})";
}
