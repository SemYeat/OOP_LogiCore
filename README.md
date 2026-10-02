# LogiCore

Программа моделирует работу транспортно-логистической компании: грузы, транспорт, заказы, подбор самой дешёвой машины, события, отчёты и сохранение состояния.


## Структура

```text
OOP_LogiCore/
├── LogiCore.Domain/   # Модель и бизнес-логика, не использует Console
├── LogiCore.App/      # Демо, меню, два подписчика на события
├── LogiCore.Tests/    # xUnit-тесты
├── Docs/              # UML и подробности
└── LogiCore.sln
```

## Набор на 60 баллов
Дменная модель - 15, T3 - 10, T4 - 10, T5 - 10, T6 - 5, T10 - 5, архитектура и оформление - 5. Это 60 баллов. Дополнительно реализованы простые варианты паттернов, LINQ-отчётов, JSON и тестов как запас.




## Быстрый запуск
Откройте `LogiCore.sln` в Rider и запустите проект `LogiCore.App`.

Из терминала:
```bash
dotnet restore --configfile NuGet.Config
dotnet build --no-restore
dotnet run --project LogiCore.App --no-build -- --demo-only
dotnet test --no-restore
```

## Соответствие требованиям

| Требование | Где реализовано |
| --- | --- |
| 3.1, T1, T2 - транспорт, инкапсуляция, полиморфизм | `Vehicle.cs`: абстрактный `Vehicle`, 5 наследников, `virtual`, `override`, `sealed`, `base.CanCarry`, приватный setter состояния |
| 3.2 - грузы и совместимость | `Cargo.cs`, `Services.cs`: 5 грузов и отдельный `CargoCompatibilityValidator` |
| 3.3 - клиент, маршрут, заказ | `Order.cs`, `Route.cs`: read-only история, `RoutePoint`, оператор `-`, машина состояний |
| 3.4 - диспетчер | `Services.cs`: выбор самого дешёвого свободного транспорта и учёт выручки |
| T3 - интерфейсы и вариантность | `Common.cs`, `Demo.cs`: 5 интерфейсов, явная реализация `IInsurable`, `out T`, `in T` и демонстрация присваивания |
| T4 - обобщения | `Repository.cs`: `Repository<T>`, индексатор, `Predicate<T>`, `yield return`, метод `ToReportTable<T>` |
| T5 - события | `Services.cs`, `Notifications.cs`: 4 события, собственный delegate, консоль и файл, отписка |
| T6 - исключения | `Exceptions.cs`, `Demo.cs`: иерархия исключений, `when`, `using`, `finally` |
| T7 - 5 паттернов | Strategy, Decorator, Factory, Observer и Singleton в `Patterns.cs` и `Services.cs` |
| T8 - LINQ | `Reports.cs`: 6 отчётов, query syntax, `Where`, `Select`, `OrderByDescending`, `GroupBy`, `Sum`, `Average`, `Count`, `ToDictionary`, `ToLookup` |
| T9 - JSON | `Persistence.cs`: простой DTO-снимок, обработка отсутствующего и повреждённого файла |
| T10 - enum и struct | `Common.cs`, `Route.cs`: `[Flags]`, побитовая операция, структура, оператор `-`, explicit string |
| Тесты | `DomainTests.cs`: 16 xUnit-тестов основных правил |
| UML | `Docs/LogiCore.puml` |

## Пять паттернов

1. **Strategy** - тариф передаётся в сервис через `ITariffStrategy`; можно выбрать обычный или срочный.
2. **Decorator** - страховка и упаковка по очереди добавляют цену к доставке.
3. **Factory** - `VehicleFactory` создаёт демонстрационный парк в одном месте.
4. **Observer** - `ConsoleNotifier` и `FileLogger` подписаны на события `DeliveryService`.
5. **Singleton** - `LogisticsSettings.Instance` хранит единственный экземпляр настроек через `Lazy<T>`.

При запуске приложение само показывает обязательный демо-сценарий. Без параметра `--demo-only` после него открывается простое меню.


