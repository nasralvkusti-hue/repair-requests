using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RepairRequests.Core.Data;
using RepairRequests.Core.Services;
using RepairRequests.WinForms.Forms;
using RepairRequests.WinForms.Infrastructure;
using Serilog;

namespace RepairRequests.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        SplashForm? splash = null;
        try
        {
            splash = new SplashForm();
            splash.Show();
            splash.Refresh();
            Application.DoEvents();

            splash.SetStatus("Создание рабочих папок…");
            AppPaths.EnsureCreated();

            splash.SetStatus("Настройка журнала…");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File(
                    Path.Combine(AppPaths.LogDirectory, "app-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 14,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppPaths.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            var configuredPath = configuration["Database:Path"] ?? "data/repair-requests.db";
            var databasePath = Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(AppPaths.BaseDirectory, configuredPath);

            Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

            var factory = new SqliteDbContextFactory(databasePath);
            var hasher = new PasswordHasher();

            Log.Information("Запуск приложения. База данных: {Path}", databasePath);

            splash.SetStatus("Проверка базы данных…");
            using (var db = factory.CreateDbContext())
            {
                db.Database.Migrate();
                splash.SetStatus("Подготовка демонстрационных данных…");
                DbSeeder.SeedAsync(db, hasher).GetAwaiter().GetResult();
            }

            AppServices.Initialize(factory, hasher, databasePath);
            Log.Information("База данных готова. Пользователей: {Count}", AppServices.Users.CountAsync().GetAwaiter().GetResult());

            splash.SetStatus("Готово");
            splash.Close();
            splash.Dispose();
            splash = null;

            Application.ThreadException += (_, e) =>
            {
                Log.Error(e.Exception, "Необработанная ошибка интерфейса");
                UiTheme.Error("Произошла непредвиденная ошибка:\n" + e.Exception.Message);
            };

            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                Log.Fatal(e.ExceptionObject as Exception, "Необработанная ошибка приложения");

            Application.Run(new LoginForm());
        }
        catch (Exception ex)
        {
            splash?.Close();
            splash?.Dispose();
            splash = null;

            Log.Fatal(ex, "Не удалось запустить приложение");
            MessageBox.Show(
                "Не удалось запустить приложение.\n" + ex.Message +
                "\n\nПодробности записаны в папку logs.",
                "Ошибка запуска", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            splash?.Dispose();
            Log.CloseAndFlush();
        }
    }
}
