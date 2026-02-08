using Labyrinth.Crawl;
using Labyrinth.Items;
using Labyrinth.Tiles;

namespace Labyrinth.Exploration
{
    public class WallFollowerExplorer
    {
        private readonly int _id;
        private readonly ICrawler _crawler;
        private readonly SharedMap _map;
        private (int X, int Y)? _targetDoor = null;
        private List<Direction>? _pathToDoor = null;
        private readonly HashSet<(int, int)> _passedDoors = new();
        private int _stuckCounter = 0;
        public bool HasKey { get; private set; } = false;

        public WallFollowerExplorer(int id, ICrawler crawler, SharedMap map)
        {
            _id = id;
            _crawler = crawler;
            _map = map;
        }

        public async Task<int> GetOut(int maxActions, Inventory? bag = null)
        {
            bag ??= new MyInventory();
            
            // Rotation initiale pour disperser les crawlers
            for (int i = 0; i < _id; i++) _crawler.Direction.TurnRight();
            
            for (; maxActions > 0; maxActions--)
            {
                _map.MarkVisit(_crawler.X, _crawler.Y);
                _map.MarkAsDiscovered(_crawler.X, _crawler.Y, typeof(Room));

                var facingTileType = await _crawler.FacingTileType;
                var (facingX, facingY) = GetFacingPosition();
                
                if (facingTileType == typeof(Outside))
                {
                    Console.WriteLine($"✓ Explorer {_id} EXIT FOUND at ({_crawler.X},{_crawler.Y})!");
                    return 0;
                }

                // Si on a une clé, chercher une porte à ouvrir
                if (HasKey && _targetDoor == null)
                {
                    _targetDoor = _map.GetBestLockedDoor(_crawler.X, _crawler.Y);
                    if (_targetDoor.HasValue)
                    {
                        _pathToDoor = FindPath(_targetDoor.Value);
                        if (_pathToDoor == null)
                        {
                            _targetDoor = null; // Pas de chemin trouvé
                        }
                    }
                }

                bool moved = false;
                
                // Suivre le chemin vers une porte si on en a un
                if (_pathToDoor != null && _pathToDoor.Count > 0)
                {
                    moved = await FollowPathToTarget(bag);
                    if (!moved)
                    {
                        // Chemin bloqué, abandonner cette porte
                        _pathToDoor = null;
                        _targetDoor = null;
                    }
                }
                
                // Sinon, exploration avec règle du mur droit
                if (!moved)
                {
                    moved = await RightHandRule(bag, facingTileType, facingX, facingY);
                }

                // Détection de blocage
                if (!moved)
                {
                    _stuckCounter++;
                    if (_stuckCounter > 10)
                    {
                        // Faire demi-tour si vraiment coincé
                        _crawler.Direction.TurnRight();
                        _crawler.Direction.TurnRight();
                        _stuckCounter = 0;
                    }
                }
                else
                {
                    _stuckCounter = 0;
                }
                
                _map.MarkAsDiscovered(facingX, facingY, facingTileType);
            }
            return maxActions;
        }

        private async Task<bool> RightHandRule(Inventory bag, Type facingTileType, int facingX, int facingY)
        {
            // 1. Essayer à droite
            _crawler.Direction.TurnRight();
            var rightTile = await _crawler.FacingTileType;
            var (rx, ry) = GetFacingPosition();

            if (rightTile != typeof(Wall))
            {
                if (await _crawler.TryWalk(bag) is Inventory content)
                {
                    await PostMoveProcessing(bag, content, rightTile, rx, ry);
                    return true;
                }
                if (rightTile == typeof(Door)) _map.RegisterLockedDoor(rx, ry);
            }

            // 2. Essayer tout droit
            _crawler.Direction.TurnLeft();
            
            if (facingTileType == typeof(Door))
            {
                if (await _crawler.TryWalk(bag) is Inventory content)
                {
                    await PostMoveProcessing(bag, content, facingTileType, facingX, facingY);
                    return true;
                }
                _map.RegisterLockedDoor(facingX, facingY);
                _crawler.Direction.TurnLeft();
                return false;
            }
            
            if (facingTileType != typeof(Wall))
            {
                if (await _crawler.TryWalk(bag) is Inventory content)
                {
                    await PostMoveProcessing(bag, content, facingTileType, facingX, facingY);
                    return true;
                }
            }

            // 3. Tourner à gauche
            _crawler.Direction.TurnLeft();
            return false;
        }

        private async Task PostMoveProcessing(Inventory bag, Inventory content, Type tileType, int x, int y)
        {
            await CollectItems(bag, content);
            _map.MarkAsDiscovered(x, y, tileType);

            if (tileType == typeof(Door))
            {
                bool wasAlreadyOpen = !_map.IsDoorLocked(x, y);
                _map.RemoveLockedDoor(x, y);

                // Si porte déjà ouverte, diverger pour explorer autre chose
                if (wasAlreadyOpen && !_passedDoors.Contains((x, y)))
                {
                    _crawler.Direction.TurnRight();
                    _crawler.Direction.TurnRight();
                }
                _passedDoors.Add((x, y));
            }
        }

        private async Task<bool> FollowPathToTarget(Inventory bag)
        {
            if (_pathToDoor == null || _pathToDoor.Count == 0) return false;
            
            var nextDir = _pathToDoor[0];
            while (!_crawler.Direction.Equals(nextDir)) _crawler.Direction.TurnLeft();
            
            var tileType = await _crawler.FacingTileType;
            var (fx, fy) = GetFacingPosition();
            
            if (tileType == typeof(Wall)) return false;

            if (await _crawler.TryWalk(bag) is Inventory content)
            {
                await PostMoveProcessing(bag, content, tileType, fx, fy);
                _pathToDoor.RemoveAt(0);
                
                // Si on a atteint la porte
                if (_pathToDoor.Count == 0) _targetDoor = null;
                
                return true;
            }
            return false;
        }

        private async Task CollectItems(Inventory bag, Inventory content)
        {
            var itemTypes = (await content.ItemTypes).ToList();
            if (itemTypes.Any())
            {
                if (await bag.TryMoveItemsFrom(content, itemTypes.Select(_ => true).ToList()))
                {
                    HasKey = true;
                }
            }
        }

        private List<Direction>? FindPath((int X, int Y) target)
        {
            var queue = new Queue<(int X, int Y, List<Direction> Path)>();
            var visited = new HashSet<(int, int)> { (_crawler.X, _crawler.Y) };
            queue.Enqueue((_crawler.X, _crawler.Y, new List<Direction>()));
            
            var dirs = new[] 
            { 
                (Direction.North, 0, -1), 
                (Direction.East, 1, 0), 
                (Direction.South, 0, 1), 
                (Direction.West, -1, 0) 
            };

            while (queue.Count > 0 && queue.Count < 2000) // Limite pour performances
            {
                var (x, y, path) = queue.Dequeue();
                
                if (x == target.X && y == target.Y) return path;
                
                foreach (var (d, dx, dy) in dirs)
                {
                    var (nx, ny) = (x + dx, y + dy);
                    
                    if (visited.Contains((nx, ny))) continue;
                    
                    var tile = _map.GetTileInfo(nx, ny);
                    if (tile == null || tile.TileType == typeof(Wall)) continue;
                    
                    // Éviter les portes fermées sauf si c'est notre cible
                    if (tile.TileType == typeof(Door) && 
                        (nx != target.X || ny != target.Y) && 
                        _map.IsDoorLocked(nx, ny)) 
                        continue;
                    
                    visited.Add((nx, ny));
                    queue.Enqueue((nx, ny, new List<Direction>(path) { d }));
                }
            }
            return null;
        }

        private (int X, int Y) GetFacingPosition() => 
            (_crawler.X + _crawler.Direction.DeltaX, _crawler.Y + _crawler.Direction.DeltaY);
    }
}