using Microsoft.EntityFrameworkCore;
using RepairRequests.Core.Data;
using RepairRequests.Core.Models;
using RepairRequests.Core.Repositories;
using RepairRequests.Core.Services;

namespace RepairRequests.Tests;

/// <summary>
/// Интеграционные тесты слоя данных на реальной СУБД SQLite (временный файл).
/// Проверяют миграции, ссылочную целостность и работу репозиториев.
/// </summary>
public class RepositoriesIntegrationTests
{
    [Fact]
    public async Task Migrations_CreateSchemaAndAllowSeeding()
    {
        using var factory = new TestDbFactory();
        await using var db = factory.CreateDbContext();

        await db.Database.MigrateAsync();
        await DbSeeder.SeedAsync(db, new PasswordHasher());

        Assert.Equal(42, await db.RepairRequests.CountAsync());
        Assert.True(await db.Users.AnyAsync(u => u.Email == "admin@remont.ru"));
    }

    [Fact]
    public async Task Seeder_CreatesExpectedDemoData()
    {
        using var factory = await TestDbFactory.CreateSeededAsync();
        await using var db = factory.CreateDbContext();

        Assert.Equal(6, await db.Users.CountAsync());
        Assert.Equal(10, await db.FaultTypes.CountAsync());
        Assert.Equal(7, await db.Vehicles.CountAsync());
        Assert.Equal(42, await db.RepairRequests.CountAsync());

        Assert.Equal(2, await db.Users.CountAsync(u => u.Role == UserRole.Manager));
        Assert.Equal(3, await db.Users.CountAsync(u => u.Role == UserRole.Client));

        var numbers = await db.RepairRequests.Select(r => r.Number).ToListAsync();
        Assert.Equal(numbers.Count, numbers.Distinct().Count());
        Assert.Contains(numbers, n => n.EndsWith("-0001", StringComparison.Ordinal));

        var statuses = await db.RepairRequests.Select(r => r.Status).ToListAsync();
        Assert.Equal(2, statuses.Count(s => s == RequestStatus.New));
        Assert.Equal(1, statuses.Count(s => s == RequestStatus.Accepted));
        Assert.Equal(2, statuses.Count(s => s == RequestStatus.InProgress));
        Assert.Equal(3, statuses.Count(s => s == RequestStatus.Cancelled));
        Assert.Equal(34, statuses.Count(s => s is RequestStatus.Completed or RequestStatus.Closed));

        Assert.True(await db.RequestStatusHistory.CountAsync() > 120);
    }

    [Fact]
    public async Task Seeder_IsIdempotent()
    {
        using var factory = await TestDbFactory.CreateSeededAsync();
        await using var db = factory.CreateDbContext();

        await DbSeeder.SeedAsync(db, new PasswordHasher());

        Assert.Equal(6, await db.Users.CountAsync());
        Assert.Equal(42, await db.RepairRequests.CountAsync());
    }

    [Fact]
    public async Task SeededPasswords_CanBeVerified()
    {
        using var factory = await TestDbFactory.CreateSeededAsync();
        await using var db = factory.CreateDbContext();
        var hasher = new PasswordHasher();

        var admin = await db.Users.SingleAsync(u => u.Email == "admin@remont.ru");
        var client = await db.Users.SingleAsync(u => u.Email == "client@remont.ru");

        Assert.True(hasher.Verify("Admin123!", admin.PasswordHash));
        Assert.True(hasher.Verify("Client123!", client.PasswordHash));
        Assert.False(hasher.Verify("Admin123!", client.PasswordHash));
    }

    [Fact]
    public async Task UniqueEmail_IsEnforced()
    {
        using var factory = await TestDbFactory.CreateSeededAsync();
        await using var db = factory.CreateDbContext();

        db.Users.Add(new User
        {
            FullName = "Дубликат Пользователь",
            Email = "admin@remont.ru",
            PasswordHash = "x"
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task UniqueLicensePlate_IsEnforced()
    {
        using var factory = await TestDbFactory.CreateSeededAsync();
        await using var db = factory.CreateDbContext();
        var owner = await db.Users.FirstAsync(u => u.Role == UserRole.Client);

        db.Vehicles.Add(new Vehicle
        {
            OwnerId = owner.Id,
            Brand = "Lada",
            Model = "Granta",
            LicensePlate = "А123ВС77",
            Year = 2023
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task VehicleWithRequests_CannotBeDeleted()
    {
        using var factory = await TestDbFactory.CreateSeededAsync();
        await using var db = factory.CreateDbContext();
        var vehicle = await db.Vehicles.FirstAsync(v => v.Requests.Any());

        db.Vehicles.Remove(vehicle);

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task FaultTypeWithRequests_CannotBeDeleted()
    {
        using var factory = await TestDbFactory.CreateSeededAsync();
        await using var db = factory.CreateDbContext();
        var fault = await db.FaultTypes.FirstAsync(f => f.Requests.Any());

        db.FaultTypes.Remove(fault);

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task DeletingRequest_CascadesHistory()
    {
        using var factory = await TestDbFactory.CreateSeededAsync();
        await using var db = factory.CreateDbContext();
        var request = await db.RepairRequests.Include(r => r.History).FirstAsync(r => r.History.Any());
        var requestId = request.Id;
        Assert.True(request.History.Count > 0);

        db.RepairRequests.Remove(request);
        await db.SaveChangesAsync();

        Assert.False(await db.RequestStatusHistory.AnyAsync(h => h.RequestId == requestId));
        Assert.False(await db.RepairRequests.AnyAsync(r => r.Id == requestId));
    }

    [Fact]
    public async Task GetPaged_ClampsPageSizeAndReturnsTotal()
    {
        using var factory = await TestDbFactory.CreateSeededAsync();
        var repo = new RepairRequestRepository(factory);

        var page = await repo.GetPagedAsync(new RequestFilter { Page = 1, PageSize = 1 });

        Assert.Equal(42, page.TotalCount);
        Assert.Equal(5, page.PageSize);
        Assert.Equal(5, page.Items.Count);
        Assert.Equal(9, page.TotalPages);
        Assert.False(page.HasPrevious);
        Assert.True(page.HasNext);
    }

    [Fact]
    public async Task GetPaged_OrdersByDateDescending()
    {
        using var factory = await TestDbFactory.CreateSeededAsync();
        var repo = new RepairRequestRepository(factory);

        var page = await repo.GetPagedAsync(new RequestFilter { PageSize = 200 });

        var dates = page.Items.Select(r => r.CreatedAt).ToList();
        Assert.Equal(dates.OrderByDescending(d => d), dates);
    }

    [Fact]
    public async Task GetPaged_FiltersByClientAndStatus()
    {
        using var factory = await TestDbFactory.CreateSeededAsync();
        var repo = new RepairRequestRepository(factory);
        int clientId;
        await using (var db = factory.CreateDbContext())
            clientId = (await db.Users.SingleAsync(u => u.Email == "client@remont.ru")).Id;

        var byClient = await repo.GetPagedAsync(new RequestFilter { ClientId = clientId, PageSize = 200 });
        Assert.All(byClient.Items, r => Assert.Equal(clientId, r.ClientId));

        var byStatus = await repo.GetPagedAsync(new RequestFilter
        {
            Status = RequestStatus.New,
            PageSize = 200
        });
        Assert.Equal(2, byStatus.TotalCount);
        Assert.All(byStatus.Items, r => Assert.Equal(RequestStatus.New, r.Status));
    }

    [Fact]
    public async Task GetPaged_SearchFindsByNumberAndPlate()
    {
        using var factory = await TestDbFactory.CreateSeededAsync();
        var repo = new RepairRequestRepository(factory);

        var byNumber = await repo.GetPagedAsync(new RequestFilter { Search = $"ЗР-{DateTime.Now.Year}-0044" });
        Assert.Equal(1, byNumber.TotalCount);
        Assert.Equal($"ЗР-{DateTime.Now.Year}-0044", byNumber.Items[0].Number);

        var byPlate = await repo.GetPagedAsync(new RequestFilter { Search = "А123ВС77", PageSize = 200 });
        Assert.True(byPlate.TotalCount > 0);
        Assert.All(byPlate.Items, r => Assert.Equal("А123ВС77", r.Vehicle!.LicensePlate));
    }

    [Fact]
    public async Task GetPaged_IncludesRelatedEntities()
    {
        using var factory = await TestDbFactory.CreateSeededAsync();
        var repo = new RepairRequestRepository(factory);

        var page = await repo.GetPagedAsync(new RequestFilter { PageSize = 5 });

        Assert.All(page.Items, r =>
        {
            Assert.NotNull(r.Vehicle);
            Assert.NotNull(r.FaultType);
            Assert.NotNull(r.Client);
        });
    }

    [Fact]
    public async Task GetForPeriod_RespectsDatesAndClient()
    {
        using var factory = await TestDbFactory.CreateSeededAsync();
        var repo = new RepairRequestRepository(factory);
        int clientId;
        await using (var db = factory.CreateDbContext())
            clientId = (await db.Users.SingleAsync(u => u.Email == "client3@remont.ru")).Id;

        var all = await repo.GetForPeriodAsync(DateTime.Today.AddMonths(-12), DateTime.Today);
        Assert.NotEmpty(all);

        var scoped = await repo.GetForPeriodAsync(
            DateTime.Today.AddMonths(-12), DateTime.Today, clientId);
        Assert.NotEmpty(scoped);
        Assert.All(scoped, r => Assert.Equal(clientId, r.ClientId));
        Assert.All(scoped, r => Assert.True(r.CreatedAt.Date <= DateTime.Today));

        var empty = await repo.GetForPeriodAsync(new DateTime(2030, 1, 1), new DateTime(2030, 12, 31));
        Assert.Empty(empty);
    }

    [Fact]
    public async Task GetCompletedInPeriod_ReturnsOnlyFinishedRequests()
    {
        using var factory = await TestDbFactory.CreateSeededAsync();
        var repo = new RepairRequestRepository(factory);

        var completed = await repo.GetCompletedInPeriodAsync(
            DateTime.Today.AddMonths(-12), DateTime.Today);

        Assert.NotEmpty(completed);
        Assert.All(completed, r => Assert.True(
            r.Status is RequestStatus.Completed or RequestStatus.Closed));
        Assert.All(completed, r => Assert.NotNull(r.CompletedAt));
    }

    [Fact]
    public async Task GenerateNumber_ReturnsNextSequenceNumber()
    {
        using var factory = await TestDbFactory.CreateSeededAsync();
        var repo = new RepairRequestRepository(factory);

        var number = await repo.GenerateNumberAsync();

        Assert.Equal($"ЗР-{DateTime.Now.Year}-0045", number);
    }

    [Fact]
    public async Task GetDashboardStats_CountsActiveRequests()
    {
        using var factory = await TestDbFactory.CreateSeededAsync();
        var repo = new RepairRequestRepository(factory);

        var stats = await repo.GetDashboardStatsAsync();

        Assert.Equal(42, stats.TotalRequests);
        Assert.Equal(3, stats.OpenRequests);
        Assert.Equal(2, stats.InProgress);
        Assert.True(stats.AverageRepairHours >= 0);
    }

    [Fact]
    public async Task VehicleRepository_ChecksPlateIgnoringCase()
    {
        using var factory = await TestDbFactory.CreateSeededAsync();
        var repo = new VehicleRepository(factory);

        Assert.True(await repo.LicensePlateExistsAsync("а123вс77"));
        Assert.False(await repo.LicensePlateExistsAsync("Х999ХХ99"));

        var vehicle = await repo.GetPagedAsync(new VehicleFilter { Search = "Toyota" });
        Assert.Equal(1, vehicle.TotalCount);
        Assert.Equal("Toyota", vehicle.Items[0].Brand);
    }

    [Fact]
    public async Task FaultTypeRepository_ReturnsCategoriesAndActiveList()
    {
        using var factory = await TestDbFactory.CreateSeededAsync();
        var repo = new FaultTypeRepository(factory);

        var categories = await repo.GetCategoriesAsync();
        var active = await repo.GetActiveAsync();

        Assert.Equal(10, active.Count);
        Assert.Contains("Двигатель", categories);
        Assert.Contains("Кузов", categories);
        Assert.True(await repo.NameExistsAsync("Ремонт двигателя"));
        Assert.False(await repo.NameExistsAsync("Несуществующая неисправность"));
    }

    [Fact]
    public async Task AuthService_WorksAgainstSeededDatabase()
    {
        using var factory = await TestDbFactory.CreateSeededAsync();
        var auth = new AuthService(new UserRepository(factory), new PasswordHasher());

        var ok = await auth.LoginAsync("client@remont.ru", "Client123!");
        Assert.True(ok.Success);
        Assert.Equal(UserRole.Client, ok.User!.Role);

        var fail = await auth.LoginAsync("client@remont.ru", "Client124!");
        Assert.False(fail.Success);

        await using var db = factory.CreateDbContext();
        var stored = await db.Users.SingleAsync(u => u.Email == "client@remont.ru");
        Assert.NotNull(stored.LastLoginAt);
    }

    [Fact]
    public async Task WorkflowUpdate_PersistsStatusAndHistory()
    {
        using var factory = await TestDbFactory.CreateSeededAsync();
        var repo = new RepairRequestRepository(factory);
        int managerId;
        await using (var db = factory.CreateDbContext())
            managerId = (await db.Users.SingleAsync(u => u.Email == "manager@remont.ru")).Id;

        var page = await repo.GetPagedAsync(new RequestFilter { Status = RequestStatus.New });
        var request = await repo.GetByIdAsync(page.Items[0].Id, asTracking: true);
        Assert.NotNull(request);
        var historyBefore = request!.History.Count;

        RequestWorkflow.Apply(request, RequestStatus.Accepted, managerId, "Принята менеджером");
        await repo.UpdateAsync(request);

        var reloaded = await repo.GetByIdAsync(request.Id);
        Assert.NotNull(reloaded);
        Assert.Equal(RequestStatus.Accepted, reloaded!.Status);
        Assert.NotNull(reloaded.AcceptedAt);
        Assert.Equal(historyBefore + 1, reloaded.History.Count);
        Assert.Contains(reloaded.History, h => h.Status == RequestStatus.Accepted);
    }

    [Fact]
    public async Task AddRequest_GeneratesNumberAndPersists()
    {
        using var factory = await TestDbFactory.CreateSeededAsync();
        var repo = new RepairRequestRepository(factory);
        var vehicleRepo = new VehicleRepository(factory);
        var faultRepo = new FaultTypeRepository(factory);

        var vehicle = (await vehicleRepo.GetAllAsync())[0];
        var fault = (await faultRepo.GetActiveAsync())[0];
        var number = await repo.GenerateNumberAsync();

        var request = new RepairRequest
        {
            Number = number,
            VehicleId = vehicle.Id,
            FaultTypeId = fault.Id,
            ClientId = vehicle.OwnerId,
            Description = "Тестовая заявка, созданная интеграционным тестом",
            Priority = RequestPriority.High,
            Status = RequestStatus.New,
            CreatedAt = DateTime.Now,
            LaborCost = 1000m,
            PartsCost = 250m
        };

        await repo.AddAsync(request);
        Assert.True(request.Id > 0);

        var stored = await repo.GetByIdAsync(request.Id);
        Assert.NotNull(stored);
        Assert.Equal(number, stored!.Number);
        Assert.Equal(RequestPriority.High, stored.Priority);
        Assert.Equal(1250m, stored.TotalCost);
    }
}
