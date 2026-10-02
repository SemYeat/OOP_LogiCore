namespace LogiCore.App;

internal static class Program
{
    private static void Main(string[] args)
    {
        Demo.Run();
        if (!args.Contains("--demo-only")) Menu.Run();
    }
}
