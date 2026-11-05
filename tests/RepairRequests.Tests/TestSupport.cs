using Microsoft.EntityFrameworkCore;
using RepairRequests.Core.Data;
using RepairRequests.Core.Models;
using RepairRequests.Core.Services;

namespace RepairRequests.Tests;

/// <summary>
/// Фабрика контекста для тестов: отдельный файл SQLite во временной папке,
/// который удаляется после завершения теста.
/// </summary>
internal sealed class TestDbFactory : IDbContextFactory<AppDbContext>, IDisposable
{
    private readonly string _path;
    private readonly DbContextOptions<AppDbContext> _options;

    public TestDbFactory()
    {
        _path = Path.Combine(Path.GetTempPath(), $"repair-requests-tests-{Guid.NewGuid():N}.db");
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_path}")
            .Options;
    }

    public AppDbContext CreateDbContext() => new(_options);

    public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(CreateDbContext());

    /// <summary>Создаёт схему и заполняет базу демонстрационными данными.</summary>
    public static async Task<TestDbFactory> CreateSeededAsync()
    {
        var factory = new TestDbFactory();
        await using var db = factory.CreateDbContext();
        await db.Database.EnsureCreatedAsync();
        await DbSeeder.SeedAsync(db, new PasswordHasher());
        return factory;
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try
        {
            if (File.Exists(_path))
                File.Delete(_path);
        }
        catch (IOException)
        {
            // Временный файл будет удалён средствами ОС.
        }
    }
}

/// <summary>Фабрика готовых доменных объектов для модульных тестов аналитики.</summary>
internal static class Sample
{
    public static RepairRequest Request(
        int id,
        RequestStatus status,
        DateTime createdAt,
        DateTime? startedAt = null,
        DateTime? completedAt = null,
        decimal labor = 1000m,
        decimal parts = 500m,
        string category = "Двигатель",
        string vehicle = "Lada Vesta (А123ВС77)",
        RequestPriority priority = RequestPriority.Normal,
        DateTime? planned = null)
    {
        var request = new RepairRequest
        {
            Id = id,
            Number = $"ЗР-2026-{id:D4}",
            Description = "Описание неисправности для модульного теста",
            Status = status,
            Priority = priority,
            CreatedAt = createdAt,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            ClosedAt = status == RequestStatus.Closed ? completedAt : null,
            LaborCost = labor,
            PartsCost = parts,
            PlannedCompletion = planned,
            VehicleId = id,
            FaultTypeId = id,
            ClientId = 1
        };

        if (vehicle.Length > 0)
        {
            request.Vehicle = new Vehicle
            {
                Id = id,
                Brand = vehicle.Split(' ')[0],
                Model = "Vesta",
                LicensePlate = $"А{id:D3}ВС77",
                OwnerId = 1
            };
        }

        if (category.Length > 0)
        {
            request.FaultType = new FaultType
            {
                Id = id,
                Name = $"Неисправность {id}",
                Category = category
            };
        }

        return request;
    }
}
