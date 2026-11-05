using System.ComponentModel.DataAnnotations;

namespace RepairRequests.Core.Models;

/// <summary>Учётная запись пользователя. Пароль хранится только в виде PBKDF2-хэша.</summary>
public class User
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Укажите ФИО")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "ФИО должно содержать от 3 до 120 символов")]
    [Display(Name = "ФИО")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите адрес электронной почты")]
    [EmailAddress(ErrorMessage = "Некорректный адрес электронной почты")]
    [StringLength(160)]
    [Display(Name = "Электронная почта")]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Display(Name = "Роль")]
    public UserRole Role { get; set; } = UserRole.Client;

    [StringLength(30)]
    [Display(Name = "Телефон")]
    public string? Phone { get; set; }

    [Display(Name = "Заблокирован")]
    public bool IsBlocked { get; set; }

    [Display(Name = "Дата регистрации")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Display(Name = "Последний вход")]
    public DateTime? LastLoginAt { get; set; }

    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    public ICollection<RepairRequest> Requests { get; set; } = new List<RepairRequest>();
    public ICollection<RepairRequest> AssignedRequests { get; set; } = new List<RepairRequest>();
}
