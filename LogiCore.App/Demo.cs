using LogiCore.Domain;

namespace LogiCore.App;

public static class Demo
{
    public static void Run()
    {
        Console.WriteLine("=== LogiCore: автоматический демонстрационный сценарий ===");
        var vehicles = new Repository<Vehicle>();
        foreach (Vehicle vehicle in VehicleFactory.CreateDemoFleet()) vehicles.Add(vehicle);

        var orders = new Repository<Order>();
        var customers = new List<Customer> { new("Анна", "+7 900 111-11-11"), new("Борис", "+7 900 222-22-22") };
        var service = new DeliveryService(vehicles, new CargoCompatibilityValidator(new CargoValidator()));
        var console = new ConsoleNotifier();
        service.OrderCreated += console.OnCreated;
        service.OrderStatusChanged += console.OnStatusChanged;
        service.VehicleOverloadAttempt += console.OnOverload;
        service.DeliveryCompleted += console.OnCompleted;

        string logPath = Path.Combine(AppContext.BaseDirectory, "logs", "deliveries.log");
        using var logger = new FileLogger(logPath);
        SubscribeLogger(service, logger);

        try
        {
            Route route = new([new RoutePoint(55.7500, 37.6100), new RoutePoint(55.8000, 37.6500)]);
            DateTime tomorrow = DateTime.Today.AddDays(1);
            Cargo[] cargo =
            [
                new StandardCargo("Книги", 100, 1, 20_000), new StandardCargo("Документы", 2, 0.02m, 5_000),
                new PerishableCargo("Молоко", 500, 3, 60_000, tomorrow, 4), new PerishableCargo("Овощи", 300, 2, 30_000, tomorrow, 6),
                new FragileCargo("Стекло", 200, 2, 80_000, 1.5m), new FragileCargo("Мониторы", 150, 4, 150_000, 1.2m),
                new DangerousCargo("Краска", 100, 1, 10_000, 3), new DangerousCargo("Газ", 80, 1, 20_000, 2),
                new OversizedCargo("Станок", 5_000, 30, 500_000), new OversizedCargo("Трубы", 3_000, 20, 200_000)
            ];

            Deliver(service, orders, customers[0], [cargo[0], cargo[4]], route, new StandardTariff());
            Deliver(service, orders, customers[1], [cargo[2]], route, new ExpressTariff());
            Deliver(service, orders, customers[0], [cargo[6]], route, new StandardTariff());
            Deliver(service, orders, customers[1], [cargo[8]], route, new StandardTariff());
            Deliver(service, orders, customers[0], [cargo[1]], route, new ExpressTariff());
            ShowInvalidOrder(service, orders, customers[0], [cargo[3], cargo[7]], route);
            ShowOverload(service, orders, customers[1], route);
            ShowDecorators();
            ShowReports(orders, customers);
            ShowSerialization(vehicles, customers, orders, service.Revenue);

            IValidator<Cargo> cargoValidator = new CargoValidator();
            IValidator<PerishableCargo> perishableValidator = cargoValidator;
            IReadOnlyRepository<Vehicle> readOnlyVehicles = vehicles;
            Console.WriteLine($"Вариантность: {perishableValidator.Validate((PerishableCargo)cargo[2]).IsValid}; ТС: {readOnlyVehicles.GetAll().Count()}");
            TransportConditions conditions = TransportConditions.Refrigerated | TransportConditions.Sealed;
            Console.WriteLine($"Flags: {conditions}; охлаждение: {conditions.HasFlag(TransportConditions.Refrigerated)}");
        }
        finally
        {
            UnsubscribeLogger(service, logger);
        }
    }

    private static void Deliver(DeliveryService service, Repository<Order> orders, Customer customer, Cargo[] cargo, Route route, ITariffStrategy tariff)
    {
        Order order = service.CreateOrder(customer, cargo, route);
        orders.Add(order);
        service.AssignCheapest(order, tariff);
        service.Start(order);
        service.Complete(order);
    }

    private static void ShowInvalidOrder(DeliveryService service, Repository<Order> orders, Customer customer, Cargo[] cargo, Route route)
    {
        try
        {
            Order order = service.CreateOrder(customer, cargo, route);
            orders.Add(order);
            service.AssignCheapest(order, new StandardTariff());
        }
        catch (IncompatibleCargoException exception) when (exception.Message.Contains("несовместимы"))
        {
            Console.WriteLine($"Ожидаемая ошибка совместимости: {exception.Message}");
        }
    }

    private static void ShowOverload(DeliveryService service, Repository<Order> orders, Customer customer, Route route)
    {
        try
        {
            var huge = new OversizedCargo("Сверхтяжёлый генератор", 2_000_000, 60_000, 2_000_000);
            Order order = service.CreateOrder(customer, [huge], route);
            orders.Add(order);
            service.AssignCheapest(order, new StandardTariff());
        }
        catch (VehicleOverloadException exception)
        {
            Console.WriteLine($"Ожидаемая перегрузка: {exception.Message}");
        }
    }

    private static void ShowDecorators()
    {
        IDeliveryCost price = new BasicDeliveryCost(1000);
        price = new InsuranceDecorator(price, 20_000);
        price = new PackingDecorator(price);
        Console.WriteLine("Декораторы: " + price.Describe());
    }

    private static void ShowReports(Repository<Order> orders, List<Customer> customers)
    {
        Console.WriteLine("\n--- 6 LINQ-отчётов ---");
        Print("Топ транспорта", Reports.TopVehicles(orders));
        Print("По статусам", Reports.OrdersByStatus(orders));
        Print("Средняя загрузка", Reports.AverageLoadByVehicleType(orders));
        Print("Клиенты выше порога", Reports.ExpensiveCustomers(customers, 100));
        Print("Груз -> заказ -> клиент", Reports.CargoOrderCustomer(orders).Take(5));
        Print("Опасные грузы", Reports.DangerousCargoCount(orders).Select(item => $"Класс {item.Key}: {item.Value}"));
    }

    private static void Print(string title, IEnumerable<string> lines)
    {
        Console.WriteLine(title + ":");
        Console.WriteLine(lines.DefaultIfEmpty("нет данных").ToReportTable());
    }

    private static void ShowSerialization(Repository<Vehicle> vehicles, List<Customer> customers, Repository<Order> orders, decimal revenue)
    {
        var snapshot = new SystemSnapshot(vehicles.Count(), customers.Count, orders.Count(), revenue);
        string path = Path.Combine(AppContext.BaseDirectory, "state.json");
        StateStorage.Save(path, snapshot);
        SystemSnapshot loaded = StateStorage.Load(path);
        Console.WriteLine($"JSON: загружено {loaded.OrderCount} заказов, выручка {loaded.Revenue:F2}");
    }

    private static void SubscribeLogger(DeliveryService service, FileLogger logger)
    {
        service.OrderCreated += logger.OnCreated;
        service.OrderStatusChanged += logger.OnStatusChanged;
        service.VehicleOverloadAttempt += logger.OnOverload;
        service.DeliveryCompleted += logger.OnCompleted;
    }

    private static void UnsubscribeLogger(DeliveryService service, FileLogger logger)
    {
        service.OrderCreated -= logger.OnCreated;
        service.OrderStatusChanged -= logger.OnStatusChanged;
        service.VehicleOverloadAttempt -= logger.OnOverload;
        service.DeliveryCompleted -= logger.OnCompleted;
    }
}

public static class Menu
{
    public static void Run()
    {
        while (true)
        {
            Console.Write("\n1 - повторить демо, 0 - выход: ");
            string? command = Console.ReadLine();
            if (command == "0" || command is null) return;
            if (command == "1") Demo.Run();
        }
    }
}
