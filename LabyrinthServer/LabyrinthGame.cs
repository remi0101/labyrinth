using Labyrinth;
using Labyrinth.Build;
using Labyrinth.Crawl;
using Labyrinth.Items;
using Labyrinth.Tiles;
using Dto = ApiTypes;

namespace LabyrinthServer;

/// <summary>
/// Manages the game state for training server sessions.
/// </summary>
public class LabyrinthGame
{
    private readonly Labyrinth.Labyrinth _labyrinth;
    private readonly Dictionary<Guid, CrawlerState> _crawlers = new();
    private readonly object _lock = new();

    public LabyrinthGame()
    {
        // Créer un labyrinthe prédéfini pour l'entraînement
        _labyrinth = new Labyrinth.Labyrinth(new AsciiParser("""
            +--+--------+
            |  /        |
            |  +--+--+  |
            |     |k    |
            +--+  |  +--+
               |k  x    |
            +  +-------/|
            |           |
            +-----------+
            """));
    }

    public Dto.Crawler CreateCrawler(Guid appKey, Dto.Settings? settings)
    {
        lock (_lock)
        {
            var crawler = _labyrinth.NewCrawler();
            var crawlerId = Guid.NewGuid();
            var bag = new MyInventory();
            var items = new MyInventory();

            var state = new CrawlerState
            {
                Id = crawlerId,
                Crawler = crawler,
                Bag = bag,
                Items = items,
                AppKey = appKey
            };

            _crawlers[crawlerId] = state;

            return state.ToDto();
        }
    }

    public Dto.Crawler? UpdateCrawler(Guid crawlerId, Guid appKey, Dto.Crawler updates)
    {
        lock (_lock)
        {
            if (!_crawlers.TryGetValue(crawlerId, out var state))
                return null;

            if (state.AppKey != appKey)
                return null;

            // Mettre à jour la direction
            var newDirection = updates.Dir.GetCrawlerDirection();
            while (!state.Crawler.Direction.Equals(newDirection))
            {
                state.Crawler.Direction.TurnLeft();
            }

            // Tenter de marcher si demandé
            if (updates.Walking)
            {
                var result = state.Crawler.TryWalk(state.Bag).Result;
                if (result != null)
                {
                    // Transférer les items de la salle vers Items
                    state.Items = (MyInventory)result;
                }
            }

            return state.ToDto();
        }
    }

    public bool DeleteCrawler(Guid crawlerId, Guid appKey)
    {
        lock (_lock)
        {
            if (!_crawlers.TryGetValue(crawlerId, out var state))
                return false;

            if (state.AppKey != appKey)
                return false;

            return _crawlers.Remove(crawlerId);
        }
    }

    public Dto.InventoryItem[]? MoveItems(Guid crawlerId, Guid appKey, string inventoryType, Dto.InventoryItem[] itemsToMove)
    {
        lock (_lock)
        {
            if (!_crawlers.TryGetValue(crawlerId, out var state))
                return null;

            if (state.AppKey != appKey)
                return null;

            Inventory source = inventoryType.ToLower() switch
            {
                "bag" => state.Bag,
                "items" => state.Items,
                _ => throw new ArgumentException("Invalid inventory type")
            };

            Inventory destination = inventoryType.ToLower() switch
            {
                "bag" => state.Items,
                "items" => state.Bag,
                _ => throw new ArgumentException("Invalid inventory type")
            };

            var movesRequired = itemsToMove.Select(item => item.MoveRequired ?? false).ToList();
            var success = source.TryMoveItemsFrom(destination, movesRequired).Result;

            if (!success)
                return null;

            // Retourner l'état actuel de l'inventaire source
            return source.ItemTypes.Result
                .Select(t => new Dto.InventoryItem { Type = Dto.ItemType.Key, MoveRequired = false })
                .ToArray();
        }
    }

    private class CrawlerState
    {
        public required Guid Id { get; init; }
        public required ICrawler Crawler { get; init; }
        public required MyInventory Bag { get; set; }
        public required MyInventory Items { get; set; }
        public required Guid AppKey { get; init; }

        public Dto.Crawler ToDto()
        {
            var facingTile = Crawler.FacingTileType.Result;
            
            return new Dto.Crawler
            {
                Id = Id,
                X = Crawler.X,
                Y = Crawler.Y,
                Dir = Crawler.Direction.GetApiDirection(),
                Walking = false,
                FacingTile = facingTile.GetTileType(),
                Bag = Bag.ItemTypes.Result.Select(t => new Dto.InventoryItem 
                { 
                    Type = Dto.ItemType.Key, 
                    MoveRequired = false 
                }).ToArray(),
                Items = Items.ItemTypes.Result.Select(t => new Dto.InventoryItem 
                { 
                    Type = Dto.ItemType.Key, 
                    MoveRequired = false 
                }).ToArray()
            };
        }
    }
}
