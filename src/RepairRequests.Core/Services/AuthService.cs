using RepairRequests.Core.Models;
using RepairRequests.Core.Repositories;

namespace RepairRequests.Core.Services;

public sealed record AuthResult(bool Success, User? User, string? Error)
{
    public static AuthResult Ok(User user) => new(true, user, null);
    public static AuthResult Fail(string error) => new(false, null, error);
}

/// <summary>Аутентификация и регистрация клиентов (модуль авторизации).</summary>
public class AuthService
{
    private readonly IUserRepository _users;
    private readonly PasswordHasher _hasher;

    public AuthService(IUserRepository users, PasswordHasher hasher)
    {
        _users = users;
        _hasher = hasher;
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return AuthResult.Fail("Введите электронную почту и пароль.");

        var user = await _users.GetByEmailAsync(email);
        if (user is null || !_hasher.Verify(password, user.PasswordHash))
            return AuthResult.Fail("Неверная электронная почта или пароль.");

        if (user.IsBlocked)
            return AuthResult.Fail("Учётная запись заблокирована. Обратитесь к администратору.");

        user.LastLoginAt = DateTime.Now;
        await _users.UpdateAsync(user);
        return AuthResult.Ok(user);
    }

    public async Task<AuthResult> RegisterAsync(
        string fullName, string email, string? phone, string password, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Trim().Length < 3)
            return AuthResult.Fail("Укажите ФИО (не менее 3 символов).");

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@') || !email.Contains('.'))
            return AuthResult.Fail("Укажите корректный адрес электронной почты.");

        if (password.Length < 8)
            return AuthResult.Fail("Пароль должен содержать не менее 8 символов.");

        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            return AuthResult.Fail("Пароль должен содержать буквы и цифры.");

        if (password != confirmPassword)
            return AuthResult.Fail("Пароли не совпадают.");

        if (await _users.EmailExistsAsync(email))
            return AuthResult.Fail("Пользователь с такой почтой уже зарегистрирован.");

        var user = new User
        {
            FullName = fullName.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            PasswordHash = _hasher.Hash(password),
            Role = UserRole.Client,
            CreatedAt = DateTime.Now
        };

        await _users.AddAsync(user);
        return AuthResult.Ok(user);
    }
}
