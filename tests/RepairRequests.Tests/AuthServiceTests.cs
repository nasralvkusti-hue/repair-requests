using RepairRequests.Core.Models;
using RepairRequests.Core.Repositories;
using RepairRequests.Core.Services;

namespace RepairRequests.Tests;

public class AuthServiceTests
{
    private readonly InMemoryUserRepository _users = new();
    private readonly PasswordHasher _hasher = new();
    private readonly AuthService _auth;

    public AuthServiceTests()
    {
        _auth = new AuthService(_users, _hasher);
        _users.Seed(new User
        {
            FullName = "Иванов Пётр Андреевич",
            Email = "manager@remont.ru",
            PasswordHash = _hasher.Hash("Manager123!"),
            Role = UserRole.Manager
        });
        _users.Seed(new User
        {
            FullName = "Заблокированный Пользователь",
            Email = "blocked@remont.ru",
            PasswordHash = _hasher.Hash("Blocked123!"),
            Role = UserRole.Client,
            IsBlocked = true
        });
    }

    [Fact]
    public async Task Login_Succeeds_ForValidCredentials()
    {
        var result = await _auth.LoginAsync("manager@remont.ru", "Manager123!");

        Assert.True(result.Success);
        Assert.NotNull(result.User);
        Assert.Equal(UserRole.Manager, result.User!.Role);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task Login_UpdatesLastLoginAt()
    {
        var result = await _auth.LoginAsync("manager@remont.ru", "Manager123!");

        Assert.NotNull(result.User!.LastLoginAt);
        var stored = _users.Users.Single(u => u.Email == "manager@remont.ru");
        Assert.NotNull(stored.LastLoginAt);
    }

    [Fact]
    public async Task Login_IsCaseInsensitiveByEmail()
    {
        var result = await _auth.LoginAsync("MANAGER@remont.ru", "Manager123!");

        Assert.True(result.Success);
    }

    [Fact]
    public async Task Login_Fails_ForWrongPassword()
    {
        var result = await _auth.LoginAsync("manager@remont.ru", "Wrong123!");

        Assert.False(result.Success);
        Assert.Null(result.User);
        Assert.Contains("Неверная", result.Error);
    }

    [Fact]
    public async Task Login_Fails_ForUnknownEmail()
    {
        var result = await _auth.LoginAsync("nobody@remont.ru", "Manager123!");

        Assert.False(result.Success);
        Assert.Null(result.User);
    }

    [Fact]
    public async Task Login_Fails_ForBlockedAccount()
    {
        var result = await _auth.LoginAsync("blocked@remont.ru", "Blocked123!");

        Assert.False(result.Success);
        Assert.Contains("заблокирована", result.Error);
    }

    [Theory]
    [InlineData("", "Manager123!")]
    [InlineData("manager@remont.ru", "")]
    [InlineData("   ", "   ")]
    public async Task Login_Fails_ForBlankInput(string email, string password)
    {
        var result = await _auth.LoginAsync(email, password);

        Assert.False(result.Success);
        Assert.Contains("Введите", result.Error);
    }

    [Fact]
    public async Task Register_CreatesClientWithHashedPassword()
    {
        var result = await _auth.RegisterAsync(
            "Кузнецов Алексей", " New.Client@Example.COM ", "+7 (900) 000-00-00",
            "Client123!", "Client123!");

        Assert.True(result.Success);
        var user = result.User!;
        Assert.Equal(UserRole.Client, user.Role);
        Assert.Equal("new.client@example.com", user.Email);
        Assert.NotEqual("Client123!", user.PasswordHash);
        Assert.True(_hasher.Verify("Client123!", user.PasswordHash));
        Assert.Equal("+7 (900) 000-00-00", user.Phone);
    }

    [Fact]
    public async Task Register_TrimsFullName()
    {
        var result = await _auth.RegisterAsync(
            "  Петров Пётр  ", "petrov@example.com", null, "Passw0rd!", "Passw0rd!");

        Assert.True(result.Success);
        Assert.Equal("Петров Пётр", result.User!.FullName);
        Assert.Null(result.User.Phone);
    }

    [Theory]
    [InlineData("Ив", "user@example.com", "Passw0rd!", "Passw0rd!", "ФИО")]
    [InlineData("Пользователь", "bad-email", "Passw0rd!", "Passw0rd!", "почты")]
    [InlineData("Пользователь", "user@example.com", "short", "short", "8 символов")]
    [InlineData("Пользователь", "user@example.com", "onlyletters", "onlyletters", "буквы и цифры")]
    [InlineData("Пользователь", "user@example.com", "Passw0rd!", "Other123!", "не совпадают")]
    public async Task Register_RejectsInvalidData(
        string fullName, string email, string password, string confirm, string expectedError)
    {
        var result = await _auth.RegisterAsync(fullName, email, null, password, confirm);

        Assert.False(result.Success);
        Assert.Contains(expectedError, result.Error);
        Assert.DoesNotContain(_users.Users, u => u.Email == email.Trim().ToLowerInvariant());
    }

    [Fact]
    public async Task Register_RejectsDuplicateEmail()
    {
        var result = await _auth.RegisterAsync(
            "Другой Пользователь", "Manager@Remont.RU", null, "Passw0rd!", "Passw0rd!");

        Assert.False(result.Success);
        Assert.Contains("уже зарегистрирован", result.Error);
    }

    private sealed class InMemoryUserRepository : IUserRepository
    {
        private int _nextId = 1;

        public List<User> Users { get; } = new();

        public void Seed(User user)
        {
            user.Id = _nextId++;
            Users.Add(user);
        }

        public Task<PagedResult<User>> GetPagedAsync(UserFilter filter)
        {
            var query = Users.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(filter.Search))
                query = query.Where(u => u.FullName.Contains(filter.Search, StringComparison.OrdinalIgnoreCase)
                                         || u.Email.Contains(filter.Search, StringComparison.OrdinalIgnoreCase));
            if (filter.Role.HasValue)
                query = query.Where(u => u.Role == filter.Role.Value);

            var items = query.OrderBy(u => u.FullName).ToList();
            return Task.FromResult(new PagedResult<User>
            {
                Items = items,
                TotalCount = items.Count,
                Page = 1,
                PageSize = items.Count
            });
        }

        public Task<IReadOnlyList<User>> GetMechanicsAsync() =>
            Task.FromResult<IReadOnlyList<User>>(Users
                .Where(u => u.Role is UserRole.Manager or UserRole.Administrator && !u.IsBlocked)
                .ToList());

        public Task<IReadOnlyList<User>> GetClientsAsync() =>
            Task.FromResult<IReadOnlyList<User>>(Users.Where(u => u.Role == UserRole.Client).ToList());

        public Task<User?> GetByEmailAsync(string email) =>
            Task.FromResult(Users.FirstOrDefault(u =>
                string.Equals(u.Email, email.Trim(), StringComparison.OrdinalIgnoreCase)));

        public Task<User?> GetByIdAsync(int id) => Task.FromResult(Users.FirstOrDefault(u => u.Id == id));

        public Task AddAsync(User user)
        {
            user.Id = _nextId++;
            Users.Add(user);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(User user)
        {
            var index = Users.FindIndex(u => u.Id == user.Id);
            if (index >= 0)
                Users[index] = user;
            return Task.CompletedTask;
        }

        public Task<bool> EmailExistsAsync(string email, int? excludeId = null) =>
            Task.FromResult(Users.Any(u =>
                string.Equals(u.Email, email.Trim(), StringComparison.OrdinalIgnoreCase)
                && (!excludeId.HasValue || u.Id != excludeId.Value)));

        public Task<int> CountAsync() => Task.FromResult(Users.Count);
    }
}
