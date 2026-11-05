using Microsoft.EntityFrameworkCore;
using RepairRequests.Core.Models;
using RepairRequests.Core.Services;

namespace RepairRequests.Core.Data;

/// <summary>
/// Заполнение базы демонстрационными данными. Набор детерминирован (Random с фиксированным
/// зерном), поэтому отчёты и статистика воспроизводятся при каждом создании базы.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, PasswordHasher hasher)
    {
        if (await db.Users.AnyAsync())
            return;

        var now = DateTime.Now;
        var rnd = new Random(2026);

        var admin = new User
        {
            FullName = "Максаков Максим Сергеевич",
            Email = "admin@remont.ru",
            PasswordHash = hasher.Hash("Admin123!"),
            Role = UserRole.Administrator,
            Phone = "+7 (900) 100-10-01",
            CreatedAt = now.AddMonths(-8)
        };
        var manager = new User
        {
            FullName = "Иванов Пётр Андреевич",
            Email = "manager@remont.ru",
            PasswordHash = hasher.Hash("Manager123!"),
            Role = UserRole.Manager,
            Phone = "+7 (900) 100-10-02",
            CreatedAt = now.AddMonths(-8)
        };
        var mechanic2 = new User
        {
            FullName = "Сидоров Николай Ильич",
            Email = "mech2@remont.ru",
            PasswordHash = hasher.Hash("Manager123!"),
            Role = UserRole.Manager,
            Phone = "+7 (900) 100-10-03",
            CreatedAt = now.AddMonths(-6)
        };
        var client1 = new User
        {
            FullName = "Кузнецов Алексей Викторович",
            Email = "client@remont.ru",
            PasswordHash = hasher.Hash("Client123!"),
            Role = UserRole.Client,
            Phone = "+7 (900) 200-20-01",
            CreatedAt = now.AddMonths(-7)
        };
        var client2 = new User
        {
            FullName = "ООО «Логистик Плюс»",
            Email = "client2@remont.ru",
            PasswordHash = hasher.Hash("Client123!"),
            Role = UserRole.Client,
            Phone = "+7 (900) 200-20-02",
            CreatedAt = now.AddMonths(-6)
        };
        var client3 = new User
        {
            FullName = "Морозова Екатерина Дмитриевна",
            Email = "client3@remont.ru",
            PasswordHash = hasher.Hash("Client123!"),
            Role = UserRole.Client,
            Phone = "+7 (900) 200-20-03",
            CreatedAt = now.AddMonths(-4)
        };

        var users = new[] { admin, manager, mechanic2, client1, client2, client3 };
        db.Users.AddRange(users);
        await db.SaveChangesAsync();

        var faults = new[]
        {
            new FaultType { Name = "Замена тормозных колодок", Category = "Тормозная система", NormHours = 1.5m, BasePrice = 3500m, Description = "Замена передних или задних колодок, проверка тормозных дисков." },
            new FaultType { Name = "Ремонт двигателя", Category = "Двигатель", NormHours = 8m, BasePrice = 18000m, Description = "Диагностика и ремонт ДВС, замена прокладок и расходников." },
            new FaultType { Name = "Замена масла и фильтров", Category = "Техническое обслуживание", NormHours = 1m, BasePrice = 2500m, Description = "Плановое ТО: моторное масло, масляный и воздушный фильтры." },
            new FaultType { Name = "Замена амортизатора", Category = "Подвеска", NormHours = 2.5m, BasePrice = 6000m, Description = "Замена переднего или заднего амортизатора, проверка стоек." },
            new FaultType { Name = "Ремонт генератора", Category = "Электрооборудование", NormHours = 4m, BasePrice = 9000m, Description = "Снятие, ремонт и установка генератора, проверка зарядки АКБ." },
            new FaultType { Name = "Замена сцепления", Category = "Трансмиссия", NormHours = 6m, BasePrice = 14000m, Description = "Замена комплекта сцепления, проверка корзины и выжимного подшипника." },
            new FaultType { Name = "Ремонт системы охлаждения", Category = "Двигатель", NormHours = 3m, BasePrice = 7000m, Description = "Поиск утечки, замена патрубков, радиатора или термостата." },
            new FaultType { Name = "Шиномонтаж", Category = "Техническое обслуживание", NormHours = 0.5m, BasePrice = 1500m, Description = "Сезонная замена и балансировка колёс." },
            new FaultType { Name = "Диагностика электрики", Category = "Электрооборудование", NormHours = 1m, BasePrice = 2000m, Description = "Компьютерная диагностика, поиск неисправностей проводки." },
            new FaultType { Name = "Замена лобового стекла", Category = "Кузов", NormHours = 2m, BasePrice = 12000m, Description = "Демонтаж повреждённого и установка нового стекла, герметизация." }
        };
        db.FaultTypes.AddRange(faults);
        await db.SaveChangesAsync();

        var vehicles = new[]
        {
            new Vehicle { OwnerId = client1.Id, Brand = "Lada", Model = "Vesta", LicensePlate = "А123ВС77", Vin = "XTAGFL110M1234567", Year = 2021, Mileage = 68400, CreatedAt = now.AddMonths(-7) },
            new Vehicle { OwnerId = client1.Id, Brand = "Toyota", Model = "Camry", LicensePlate = "В456ЕК50", Vin = "JTMBFREV40J123456", Year = 2019, Mileage = 132500, CreatedAt = now.AddMonths(-6) },
            new Vehicle { OwnerId = client2.Id, Brand = "Ford", Model = "Transit", LicensePlate = "Е789МН99", Vin = "WF0XXXTTGXKY12345", Year = 2020, Mileage = 241300, CreatedAt = now.AddMonths(-6) },
            new Vehicle { OwnerId = client2.Id, Brand = "ГАЗ", Model = "Газель Next", LicensePlate = "К321ОР77", Vin = "X9612345678901234", Year = 2022, Mileage = 98700, CreatedAt = now.AddMonths(-5) },
            new Vehicle { OwnerId = client2.Id, Brand = "Mercedes-Benz", Model = "Sprinter", LicensePlate = "М654ТУ50", Vin = "WDB9066571P123456", Year = 2018, Mileage = 310800, CreatedAt = now.AddMonths(-4) },
            new Vehicle { OwnerId = client3.Id, Brand = "Kia", Model = "Rio", LicensePlate = "Н987СХ77", Vin = "Z94C241BBLR123456", Year = 2022, Mileage = 41200, CreatedAt = now.AddMonths(-4) },
            new Vehicle { OwnerId = client3.Id, Brand = "Skoda", Model = "Octavia", LicensePlate = "Р147УХ99", Vin = "TMBJJ7NE0L0123456", Year = 2020, Mileage = 88300, CreatedAt = now.AddMonths(-3) }
        };
        db.Vehicles.AddRange(vehicles);
        await db.SaveChangesAsync();

        var mechanics = new[] { manager, mechanic2 };
        var clients = new[] { client1, client2, client3 };
        var requests = new List<RepairRequest>();

        // 34 завершённых заявки за последние ~5 месяцев — база для статистики и отчётов.
        for (var i = 0; i < 34; i++)
        {
            var vehicle = vehicles[rnd.Next(vehicles.Length)];
            var fault = faults[rnd.Next(faults.Length)];
            var mechanic = mechanics[rnd.Next(mechanics.Length)];
            var created = now.Date.AddDays(-(150 - i * 4)).AddHours(8 + rnd.Next(0, 9)).AddMinutes(rnd.Next(0, 60));
            var accepted = created.AddHours(1 + rnd.Next(0, 4)).AddMinutes(rnd.Next(0, 60));
            var started = accepted.AddHours(2 + rnd.Next(0, 44)).AddMinutes(rnd.Next(0, 60));

            // Экспоненциальное распределение с масштабом, зависящим от норматива.
            var scale = (double)fault.NormHours * 1.25;
            var repairHours = Math.Clamp(-Math.Log(1 - rnd.NextDouble()) * scale + 0.4, 0.5, 30);
            var completed = started.AddHours(repairHours);
            var closed = completed.AddHours(1 + rnd.Next(0, 20));
            var status = i % 11 == 0 ? RequestStatus.Completed : RequestStatus.Closed;

            var labor = Math.Round(fault.BasePrice * (0.85m + (decimal)rnd.NextDouble() * 0.35m), 0);
            var parts = rnd.Next(0, 5) == 0 ? 0m : Math.Round(rnd.Next(500, 24000) / 100m, 0) * 100m;

            var request = new RepairRequest
            {
                Number = $"ЗР-{created:yyyy}-{(i + 1):D4}",
                VehicleId = vehicle.Id,
                FaultTypeId = fault.Id,
                ClientId = vehicle.OwnerId,
                AssignedMechanicId = mechanic.Id,
                Description = BuildDescription(fault, vehicle, i),
                Priority = (RequestPriority)rnd.Next(1, 5),
                Status = status,
                CreatedAt = created,
                AcceptedAt = accepted,
                StartedAt = started,
                CompletedAt = completed,
                ClosedAt = status == RequestStatus.Closed ? closed : null,
                PlannedCompletion = created.Date.AddDays(3),
                LaborCost = labor,
                PartsCost = parts,
                Comment = status == RequestStatus.Closed ? "Работы выполнены, автомобиль выдан клиенту." : "Работы выполнены, ожидается приёмка."
            };
            requests.Add(request);

            request.History.Add(History(RequestStatus.New, created, vehicle.OwnerId, "Заявка создана"));
            request.History.Add(History(RequestStatus.Accepted, accepted, mechanic.Id, "Заявка принята в работу"));
            request.History.Add(History(RequestStatus.InProgress, started, mechanic.Id, "Начаты ремонтные работы"));
            request.History.Add(History(RequestStatus.Completed, completed, mechanic.Id, "Ремонт завершён"));
            if (status == RequestStatus.Closed)
                request.History.Add(History(RequestStatus.Closed, closed, manager.Id, "Заявка закрыта, автомобиль выдан"));
        }

        // Три отменённые заявки.
        for (var i = 0; i < 3; i++)
        {
            var vehicle = vehicles[rnd.Next(vehicles.Length)];
            var fault = faults[rnd.Next(faults.Length)];
            var created = now.Date.AddDays(-(18 + i * 9)).AddHours(10 + i);
            var request = new RepairRequest
            {
                Number = $"ЗР-{created:yyyy}-{(35 + i):D4}",
                VehicleId = vehicle.Id,
                FaultTypeId = fault.Id,
                ClientId = vehicle.OwnerId,
                Description = BuildDescription(fault, vehicle, 40 + i),
                Priority = RequestPriority.Normal,
                Status = RequestStatus.Cancelled,
                CreatedAt = created,
                PlannedCompletion = created.Date.AddDays(3),
                Comment = "Клиент отказался от ремонта: неисправность устранена самостоятельно."
            };
            request.History.Add(History(RequestStatus.New, created, vehicle.OwnerId, "Заявка создана"));
            request.History.Add(History(RequestStatus.Cancelled, created.AddHours(3 + i), vehicle.OwnerId, "Отмена по инициативе клиента"));
            requests.Add(request);
        }

        // Пять активных заявок в разных статусах.
        var activeStatuses = new[]
        {
            RequestStatus.New, RequestStatus.New,
            RequestStatus.Accepted,
            RequestStatus.InProgress, RequestStatus.InProgress
        };
        for (var i = 0; i < activeStatuses.Length; i++)
        {
            var vehicle = vehicles[rnd.Next(vehicles.Length)];
            var fault = faults[rnd.Next(faults.Length)];
            var status = activeStatuses[i];
            var created = now.AddDays(-(6 - i)).AddHours(-rnd.Next(1, 8));
            var mechanic = mechanics[rnd.Next(mechanics.Length)];
            var request = new RepairRequest
            {
                Number = $"ЗР-{now:yyyy}-{(40 + i):D4}",
                VehicleId = vehicle.Id,
                FaultTypeId = fault.Id,
                ClientId = vehicle.OwnerId,
                AssignedMechanicId = status == RequestStatus.New ? null : mechanic.Id,
                Description = BuildDescription(fault, vehicle, 50 + i),
                Priority = i == 4 ? RequestPriority.Critical : (RequestPriority)rnd.Next(1, 4),
                Status = status,
                CreatedAt = created,
                AcceptedAt = status is RequestStatus.Accepted or RequestStatus.InProgress ? created.AddHours(1) : null,
                StartedAt = status == RequestStatus.InProgress ? created.AddHours(3) : null,
                PlannedCompletion = created.Date.AddDays(3),
                Comment = status == RequestStatus.InProgress ? "Ожидается поставка запчасти." : null
            };
            request.History.Add(History(RequestStatus.New, created, vehicle.OwnerId, "Заявка создана"));
            if (status is RequestStatus.Accepted or RequestStatus.InProgress)
                request.History.Add(History(RequestStatus.Accepted, created.AddHours(1), mechanic.Id, "Заявка принята"));
            if (status == RequestStatus.InProgress)
                request.History.Add(History(RequestStatus.InProgress, created.AddHours(3), mechanic.Id, "Начаты работы"));
            requests.Add(request);
        }

        db.RepairRequests.AddRange(requests);
        await db.SaveChangesAsync();
    }

    private static RequestStatusHistory History(RequestStatus status, DateTime at, int userId, string comment) =>
        new() { Status = status, ChangedAt = at, ChangedById = userId, Comment = comment };

    private static string BuildDescription(FaultType fault, Vehicle vehicle, int seed)
    {
        var complaints = new[]
        {
            "Клиент сообщает о постороннем шуме и вибрации при движении.",
            "Появился скрип при торможении, педаль стала мягкой.",
            "На приборной панели горит индикатор неисправности.",
            "Периодически глохнет двигатель, вырос расход топлива.",
            "Обнаружена течь технической жидкости под автомобилем.",
            "Требуется плановое техническое обслуживание перед рейсом."
        };
        return $"{complaints[seed % complaints.Length]} Автомобиль {vehicle.Brand} {vehicle.Model}, пробег {vehicle.Mileage:N0} км.";
    }
}
