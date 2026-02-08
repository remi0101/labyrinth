using Labyrinth.Exploration;
using Labyrinth.Tiles;

namespace LabyrinthTest
{
    [TestFixture]
    public class SharedMapTest
    {
        private static SharedMap NewMap(int x = 0, int y = 0) => new SharedMap(x, y);

        private static void RegisterLocked(SharedMap m, (int X, int Y) coord, int visits = 0)
        {
            m.RegisterLockedDoor(coord.X, coord.Y);
            for (int i = 0; i < visits; i++) m.MarkVisit(coord.X, coord.Y);
        }

        [Test]
        public void Constructor_marks_initial_tile_as_room()
        {
            var x = 2; var y = 3;
            
            var map = NewMap(x, y);
            var info = map.GetTileInfo(x, y);
            
            Assert.That(map.TileCount, Is.EqualTo(1));
            Assert.That(info, Is.Not.Null);
            Assert.That(info!.TileType, Is.EqualTo(typeof(Room)));
        }

        [Test]
        public void MarkVisit_increments_visit_count()
        {
            var map = NewMap();
            
            map.MarkVisit(0, 0);
            map.MarkVisit(0, 0);
            
            Assert.That(map.GetVisitCount(0, 0), Is.EqualTo(2));
        }

        [Test]
        public void Register_and_remove_locked_door_updates_lock_state()
        {
            var map = NewMap();
            
            map.RegisterLockedDoor(1, 1);
            map.RemoveLockedDoor(1, 1);
            
            Assert.That(map.IsDoorLocked(1, 1), Is.False);
        }

        [Test]
        public void GetNearestLockedDoor_returns_closest_by_manhattan_distance()
        {
            var map = NewMap();
            map.RegisterLockedDoor(0, 0);
            map.RegisterLockedDoor(5, 5);
            map.RegisterLockedDoor(1, 2);

            var nearest = map.GetNearestLockedDoor(2, 2);

            Assert.That(nearest.HasValue, Is.True);
            Assert.That(nearest.Value, Is.EqualTo((1, 2)));
        }

        [Test]
        public void GetBestLockedDoor_prefers_less_visited_then_closer()
        {
            var map = NewMap();
            RegisterLocked(map, (0, 0), visits: 1); // best candidate
            RegisterLocked(map, (2, 0), visits: 1);
            RegisterLocked(map, (1, 1), visits: 2);

            var best = map.GetBestLockedDoor(0, 0);

            Assert.That(best.HasValue, Is.True);
            Assert.That(best.Value, Is.EqualTo((0, 0)));
        }
        
        [Test]
        public void GetBestLockedDoor_with_equal_visits_and_distance_returns_valid_candidate()
        {
            var map = NewMap();
            RegisterLocked(map, (2, 0), visits: 1);
            RegisterLocked(map, (-2, 0), visits: 1);

            var best = map.GetBestLockedDoor(0, 0);

            Assert.That(best.HasValue, Is.True);
            var candidates = new[] { (2, 0), (-2, 0) };
            Assert.That(candidates, Does.Contain(best.Value));
        }
        
        [Test]
        public void GetNearestLockedDoor_tie_breaks_by_registration_order()
        {
            var map = NewMap();
            map.RegisterLockedDoor(1, 0);   // registered first
            map.RegisterLockedDoor(-1, 0);  // same manhattan distance from (0,0)

            var nearest = map.GetNearestLockedDoor(0, 0);

            Assert.That(nearest.HasValue, Is.True);
            Assert.That(nearest.Value, Is.EqualTo((1, 0)));
        }
        
        [Test]
        public void NoLockedDoors_GetNearestAndGetBestReturnNone()
        {
            var map = NewMap();

            var nearest = map.GetNearestLockedDoor(0, 0);
            var best = map.GetBestLockedDoor(0, 0);

            Assert.That(nearest.HasValue, Is.False);
            Assert.That(best.HasValue, Is.False);
        }
    }
}
