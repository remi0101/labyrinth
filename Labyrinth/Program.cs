using Labyrinth.ApiClient;
using Labyrinth.Exploration;

if (args.Length < 2)
{
    Console.WriteLine("Usage: Labyrinth <serverUrl> <appKey> [maxActions]");
    return 1;
}

var serverUrl = new Uri(args[0]);
var appKey = Guid.Parse(args[1]);
var maxActions = args.Length > 2 ? int.Parse(args[2]) : 1000;

Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
Console.WriteLine("║            LABYRINTH EXPLORATION CLIENT                    ║");
Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
Console.WriteLine($"Server     : {serverUrl}");
Console.WriteLine($"App Key    : {appKey}");
Console.WriteLine($"Max Actions: {maxActions} per crawler\n");

ContestSession? session = null;
TeamController? team = null;

try
{
    Console.WriteLine("Connecting to server...");
    session = await ContestSession.Open(serverUrl, appKey);
    Console.WriteLine("Connected successfully!\n");

    team = new TeamController(session);

    Console.WriteLine("Starting exploration...");
    await team.StartTeam(maxActions);

    // Affichage de la carte
    if (team.Map != null)
    {
        Console.WriteLine(team.Map.ExportToAscii());
        await File.WriteAllTextAsync("map.txt", team.Map.ExportToAscii());
        Console.WriteLine("\nMap exported to map.txt");
    }

    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"\n[ERROR] {ex.GetType().Name}: {ex.Message}");
    return 1;
}
finally
{
    if (session != null)
    {
        Console.WriteLine("Closing session...");
        await session.Close();
    }
}