using Microsoft.EntityFrameworkCore;
using RepairRequests.Core.Models;

namespace RepairRequests.Core.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<FaultType> FaultTypes => Set<FaultType>();
    public DbSet<RepairRequest> RepairRequests => Set<RepairRequest>();
    public DbSet<RequestStatusHistory> RequestStatusHistory => Set<RequestStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
            entity.Property(u => u.FullName).IsRequired().HasMaxLength(120);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(160);
            entity.Property(u => u.PasswordHash).IsRequired();
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasIndex(v => v.LicensePlate).IsUnique();
            entity.Property(v => v.LicensePlate).IsRequired().HasMaxLength(12);

            // Ссылочная целостность: владельца с автомобилями удалить нельзя.
            entity.HasOne(v => v.Owner)
                .WithMany(u => u.Vehicles)
                .HasForeignKey(v => v.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FaultType>(entity =>
        {
            entity.Property(f => f.Name).IsRequired().HasMaxLength(120);
            entity.Property(f => f.NormHours).HasPrecision(6, 2);
            entity.Property(f => f.BasePrice).HasPrecision(12, 2);
        });

        modelBuilder.Entity<RepairRequest>(entity =>
        {
            entity.HasIndex(r => r.Number).IsUnique();
            entity.HasIndex(r => r.CreatedAt);
            entity.HasIndex(r => r.Status);
            entity.Property(r => r.Number).IsRequired().HasMaxLength(24);
            entity.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(r => r.Priority).HasConversion<string>().HasMaxLength(20);
            entity.Property(r => r.LaborCost).HasPrecision(12, 2);
            entity.Property(r => r.PartsCost).HasPrecision(12, 2);
            entity.Property(r => r.PartsCostCurrency).HasPrecision(12, 2);
            entity.Property(r => r.ExchangeRate).HasPrecision(12, 4);

            // Запрет удаления справочных записей, на которые ссылаются заявки.
            entity.HasOne(r => r.Vehicle)
                .WithMany(v => v.Requests)
                .HasForeignKey(r => r.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.FaultType)
                .WithMany(f => f.Requests)
                .HasForeignKey(r => r.FaultTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Client)
                .WithMany(u => u.Requests)
                .HasForeignKey(r => r.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.AssignedMechanic)
                .WithMany(u => u.AssignedRequests)
                .HasForeignKey(r => r.AssignedMechanicId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RequestStatusHistory>(entity =>
        {
            entity.Property(h => h.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(h => new { h.RequestId, h.ChangedAt });

            // История является частью заявки: удаляется каскадно вместе с ней.
            entity.HasOne(h => h.Request)
                .WithMany(r => r.History)
                .HasForeignKey(h => h.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(h => h.ChangedBy)
                .WithMany()
                .HasForeignKey(h => h.ChangedById)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
