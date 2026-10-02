using System.Text.Json;

namespace LogiCore.Domain;

public record SystemSnapshot(int VehicleCount, int CustomerCount, int OrderCount, decimal Revenue);

public static class StateStorage
{
    public static void Save(string path, SystemSnapshot snapshot)
    {
        string json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    public static SystemSnapshot Load(string path)
    {
        if (!File.Exists(path)) throw new LogisticsException("Файл состояния не найден.");
        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<SystemSnapshot>(json)
                   ?? throw new LogisticsException("Файл состояния пуст.");
        }
        catch (JsonException)
        {
            throw new LogisticsException("Файл состояния повреждён.");
        }
    }
}
