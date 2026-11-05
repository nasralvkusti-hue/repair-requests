using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RepairRequests.Core.Data;

/// <summary>
/// Фабрика контекста для команд `dotnet ef` (миграции создаются без запуска WinForms).
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=repair-design.db")
            .Options;

        return new AppDbContext(options);
    }
}
