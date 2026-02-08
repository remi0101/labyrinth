using Labyrinth.Tiles;
using System.Collections.Concurrent;

namespace Labyrinth.Exploration
{
    public class SharedMap
    {
        private readonly ConcurrentDictionary<(int X, int Y), TileInfo> _tiles = new();
        private readonly ConcurrentDictionary<(int X, int Y), int> _visits = new();
        private readonly List<(int X, int Y)> _lockedDoors = new();
        private readonly object _lock = new();

        public int TileCount => _tiles.Count;

        public SharedMap(int x, int y) => MarkAsDiscovered(x, y, typeof(Room));

        public void MarkAsDiscovered(int x, int y, Type type) => 
            _tiles[(x, y)] = new TileInfo { X = x, Y = y, TileType = type };

        public void MarkVisit(int x, int y) => 
            _visits.AddOrUpdate((x, y), 1, (_, c) => c + 1);

        public int GetVisitCount(int x, int y) => 
            _visits.TryGetValue((x, y), out var count) ? count : 0;

        public void RegisterLockedDoor(int x, int y) 
        { 
            lock (_lock) 
            { 
                if (!_lockedDoors.Contains((x, y))) 
                {
                    _lockedDoors.Add((x, y)); 
                }
            } 
        }

        public bool IsDoorLocked(int x, int y) 
        { 
            lock (_lock) return _lockedDoors.Contains((x, y)); 
        }

        public (int X, int Y)? GetNearestLockedDoor(int fx, int fy)
        {
            lock (_lock)
            {
                return _lockedDoors
                    .OrderBy(d => Math.Abs(d.X - fx) + Math.Abs(d.Y - fy))
                    .Cast<(int, int)?>()
                    .FirstOrDefault();
            }
        }

        public (int X, int Y)? GetBestLockedDoor(int fx, int fy)
        {
            lock (_lock)
            {
                if (_lockedDoors.Count == 0) return null;
                
                // Prioriser les portes moins visitées et plus proches
                return _lockedDoors
                    .Select(d => new 
                    { 
                        Door = d,
                        Distance = Math.Abs(d.X - fx) + Math.Abs(d.Y - fy),
                        Visits = GetVisitCount(d.X, d.Y)
                    })
                    .OrderBy(d => d.Visits) // Moins visitée d'abord
                    .ThenBy(d => d.Distance) // Puis la plus proche
                    .Select(d => (d.Door.X, d.Door.Y))
                    .Cast<(int, int)?>()
                    .FirstOrDefault();
            }
        }

        public void RemoveLockedDoor(int x, int y) 
        { 
            lock (_lock) 
            { 
                _lockedDoors.Remove((x, y)); 
            } 
        }

        public TileInfo? GetTileInfo(int x, int y) => 
            _tiles.TryGetValue((x, y), out var info) ? info : null;

        public string ExportToAscii()
        {
            if (!_tiles.Any()) return "Empty map";
            
            var minX = _tiles.Keys.Min(k => k.X);
            var maxX = _tiles.Keys.Max(k => k.X);
            var minY = _tiles.Keys.Min(k => k.Y);
            var maxY = _tiles.Keys.Max(k => k.Y);
            
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Map ({maxX - minX + 1}x{maxY - minY + 1}) - {TileCount} tiles");
            sb.AppendLine();
            
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (!_tiles.TryGetValue((x, y), out var t))
                    {
                        sb.Append('?');
                    }
                    else
                    {
                        sb.Append(t.TileType.Name switch
                        {
                            "Room" => ' ',
                            "Wall" => '#',
                            "Door" => '/',
                            "Outside" => 'X',
                            _ => '.'
                        });
                    }
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }

    public class TileInfo 
    { 
        public required int X { get; init; }
        public required int Y { get; init; }
        public required Type TileType { get; init; }
    }
}