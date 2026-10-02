namespace LogiCore.Domain;

public sealed class CargoValidator : IValidator<Cargo>
{
    public ValidationResult Validate(Cargo item)
    {
        if (item is PerishableCargo perishable && perishable.ExpirationDate.Date < DateTime.Today)
            return new ValidationResult(false, "Срок годности груза истёк.");
        return new ValidationResult(true);
    }
}

public sealed class CargoCompatibilityValidator
{
    private readonly IValidator<Cargo> _cargoValidator;

    public CargoCompatibilityValidator(IValidator<Cargo> cargoValidator) => _cargoValidator = cargoValidator;

    public void Validate(Vehicle vehicle, IReadOnlyCollection<Cargo> cargo)
    {
        ValidateCargoSet(cargo);

        if (cargo.Any(item => item is PerishableCargo) && vehicle is not RefrigeratorTruck)
            throw new IncompatibleCargoException("Скоропортящийся груз нужно перевозить в рефрижераторе.");

        decimal weight = cargo.Sum(item => item.WeightKg);
        decimal volume = cargo.Sum(item => item.VolumeM3);
        if (weight > vehicle.MaxLoadKg || volume > vehicle.MaxVolumeM3)
            throw new VehicleOverloadException($"Превышены лимиты транспорта {vehicle.RegistrationNumber}.");

        if (cargo.Any(item => !vehicle.CanCarry(item)))
            throw new IncompatibleCargoException($"Транспорт {vehicle.RegistrationNumber} не подходит.");
    }

    public void ValidateCargoSet(IReadOnlyCollection<Cargo> cargo)
    {
        foreach (Cargo item in cargo)
        {
            ValidationResult result = _cargoValidator.Validate(item);
            if (!result.IsValid) throw new CargoValidationException(result.Error);
        }

        if (cargo.Any(item => item is DangerousCargo) && cargo.Any(item => item is PerishableCargo))
            throw new IncompatibleCargoException("Опасный и скоропортящийся грузы несовместимы.");
    }
}

public sealed class OrderEventArgs : EventArgs
{
    public OrderEventArgs(Order order) => Order = order;
    public Order Order { get; }
}

public sealed class OrderStatusChangedEventArgs : EventArgs
{
    public OrderStatusChangedEventArgs(Order order, OrderStatus oldStatus) { Order = order; OldStatus = oldStatus; }
    public Order Order { get; }
    public OrderStatus OldStatus { get; }
}

public sealed class VehicleOverloadEventArgs : EventArgs
{
    public VehicleOverloadEventArgs(Order order) => Order = order;
    public Order Order { get; }
}

public delegate void OrderEventHandler(object sender, OrderEventArgs args);

public sealed class DeliveryService
{
    private readonly Repository<Vehicle> _vehicles;
    private readonly CargoCompatibilityValidator _validator;

    public DeliveryService(Repository<Vehicle> vehicles, CargoCompatibilityValidator validator)
    {
        _vehicles = vehicles;
        _validator = validator;
    }

    public event OrderEventHandler? OrderCreated;
    public event EventHandler<OrderStatusChangedEventArgs>? OrderStatusChanged;
    public event EventHandler<VehicleOverloadEventArgs>? VehicleOverloadAttempt;
    public event OrderEventHandler? DeliveryCompleted;
    public decimal Revenue { get; private set; }

    public Order CreateOrder(Customer customer, IEnumerable<Cargo> cargo, Route route)
    {
        var order = new Order(customer, cargo, route);
        OrderCreated?.Invoke(this, new OrderEventArgs(order));
        return order;
    }

    public void AssignCheapest(Order order, ITariffStrategy tariff)
    {
        _validator.ValidateCargoSet(order.Cargo);
        var offers = new List<(Vehicle Vehicle, decimal Cost)>();
        bool overloadFound = false;

        foreach (Vehicle vehicle in _vehicles.Where(item => item.State == VehicleState.Free))
        {
            try
            {
                _validator.Validate(vehicle, order.Cargo);
                decimal cost = tariff.Calculate(vehicle.CalculateDeliveryCost(order.Route, order.Cargo));
                offers.Add((vehicle, cost));
            }
            catch (VehicleOverloadException)
            {
                overloadFound = true;
            }
            catch (IncompatibleCargoException)
            {
                // Этот транспорт не подходит, проверяем следующий.
            }
        }

        if (offers.Count == 0)
        {
            if (overloadFound) VehicleOverloadAttempt?.Invoke(this, new VehicleOverloadEventArgs(order));
            throw new VehicleOverloadException("Подходящий транспорт не найден.");
        }

        OrderStatus oldStatus = order.Status;
        (Vehicle selectedVehicle, decimal selectedCost) = offers.OrderBy(item => item.Cost).First();
        order.Assign(selectedVehicle, selectedCost);
        OrderStatusChanged?.Invoke(this, new OrderStatusChangedEventArgs(order, oldStatus));
    }

    public void Start(Order order)
    {
        OrderStatus oldStatus = order.Status;
        order.StartDelivery();
        OrderStatusChanged?.Invoke(this, new OrderStatusChangedEventArgs(order, oldStatus));
    }

    public void Complete(Order order)
    {
        OrderStatus oldStatus = order.Status;
        order.Complete();
        Revenue += order.TotalCost;
        OrderStatusChanged?.Invoke(this, new OrderStatusChangedEventArgs(order, oldStatus));
        DeliveryCompleted?.Invoke(this, new OrderEventArgs(order));
    }
}
