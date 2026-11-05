using System.ComponentModel.DataAnnotations;

namespace RepairRequests.Core.Models;

/// <summary>Проверка моделей по атрибутам DataAnnotations — используется формами WinForms.</summary>
public static class DataValidator
{
    public static IReadOnlyList<string> Validate(object model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model);
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results.Select(r => r.ErrorMessage ?? "Некорректное значение").ToList();
    }

    public static bool IsValid(object model) => Validate(model).Count == 0;
}
