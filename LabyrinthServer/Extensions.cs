using Labyrinth.Crawl;
using Labyrinth.Tiles;
using Dto = ApiTypes;

namespace LabyrinthServer;

public static class Extensions
{
    public static Dto.Direction GetApiDirection(this Direction dir) =>
        (Dto.Direction)(dir.DeltaY + 1 + dir.DeltaX * (dir.DeltaX - 1));

    public static Direction GetCrawlerDirection(this Dto.Direction dtoDir)
    {
        return dtoDir switch
        {
            Dto.Direction.North => Direction.North,
            Dto.Direction.East => Direction.East,
            Dto.Direction.South => Direction.South,
            Dto.Direction.West => Direction.West,
            _ => Direction.North
        };
    }

    public static Dto.TileType GetTileType(this Type tileType)
    {
        if (tileType == typeof(Outside)) return Dto.TileType.Outside;
        if (tileType == typeof(Room)) return Dto.TileType.Room;
        if (tileType == typeof(Wall)) return Dto.TileType.Wall;
        if (tileType == typeof(Door)) return Dto.TileType.Door;
        return Dto.TileType.Outside;
    }
}
