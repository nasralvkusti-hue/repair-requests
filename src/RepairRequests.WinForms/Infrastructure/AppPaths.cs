namespace RepairRequests.WinForms.Infrastructure;

/// <summary>Пути приложения: база данных, журналы, отчёты, ресурсы.</summary>
public static class AppPaths
{
    public static string BaseDirectory => AppContext.BaseDirectory;
    public static string DataDirectory => Path.Combine(BaseDirectory, "data");
    public static string LogDirectory => Path.Combine(BaseDirectory, "logs");
    public static string ReportsDirectory => Path.Combine(BaseDirectory, "reports");
    public static string ResourceDirectory => Path.Combine(BaseDirectory, "Resources");

    public static void EnsureCreated()
    {
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(LogDirectory);
        Directory.CreateDirectory(ReportsDirectory);
    }

    public static string Resource(string fileName) => Path.Combine(ResourceDirectory, fileName);
}
