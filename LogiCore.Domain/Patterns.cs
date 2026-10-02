namespace LogiCore.Domain;

public interface ITariffStrategy
{
    string Name { get; }
    decimal Calculate(decimal baseCost);
}

public sealed class StandardTariff : ITariffStrategy
{
    public string Name => "Стандартный";
    public decimal Calculate(decimal baseCost) => baseCost;
}

public sealed class ExpressTariff : ITariffStrategy
{
    public string Name => "Срочный";
    public decimal Calculate(decimal baseCost) => baseCost * 1.5m;
}

public interface IDeliveryCost
{
    decimal Total { get; }
    string Describe();
}

public sealed class BasicDeliveryCost : IDeliveryCost
{
    public BasicDeliveryCost(decimal total) => Total = total;
    public decimal Total { get; }
    public string Describe() => $"Базовая доставка: {Total:F2}";
}

public abstract class DeliveryCostDecorator : IDeliveryCost
{
    protected DeliveryCostDecorator(IDeliveryCost inner) => Inner = inner;
    protected IDeliveryCost Inner { get; }
    public abstract decimal Total { get; }
    public abstract string Describe();
}

public sealed class InsuranceDecorator : DeliveryCostDecorator
{
    private readonly decimal _declaredValue;
    public InsuranceDecorator(IDeliveryCost inner, decimal declaredValue) : base(inner) => _declaredValue = declaredValue;
    public override decimal Total => Inner.Total + _declaredValue * 0.01m;
    public override string Describe() => Inner.Describe() + $" -> страховка: {Total:F2}";
}

public sealed class PackingDecorator : DeliveryCostDecorator
{
    public PackingDecorator(IDeliveryCost inner) : base(inner) { }
    public override decimal Total => Inner.Total + 500;
    public override string Describe() => Inner.Describe() + $" -> упаковка: {Total:F2}";
}

public sealed class LogisticsSettings
{
    private static readonly Lazy<LogisticsSettings> LazyInstance = new(() => new LogisticsSettings());
    private LogisticsSettings() { }
    public static LogisticsSettings Instance => LazyInstance.Value;
    public decimal ExpensiveOrderLimit { get; } = 10_000;
}

public static class VehicleFactory
{
    public static List<Vehicle> CreateDemoFleet() =>
    [
        new Truck("TR-01"),
        new Truck("TR-02"),
        new RefrigeratorTruck("RF-01"),
        new CargoPlane("PL-01"),
        new CargoShip("SH-01"),
        new DroneCourier("DR-01")
    ];
}
