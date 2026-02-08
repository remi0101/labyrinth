using Labyrinth.Crawl;
using Labyrinth.Items;
using Labyrinth.Tiles;

namespace Labyrinth
{
    public partial class Labyrinth
    {
        private class LabyrinthCrawler(int x, int y, Tile[,] tiles) : ICrawler
        {
            public int X => _x;

            public int Y => _y;

            public Task<Type> FacingTileType => Task.FromResult(ProcessFacingTile((x, y, tile) => tile.GetType()));

            Direction ICrawler.Direction => _direction;

            public async Task<Inventory?> TryWalk(Inventory walkerInventory)
            {
                return await ProcessFacingTileAsync(async (facingX, facingY, tile) =>
                {
                    Inventory? tileContent = null;

                    if (tile is Door door)
                    {
                        await Open(door, walkerInventory);
                    }
                    if (tile.IsTraversable)
                    {
                        tileContent = tile.Pass();
                        _x = facingX;
                        _y = facingY;
                    }
                    return tileContent;
                });
            }
            
            private async Task<bool> Open(Door door, Inventory walkerInventory)
            {
                if (walkerInventory is not LocalInventory keyRing)
                {
                    throw new NotSupportedException("Local inventories only");
                }
                var itemTypes = await walkerInventory.ItemTypes;
                for(var maxKeys = itemTypes.Count(); maxKeys > 0; maxKeys--)
                {
                    if (await door.Open(keyRing))
                    {
                        return true;
                    }
                }
                return false;
            }

            private bool IsOut(int pos, int dimension) =>
                pos < 0 || pos >= _tiles.GetLength(dimension);

            private T ProcessFacingTile<T>(Func<int, int, Tile, T> process)
            {
                int facingX = _x + _direction.DeltaX,
                    facingY = _y + _direction.DeltaY;

                return process(
                    facingX, facingY,
                    IsOut(facingX, dimension: 0) ||
                    IsOut(facingY, dimension: 1)
                        ? Outside.Singleton
                        : _tiles[facingX, facingY]
                 );
            }

            private async Task<T> ProcessFacingTileAsync<T>(Func<int, int, Tile, Task<T>> process)
            {
                int facingX = _x + _direction.DeltaX,
                    facingY = _y + _direction.DeltaY;

                return await process(
                    facingX, facingY,
                    IsOut(facingX, dimension: 0) ||
                    IsOut(facingY, dimension: 1)
                        ? Outside.Singleton
                        : _tiles[facingX, facingY]
                 );
            }

            private int _x = x;
            private int _y = y;

            private readonly Direction _direction = Direction.North;
            private readonly Tile[,] _tiles = tiles;
        }
    }
}
