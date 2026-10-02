using LogiCore.Domain;

namespace LogiCore.Tests;

public sealed class DomainTests
{
    private static Route ShortRoute() => new([new RoutePoint(0, 0), new RoutePoint(0, 0.1)]);

    [Fact] public void CargoRejectsZeroWeight() => Assert.Throws<CargoValidationException>(() => new StandardCargo("A", 0, 1, 1));
    [Fact] public void DangerousCargoRejectsWrongClass() => Assert.Throws<CargoValidationException>(() => new DangerousCargo("A", 1, 1, 1, 10));
    [Fact] public void RoutePointOperatorReturnsDistance() => Assert.True(new RoutePoint(0, 1) - new RoutePoint(0, 0) > 100);
    [Fact] public void RouteNeedsTwoPoints() => Assert.Throws<RouteNotFoundException>(() => new Route([new RoutePoint(0, 0)]));

    [Fact]
    public void TruckCostIsZeroForZeroDistance()
    {
        var route = new Route([new RoutePoint(0, 0), new RoutePoint(0, 0)]);
        Assert.Equal(0, new Truck("T").CalculateDeliveryCost(route, [new StandardCargo("A", 1, 1, 1)]));
    }

    [Fact]
    public void RefrigeratorChecksTemperature()
    {
        var cargo = new PerishableCargo("A", 1, 1, 1, DateTime.Today.AddDays(1), 20);
        Assert.False(new RefrigeratorTruck("R").CanCarry(cargo));
    }

    [Fact] public void PlaneRejectsHighHazardClass() => Assert.False(new CargoPlane("P").CanCarry(new DangerousCargo("A", 1, 1, 1, 5)));

    [Fact]
    public void OrderPassesFullStateMachine()
    {
        var order = new Order(new Customer("A", "1"), [new StandardCargo("A", 1, 0.1m, 1)], ShortRoute());
        order.Assign(new Truck("T"), 100);
        order.StartDelivery();
        order.Complete();
        Assert.Equal(OrderStatus.Delivered, order.Status);
    }

    [Fact]
    public void OrderRejectsWrongTransition()
    {
        var order = new Order(new Customer("A", "1"), [new StandardCargo("A", 1, 1, 1)], ShortRoute());
        Assert.Throws<InvalidOrderStateException>(order.StartDelivery);
    }

    [Fact]
    public void RepositoryAddsAndFindsByIndex()
    {
        var repository = new Repository<Customer>();
        var customer = new Customer("A", "1");
        repository.Add(customer);
        Assert.Same(customer, repository[customer.Id]);
    }

    [Fact]
    public void RepositoryRemovesAndFinds()
    {
        var repository = new Repository<Customer>();
        var customer = new Customer("A", "1");
        repository.Add(customer);
        Assert.Single(repository.FindAll(item => item.Name == "A"));
        Assert.True(repository.Remove(customer));
        Assert.Empty(repository);
    }

    [Fact]
    public void ValidatorRejectsDangerousWithPerishable()
    {
        Cargo[] cargo = [new DangerousCargo("D", 1, 1, 1, 1), new PerishableCargo("P", 1, 1, 1, DateTime.Today.AddDays(1), 2)];
        var validator = new CargoCompatibilityValidator(new CargoValidator());
        Assert.Throws<IncompatibleCargoException>(() => validator.Validate(new RefrigeratorTruck("R"), cargo));
    }

    [Fact]
    public void ValidatorRejectsExpiredCargo()
    {
        Cargo cargo = new PerishableCargo("P", 1, 1, 1, DateTime.Today.AddDays(-1), 2);
        Assert.False(new CargoValidator().Validate(cargo).IsValid);
    }

    [Fact]
    public void DecoratorsAddTheirPrices()
    {
        IDeliveryCost price = new BasicDeliveryCost(1000);
        price = new InsuranceDecorator(price, 10_000);
        price = new PackingDecorator(price);
        Assert.Equal(1600, price.Total);
    }

    [Fact]
    public void SerializationRoundTripKeepsData()
    {
        string path = Path.Combine(Path.GetTempPath(), $"logicore-{Guid.NewGuid()}.json");
        try
        {
            var expected = new SystemSnapshot(6, 2, 5, 1234);
            StateStorage.Save(path, expected);
            Assert.Equal(expected, StateStorage.Load(path));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void FlagsCanBeCombined()
    {
        TransportConditions value = TransportConditions.Refrigerated | TransportConditions.Sealed;
        Assert.True(value.HasFlag(TransportConditions.Refrigerated));
    }
}
