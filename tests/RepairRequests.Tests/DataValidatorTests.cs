using RepairRequests.Core.Models;

namespace RepairRequests.Tests;

public class DataValidatorTests
{
    private static User ValidUser() => new()
    {
        FullName = "Иванов Иван Иванович",
        Email = "ivanov@example.com",
        PasswordHash = "pbkdf2-sha256$1000$abc$def"
    };

    private static Vehicle ValidVehicle() => new()
    {
        Brand = "Lada",
        Model = "Vesta",
        LicensePlate = "А123ВС77",
        Vin = "XTAGFL110M1234567",
        Year = 2021,
        Mileage = 68_400
    };

    private static RepairRequest ValidRequest() => new()
    {
        Number = "ЗР-2026-0001",
        Description = "Посторонний шум в передней подвеске при движении",
        LaborCost = 1000m,
        PartsCost = 500m
    };

    [Fact]
    public void ValidUser_PassesValidation()
    {
        Assert.True(DataValidator.IsValid(ValidUser()));
    }

    [Fact]
    public void User_WithShortFullName_Fails()
    {
        var user = ValidUser();
        user.FullName = "Ив";

        Assert.False(DataValidator.IsValid(user));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("user@")]
    [InlineData("@example.com")]
    public void User_WithInvalidEmail_Fails(string email)
    {
        var user = ValidUser();
        user.Email = email;

        Assert.False(DataValidator.IsValid(user));
    }

    [Fact]
    public void User_WithoutPasswordHash_Fails()
    {
        var user = ValidUser();
        user.PasswordHash = string.Empty;

        Assert.False(DataValidator.IsValid(user));
    }

    [Fact]
    public void ValidVehicle_PassesValidation()
    {
        Assert.True(DataValidator.IsValid(ValidVehicle()));
    }

    [Theory]
    [InlineData("А123ВС77")]
    [InlineData("A123BC77")]
    [InlineData("М654ТУ50")]
    public void Vehicle_WithValidPlate_Passes(string plate)
    {
        var vehicle = ValidVehicle();
        vehicle.LicensePlate = plate;

        Assert.True(DataValidator.IsValid(vehicle));
    }

    [Theory]
    [InlineData("123АВС77")]
    [InlineData("А12ВС77")]
    [InlineData("А123ВС7")]
    [InlineData("")]
    public void Vehicle_WithInvalidPlate_Fails(string plate)
    {
        var vehicle = ValidVehicle();
        vehicle.LicensePlate = plate;

        Assert.False(DataValidator.IsValid(vehicle));
    }

    [Fact]
    public void Vehicle_WithShortVin_Fails()
    {
        var vehicle = ValidVehicle();
        vehicle.Vin = "XTAGFL110M123456";

        Assert.False(DataValidator.IsValid(vehicle));
    }

    [Fact]
    public void Vehicle_WithVinContainingForbiddenLetters_Fails()
    {
        var vehicle = ValidVehicle();
        vehicle.Vin = "XTAGFL110M12345IO";

        Assert.False(DataValidator.IsValid(vehicle));
    }

    [Fact]
    public void Vehicle_WithoutVin_Passes()
    {
        var vehicle = ValidVehicle();
        vehicle.Vin = null;

        Assert.True(DataValidator.IsValid(vehicle));
    }

    [Theory]
    [InlineData(1949)]
    [InlineData(2101)]
    public void Vehicle_WithYearOutOfRange_Fails(int year)
    {
        var vehicle = ValidVehicle();
        vehicle.Year = year;

        Assert.False(DataValidator.IsValid(vehicle));
    }

    [Fact]
    public void Vehicle_WithNegativeMileage_Fails()
    {
        var vehicle = ValidVehicle();
        vehicle.Mileage = -1;

        Assert.False(DataValidator.IsValid(vehicle));
    }

    [Fact]
    public void ValidRequest_PassesValidation()
    {
        Assert.True(DataValidator.IsValid(ValidRequest()));
    }

    [Fact]
    public void Request_WithShortDescription_Fails()
    {
        var request = ValidRequest();
        request.Description = "Шумит";

        var errors = DataValidator.Validate(request);

        Assert.Contains(errors, e => e.Contains("10"));
    }

    [Fact]
    public void Request_WithNegativeCost_Fails()
    {
        var request = ValidRequest();
        request.LaborCost = -1m;

        Assert.False(DataValidator.IsValid(request));
    }

    [Fact]
    public void FaultType_WithNormHoursOutOfRange_Fails()
    {
        var fault = new FaultType
        {
            Name = "Тестовая неисправность",
            Category = "Диагностика",
            NormHours = 0m,
            BasePrice = 1000m
        };

        Assert.False(DataValidator.IsValid(fault));

        fault.NormHours = 201m;
        Assert.False(DataValidator.IsValid(fault));

        fault.NormHours = 1.5m;
        Assert.True(DataValidator.IsValid(fault));
    }

    [Fact]
    public void Validate_ReturnsAllErrors()
    {
        var user = new User { FullName = "И", Email = "bad", PasswordHash = "" };

        Assert.True(DataValidator.Validate(user).Count >= 3);
    }
}
