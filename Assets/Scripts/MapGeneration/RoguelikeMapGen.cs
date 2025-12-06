// SlayTheSpire_MapGenerator.cs
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
        Merchant,
        Treasure,
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
        // quick lookup grid [x,y] -> roomId or -1
        public int[,] Grid = new int[7, 15];

        public MapGraph()
        {
            for (int x = 0; x < 7; x++)
                for (int y = 0; y < 15; y++)
                    Grid[x, y] = -1;
        }

        public void AddRoom(Room r)
        {
            Rooms[r.Id] = r;
            Grid[r.X, r.Y] = r.Id;
        }

        public Room GetRoomAt(int x, int y)
        {
            if (x < 0 || x >= 7 || y < 0 || y >= 15) return null;
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
            Grid[r.X, r.Y] = -1;
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
        private readonly bool[,] template = new bool[7, 15];
        private readonly Random rng;
        private int nextRoomId = 1;
        private MapGraph graph;

        // Configurable parameters (weights, counts)
        public int PathTracks = 7; // number of parallel skeleton paths (the article describes 7)

        // Location weights (these are defaults you can tweak). We supply a simple set; adjust for ascension.
        // Units are weights for weighted random selection. Monster weight is relatively high by default.
        public Dictionary<RoomType, int> LocationWeights = new Dictionary<RoomType, int>
        {
            { RoomType.Monster, 53 },
            { RoomType.Elite, 8 },
            { RoomType.Event, 15 },
            { RoomType.Merchant, 7 },
            { RoomType.Treasure, 8 },
            { RoomType.Rest, 9 }
        };

        public MapGenerator(int? seed = null)
        {
            rng = seed.HasValue ? new Random(seed.Value) : new Random();
            BuildTemplate();
        }

        private void BuildTemplate()
        {
            // We'll use a simple template similar to Slay: many valid positions, some invalid.
            // The original uses an irregular isometric triangular grid. We emulate it by enabling
            // positions in a centered pyramid-ish pattern per floor.

            // For fidelity you can replace this hard-coded mask with the exact template from the game.
            for (int x = 0; x < 7; x++)
                for (int y = 0; y < 15; y++)
                    template[x, y] = true; // default allow

            // Create some unavailable slots to mimic irregular shape (optional). This is conservative.
            // (You can modify or remove these lines.)
            template[0, 14] = false;
            template[6, 14] = false;
            template[0, 13] = false;
            template[6, 13] = false;
            template[0, 0] = true; // ensure some starts
        }

        // Public API: Generate map and return a MapGraph
        public MapGraph Generate(int? seed = null)
        {
            if (seed.HasValue) rng = new Random(seed.Value);

            nextRoomId = 1;
            graph = new MapGraph();

            // 1) instantiate rooms for every template cell
            for (int y = 0; y < 15; y++)
            {
                for (int x = 0; x < 7; x++)
                {
                    if (!template[x, y]) continue;
                    var r = new Room(nextRoomId++, x, y);
                    graph.AddRoom(r);
                }
            }

            // 2) Generate skeleton paths (upwards chains) - create PathTracks tracks
            GenerateSkeletonPaths();

            // 3) prune unconnected rooms
            PruneUnconnectedRooms();

            // 4) Assign base locations (floor rules)
            AssignBaseLocations();

            // 5) Assign remaining rooms with weighted random types (and enforce rules)
            AssignRemainingLocations();

            // 6) Allocate boss room above top
            AllocateBossRoom();

            return graph;
        }

        private void GenerateSkeletonPaths()
        {
            // We will create PathTracks independent attempts to make chains from floor 0 to floor 14.
            // Each chain: pick a random start on floor 0, then at each step choose one of the nearest
            // candidates on next floor (x-1, x, x+1), ensuring no crossing with existing connections.

            // Keep a list of chosen starts to ensure first two are not the same.
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

                // walk up floors
                Room current = start;
                for (int y = 0; y < 14; y++)
                {
                    var candidates = GetCandidateRoomsAbove(current);
                    if (candidates.Count == 0) break;

                    // shuffle candidates and try to pick one that doesn't cause crossing
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
                        // fallback: take first candidate even if crossing would occur
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
            if (y >= 15) return list;
            // candidate x positions are x-1, x, x+1 (clamped)
            for (int dx = -1; dx <= 1; dx++)
            {
                int nx = r.X + dx;
                if (nx < 0 || nx >= 7) continue;
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
            // Crossing can only happen between connections that span the same pair of floors.
            // For all existing connections from floor a.Y -> a.Y+1, check ordering.
            int y = a.Y;
            foreach (var room in graph.RoomsOnFloor(y))
            {
                foreach (var outId in room.Outgoing)
                {
                    var dest = graph.Rooms[outId];
                    if (dest.Y != y + 1) continue;
                    // we have an existing connection room -> dest. Check crossing with a->b.
                    // crossing when (room.X < a.X && dest.X > b.X) || (room.X > a.X && dest.X < b.X)
                    if ((room.X < a.X && dest.X > b.X) || (room.X > a.X && dest.X < b.X))
                        return true;
                }
            }
            return false;
        }

        private void PruneUnconnectedRooms()
        {
            // Remove any room that has no incoming and no outgoing (isolated template cells)
            var isolated = graph.Rooms.Values.Where(r => r.Incoming.Count == 0 && r.Outgoing.Count == 0 && r.Y != 0).Select(r => r.Id).ToList();
            // note: keep some start rooms on floor 0 even if isolated (though normally they should be connected)
            foreach (var id in isolated)
                graph.RemoveRoom(id);
        }

        private void AssignBaseLocations()
        {
            // Floor 1 (Y==0 in zero-index) -> Monsters
            foreach (var r in graph.RoomsOnFloor(0))
                r.Type = RoomType.Monster;

            // Floor 9 -> Treasure (zero-index floor 8)
            foreach (var r in graph.RoomsOnFloor(8))
                r.Type = RoomType.Treasure;

            // Floor 15 -> Rest (zero-index floor 14)
            foreach (var r in graph.RoomsOnFloor(14))
                r.Type = RoomType.Rest;
        }

        private void AssignRemainingLocations()
        {
            // For all rooms that are None, assign by weighted random, then apply rules with re-rolls until valid.
            var roomsToAssign = graph.Rooms.Values.Where(r => r.Type == RoomType.None).ToList();

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
                if (tries > 200) break; // safety
            } while (!LocationRulesSatisfied(r));
        }

        private RoomType PickWeightedLocation(Room r)
        {
            // special-case: floors with preassigned (already set earlier) should not be overwritten
            if (r.Y == 0) return RoomType.Monster;
            if (r.Y == 8) return RoomType.Treasure;
            if (r.Y == 14) return RoomType.Rest;

            // enforce elites/rest not below floor 6 (zero-index 5)
            var allowed = new List<KeyValuePair<RoomType, int>>();
            foreach (var kv in LocationWeights)
            {
                var type = kv.Key;
                var w = kv.Value;
                if ((type == RoomType.Elite || type == RoomType.Rest) && r.Y < 5) continue;
                allowed.Add(kv);
            }

            // build cumulative
            int total = allowed.Sum(k => k.Value);
            if (total <= 0) return RoomType.Event;
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
            // Rule 1: Elite and Rest cannot be below floor 6 (zero-index < 5) - guaranteed during pick but double-check
            if ((r.Type == RoomType.Elite || r.Type == RoomType.Rest) && r.Y < 5) return false;

            // Rule 2: Elite, Merchant, Rest cannot be consecutive along any path.
            // For all incoming neighbors: if neighbor.Type is special and equal to this type -> invalid
            foreach (var incId in r.Incoming)
            {
                var inc = graph.Rooms[incId];
                if (IsSpecial(inc.Type) && IsSameSpecial(inc.Type, r.Type)) return false;
            }
            // For outgoing neighbors as well
            foreach (var outId in r.Outgoing)
            {
                var outR = graph.Rooms[outId];
                if (IsSpecial(outR.Type) && IsSameSpecial(outR.Type, r.Type)) return false;
            }

            // Rule 3: If this room has 2+ outgoing, destinations must be unique types
            if (r.Outgoing.Count >= 2)
            {
                var types = r.Outgoing.Select(id => graph.Rooms[id].Type).Where(t => t != RoomType.None).ToList();
                if (types.Count != types.Distinct().Count()) return false;
            }

            // Rule 4: Rest cannot be on floor 14 (zero-index 13)
            if (r.Type == RoomType.Rest && r.Y == 13) return false;

            return true;
        }

        private bool IsSpecial(RoomType t)
        {
            return t == RoomType.Elite || t == RoomType.Merchant || t == RoomType.Rest;
        }

        private bool IsSameSpecial(RoomType a, RoomType b)
        {
            if (!IsSpecial(a) || !IsSpecial(b)) return false;
            return a == b;
        }

        private void AllocateBossRoom()
        {
            // Create a boss room node above floor 14 and connect all floor-14 rooms to it.
            var topRooms = graph.RoomsOnFloor(14).ToList();
            if (topRooms.Count == 0) return;

            var boss = new Room(nextRoomId++, 3, 15) { Type = RoomType.Boss }; // X=3 is arbitrary; Y=15 is above
            graph.AddRoom(boss);

            foreach (var r in topRooms)
            {
                ConnectRooms(r, boss);
            }
        }
    }

    // Example usage for headless testing
    public static class Example
    {
        public static void Main()
        {
            var gen = new MapGenerator(seed: 12345);
            var map = gen.Generate();

            Console.WriteLine($"Generated rooms: {map.Rooms.Count}");
            foreach (var r in map.Rooms.Values.OrderBy(rr => rr.Y).ThenBy(rr => rr.X))
            {
                Console.WriteLine(r);
            }
        }
    }
}
