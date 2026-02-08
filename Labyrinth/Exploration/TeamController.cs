using Labyrinth.ApiClient;
using Labyrinth.Crawl;
using Labyrinth.Items;

namespace Labyrinth.Exploration
{
    public class TeamController
    {
        private readonly ContestSession _session;
        private SharedMap? _map;
        private readonly List<Task> _tasks = new();

        public SharedMap? Map => _map;

        public TeamController(ContestSession session) => _session = session;

        public async Task StartTeam(int maxActions = 5000)
        {
            Console.WriteLine($"Starting team with {maxActions} actions per crawler...\n");
            
            var crawlers = await _session.CreateCrawlers(3);
            if (crawlers.Count == 0)
            {
                Console.WriteLine("Failed to create crawlers!");
                return;
            }
            
            _map = new SharedMap(crawlers[0].X, crawlers[0].Y);

            for (int i = 0; i < crawlers.Count; i++)
            {
                int id = i;
                var crawler = crawlers[i];
                var bag = _session.Bags.ElementAt(id);
                var explorer = new WallFollowerExplorer(id, crawler, _map);

                _tasks.Add(Task.Run(async () =>
                {
                    // Décalage pour éviter collisions initiales
                    await Task.Delay(id * 200);
                    
                    try
                    {
                        var remaining = await explorer.GetOut(maxActions, bag);
                        if (remaining == 0)
                        {
                            Console.WriteLine($"✓ Explorer {id} completed mission");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"✗ Explorer {id} error: {ex.Message}");
                    }
                }));
            }
            
            await Task.WhenAll(_tasks);
            
            Console.WriteLine($"\nExploration complete! {_map.TileCount} tiles discovered");
        }

        public async Task CleanupExistingCrawlers()
        {
            foreach (var c in _session.Crawlers.OfType<ClientCrawler>())
            {
                try { await c.Delete(); } 
                catch { /* Ignore cleanup errors */ }
            }
        }

        public void DisplayMap()
        {
            if (_map == null) return;
            Console.WriteLine("\n" + _map.ExportToAscii());
        }

        public async Task ExportMap(string path)
        {
            if (_map == null) return;
            await File.WriteAllTextAsync(path, _map.ExportToAscii());
            Console.WriteLine($"Map exported to {path}");
        }

        public async Task Shutdown()
        {
            await _session.Close();
        }
    }
}