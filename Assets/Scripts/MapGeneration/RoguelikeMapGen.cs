// RoguelikeMapGen.cs
// Pure C# (headless) implementation of the map generation described in the reference.
// Designed to be copy-paste ready for a Unity project later (no Unity dependencies).
// Reference used for algorithm outline: KosGames Map Generation Guide. fileciteturn1file0

using System;
using System.Collections.Generic;
using System.Linq;

namespace RoguelikeMapGen
{
    public enum RoomType
    {
        None,
        Monster,
        Elite,
        Event,
        Shop,
        Rest,
        Boss
    }

    public class Room
    {
        public int Id { get; }
        public int X { get; }   // column 0..6
        public int Y { get; }   // floor 0..14 (0 is bottom)
        public List<int> Outgoing { get; } = new List<int>();
        public List<int> Incoming { get; } = new List<int>();
        public RoomType Type { get; set; } = RoomType.None;

        public Room(int id, int x, int y)
        {
            Id = id;
            X = x;
            Y = y;
        }

        public override string ToString()
        {
            return $"Room(Id={Id}, x={X}, y={Y}, type={Type}, out={Outgoing.Count})";
        }
    }

    public class MapGraph
    {
        // rooms by id
        public Dictionary<int, Room> Rooms { get; } = new Dictionary<int, Room>();
        
        // Dimensions for the standard map floors (0-14)
        public const int Width = 7;
        public const int Height = 15;
        
        // quick lookup grid [x,y] -> roomId or -1
        public int[,] Grid = new int[Width, Height];

        public MapGraph()
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    Grid[x, y] = -1;
        }

        public void AddRoom(Room r)
        {
            Rooms[r.Id] = r;

            if (r.X >= 0 && r.X < Width && r.Y >= 0 && r.Y < Height)
            {
                Grid[r.X, r.Y] = r.Id;
            }
        }

        public Room GetRoomAt(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return null;
            int id = Grid[x, y];
            return id == -1 ? null : Rooms[id];
        }

        public IEnumerable<Room> RoomsOnFloor(int y)
        {
            return Rooms.Values.Where(r => r.Y == y).OrderBy(r => r.X);
        }

        public void RemoveRoom(int id)
        {
            if (!Rooms.ContainsKey(id)) return;
            var r = Rooms[id];
            
            // Only try to remove from grid if it was in grid
            if (r.X >= 0 && r.X < Width && r.Y >= 0 && r.Y < Height)
            {
                Grid[r.X, r.Y] = -1;
            }

            // remove connections referencing it
            foreach (var other in Rooms.Values)
            {
                other.Outgoing.RemoveAll(o => o == id);
                other.Incoming.RemoveAll(i => i == id);
            }
            Rooms.Remove(id);
        }
    }

    public class MapGenerator
    {
        private const int WIDTH = 7;
        private const int HEIGHT = 15; // 0 to 14
        
        private readonly bool[,] template = new bool[WIDTH, HEIGHT];
        private readonly Random rng;
        private int nextRoomId = 1;
        private MapGraph graph;

        // Configurable parameters
        public int PathTracks = 7; 
        
        // The weights used for random generation.
        private readonly Dictionary<RoomType, int> _locationWeights;

        public MapGenerator(int seed, Dictionary<RoomType, int> weights = null)
        {
            rng = new Random(seed);
            
            // If no weights provided, use these defaults adjusted for our new Enums
            _locationWeights = weights ?? new Dictionary<RoomType, int>
            {
                { RoomType.Monster, 45 },
                { RoomType.Elite, 12 },
                { RoomType.Event, 22 },
                { RoomType.Shop, 8 },
                { RoomType.Rest, 13 }
            };

            BuildTemplate();
        }

        private void BuildTemplate()
        {
            // Standard centered pyramid-ish pattern mask
            for (int x = 0; x < WIDTH; x++)
                for (int y = 0; y < HEIGHT; y++)
                    template[x, y] = true; 

            template[0, 14] = false;
            template[6, 14] = false;
            template[0, 13] = false;
            template[6, 13] = false;
            template[0, 0] = true; 
        }

        public MapGraph Generate()
        {
            nextRoomId = 1;
            graph = new MapGraph();

            // 1) Instantiate rooms for every template cell
            for (int y = 0; y < HEIGHT; y++)
            {
                for (int x = 0; x < WIDTH; x++)
                {
                    if (!template[x, y]) continue;
                    var r = new Room(nextRoomId++, x, y);
                    graph.AddRoom(r);
                }
            }

            // 2) Generate skeleton paths
            GenerateSkeletonPaths();

            // 3) Prune unconnected rooms
            PruneUnconnectedRooms();

            // 4) Assign base locations (Structural Rules)
            AssignBaseLocations();

            // 5) Assign remaining rooms using weights
            AssignRemainingLocations();

            // 6) Allocate boss room above top (Floor 15)
            AllocateBossRoom();

            return graph;
        }

        private void GenerateSkeletonPaths()
        {
            var starts = new List<int>();
            var floor0Rooms = graph.RoomsOnFloor(0).ToList();
            if (floor0Rooms.Count == 0) return;

            for (int track = 0; track < PathTracks; track++)
            {
                Room start;
                int attempts = 0;
                do
                {
                    start = floor0Rooms[rng.Next(floor0Rooms.Count)];
                    attempts++;
                    if (attempts > 100) break;
                } while (track == 1 && starts.Count > 0 && start.Id == starts[0]);

                starts.Add(start.Id);

                Room current = start;
                for (int y = 0; y < HEIGHT - 1; y++)
                {
                    var candidates = GetCandidateRoomsAbove(current);
                    if (candidates.Count == 0) break;

                    var candidatesShuffled = candidates.OrderBy(_ => rng.Next()).ToList();
                    Room chosen = null;
                    foreach (var cand in candidatesShuffled)
                    {
                        if (!WouldCross(current, cand))
                        {
                            chosen = cand;
                            break;
                        }
                    }

                    if (chosen == null)
                    {
                        chosen = candidatesShuffled.FirstOrDefault();
                        if (chosen == null) break;
                    }

                    ConnectRooms(current, chosen);
                    current = chosen;
                }
            }
        }

        private List<Room> GetCandidateRoomsAbove(Room r)
        {
            var list = new List<Room>();
            int y = r.Y + 1;
            if (y >= HEIGHT) return list;
            
            for (int dx = -1; dx <= 1; dx++)
            {
                int nx = r.X + dx;
                if (nx < 0 || nx >= WIDTH) continue;
                var nr = graph.GetRoomAt(nx, y);
                if (nr != null) list.Add(nr);
            }
            return list;
        }

        private void ConnectRooms(Room a, Room b)
        {
            if (!a.Outgoing.Contains(b.Id)) a.Outgoing.Add(b.Id);
            if (!b.Incoming.Contains(a.Id)) b.Incoming.Add(a.Id);
        }

        private bool WouldCross(Room a, Room b)
        {
            int y = a.Y;
            foreach (var room in graph.RoomsOnFloor(y))
            {
                foreach (var outId in room.Outgoing)
                {
                    var dest = graph.Rooms[outId];
                    if (dest.Y != y + 1) continue;
                    if ((room.X < a.X && dest.X > b.X) || (room.X > a.X && dest.X < b.X))
                        return true;
                }
            }
            return false;
        }

        private void PruneUnconnectedRooms()
        {
            var isolated = graph.Rooms.Values
                .Where(r => r.Incoming.Count == 0 && r.Outgoing.Count == 0 && r.Y != 0)
                .Select(r => r.Id)
                .ToList();

            foreach (var id in isolated)
                graph.RemoveRoom(id);
        }

        private void AssignBaseLocations()
        {
            // Rule: Floor 0 (Bottom) is always Monsters (Start of run)
            foreach (var r in graph.RoomsOnFloor(0))
                r.Type = RoomType.Monster;

            // Rule: Floor 14 (Top of Act) is always Rest before Boss
            foreach (var r in graph.RoomsOnFloor(HEIGHT - 1))
                r.Type = RoomType.Rest;
                
            // Removed fixed Treasure on floor 8. It will now be assigned by weights.
        }

        private void AssignRemainingLocations()
        {
            var roomsToAssign = graph.Rooms.Values
                .Where(r => r.Type == RoomType.None)
                .OrderBy(r => r.Y) // Assign from bottom up
                .ThenBy(r => r.X)
                .ToList();

            foreach (var r in roomsToAssign)
            {
                AssignRoomWithRules(r);
            }
        }

        private void AssignRoomWithRules(Room r)
        {
            int tries = 0;
            do
            {
                r.Type = PickWeightedLocation(r);
                tries++;
                if (tries > 200) break;
            } while (!LocationRulesSatisfied(r));
        }

        private RoomType PickWeightedLocation(Room r)
        {
            // Filter allowed types based on Floor Height
            var allowed = new List<KeyValuePair<RoomType, int>>();
            foreach (var kv in _locationWeights)
            {
                var type = kv.Key;
                // Rule: Elites and Rest sites don't appear in the first 5 floors (0-4)
                if ((type == RoomType.Elite || type == RoomType.Rest) && r.Y < 5) continue;
                allowed.Add(kv);
            }

            // Weighted Random Selection
            int total = allowed.Sum(k => k.Value);
            if (total <= 0) return RoomType.Monster; // Fallback
            
            int pick = rng.Next(total);
            int acc = 0;
            foreach (var kv in allowed)
            {
                acc += kv.Value;
                if (pick < acc) return kv.Key;
            }
            return allowed.Last().Key;
        }

        private bool LocationRulesSatisfied(Room r)
        {
            // Rule 1: Elite and Rest cannot be below floor 5 (Already handled in Pick, but safety check)
            if ((r.Type == RoomType.Elite || r.Type == RoomType.Rest) && r.Y < 5) return false;

            // Rule 2: Special nodes (Elite, Shop, Rest) cannot be consecutive
            foreach (var incId in r.Incoming)
            {
                var inc = graph.Rooms[incId];
                if (IsSpecial(inc.Type) && IsSameSpecial(inc.Type, r.Type)) return false;
            }
            foreach (var outId in r.Outgoing)
            {
                var outR = graph.Rooms[outId];
                if (IsSpecial(outR.Type) && IsSameSpecial(outR.Type, r.Type)) return false;
            }

            // Rule 3: If this room has 2+ outgoing paths, destinations must be unique types
            // (e.g. don't offer two Monster rooms as the only choices)
            if (r.Outgoing.Count >= 2)
            {
                var types = r.Outgoing.Select(id => graph.Rooms[id].Type).Where(t => t != RoomType.None).ToList();
                if (types.Count != types.Distinct().Count()) return false;
            }

            return true;
        }

        private bool IsSpecial(RoomType t)
        {
            return t == RoomType.Elite || t == RoomType.Shop || t == RoomType.Rest;
        }

        private bool IsSameSpecial(RoomType a, RoomType b)
        {
            if (!IsSpecial(a) || !IsSpecial(b)) return false;
            return a == b;
        }

        private void AllocateBossRoom()
        {
            var topRooms = graph.RoomsOnFloor(HEIGHT - 1).ToList();
            if (topRooms.Count == 0) return;

            // Boss is technically at Y=15 (Height), outside the grid array bounds of 0-14
            var boss = new Room(nextRoomId++, 3, HEIGHT) { Type = RoomType.Boss }; 
            graph.AddRoom(boss);

            foreach (var r in topRooms)
            {
                ConnectRooms(r, boss);
            }
        }
    }
}