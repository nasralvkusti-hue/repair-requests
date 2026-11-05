using RepairRequests.Core.Models;

namespace RepairRequests.Core.Repositories;

public interface IRepairRequestRepository
{
    Task<PagedResult<RepairRequest>> GetPagedAsync(RequestFilter filter);
    Task<RepairRequest?> GetByIdAsync(int id, bool asTracking = false);
    Task<IReadOnlyList<RepairRequest>> GetRecentAsync(int count, int? clientId = null);
    Task<IReadOnlyList<RepairRequest>> GetForPeriodAsync(DateTime from, DateTime to, int? clientId = null);
    Task<IReadOnlyList<RepairRequest>> GetCompletedInPeriodAsync(DateTime from, DateTime to, int? clientId = null);
    Task<DashboardStats> GetDashboardStatsAsync(int? clientId = null);
    Task<string> GenerateNumberAsync();
    Task AddAsync(RepairRequest request);
    Task UpdateAsync(RepairRequest request);
    Task DeleteAsync(RepairRequest request);
    Task<bool> HasRequestsForVehicleAsync(int vehicleId);
    Task<bool> HasRequestsForFaultTypeAsync(int faultTypeId);
}

public interface IVehicleRepository
{
    Task<PagedResult<Vehicle>> GetPagedAsync(VehicleFilter filter);
    Task<IReadOnlyList<Vehicle>> GetAllAsync(int? ownerId = null);
    Task<Vehicle?> GetByIdAsync(int id);
    Task AddAsync(Vehicle vehicle);
    Task UpdateAsync(Vehicle vehicle);
    Task DeleteAsync(Vehicle vehicle);
    Task<bool> HasRequestsAsync(int vehicleId);
    Task<bool> LicensePlateExistsAsync(string licensePlate, int? excludeId = null);
}

public interface IFaultTypeRepository
{
    Task<PagedResult<FaultType>> GetPagedAsync(FaultTypeFilter filter);
    Task<IReadOnlyList<FaultType>> GetActiveAsync();
    Task<IReadOnlyList<string>> GetCategoriesAsync();
    Task<FaultType?> GetByIdAsync(int id);
    Task AddAsync(FaultType faultType);
    Task UpdateAsync(FaultType faultType);
    Task DeleteAsync(FaultType faultType);
    Task<bool> HasRequestsAsync(int faultTypeId);
    Task<bool> NameExistsAsync(string name, int? excludeId = null);
}

public interface IUserRepository
{
    Task<PagedResult<User>> GetPagedAsync(UserFilter filter);
    Task<IReadOnlyList<User>> GetMechanicsAsync();
    Task<IReadOnlyList<User>> GetClientsAsync();
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(int id);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task<bool> EmailExistsAsync(string email, int? excludeId = null);
    Task<int> CountAsync();
}
