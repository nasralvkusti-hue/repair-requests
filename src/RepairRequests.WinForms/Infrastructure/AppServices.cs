using Microsoft.EntityFrameworkCore;
using RepairRequests.Core.Data;
using RepairRequests.Core.Models;
using RepairRequests.Core.Repositories;
using RepairRequests.Core.Services;

namespace RepairRequests.WinForms.Infrastructure;

/// <summary>Фабрика контекста базы данных SQLite.</summary>
public sealed class SqliteDbContextFactory : IDbContextFactory<AppDbContext>
{
    private readonly DbContextOptions<AppDbContext> _options;

    public SqliteDbContextFactory(string databasePath)
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;
    }

    public AppDbContext CreateDbContext() => new(_options);
}

/// <summary>
/// Точка доступа к сервисам и репозиториям приложения. Инициализируется один раз при запуске.
/// </summary>
public static class AppServices
{
    public static IUserRepository Users { get; private set; } = null!;
    public static IVehicleRepository Vehicles { get; private set; } = null!;
    public static IFaultTypeRepository FaultTypes { get; private set; } = null!;
    public static IRepairRequestRepository Requests { get; private set; } = null!;
    public static AuthService Auth { get; private set; } = null!;
    public static PasswordHasher Hasher { get; private set; } = null!;
    public static CurrencyService Currency { get; } = new();

    public static string DatabasePath { get; private set; } = string.Empty;

    public static User? CurrentUser { get; set; }

    public static bool IsAuthorized => CurrentUser is not null;
    public static bool IsManager => CurrentUser?.Role is UserRole.Manager or UserRole.Administrator;
    public static bool IsAdministrator => CurrentUser?.Role == UserRole.Administrator;
    public static int? ClientScope => CurrentUser?.Role == UserRole.Client ? CurrentUser.Id : null;

    public static void Initialize(IDbContextFactory<AppDbContext> factory, PasswordHasher hasher, string databasePath)
    {
        Hasher = hasher;
        DatabasePath = databasePath;

        Users = new UserRepository(factory);
        Vehicles = new VehicleRepository(factory);
        FaultTypes = new FaultTypeRepository(factory);
        Requests = new RepairRequestRepository(factory);
        Auth = new AuthService(Users, hasher);
    }

    public static void Logout()
    {
        CurrentUser = null;
    }
}
