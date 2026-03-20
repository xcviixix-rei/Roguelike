using System;
using System.Collections.Generic;
using System.Linq;

namespace Roguelike.Core.Map
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
        public int X { get; }
        public int Y { get; }
        public List<int> Outgoing { get; } = new List<int>();
        public List<int> Incoming { get; } = new List<int>();
        public RoomType Type { get; set; } = RoomType.None;
        public int StarRating { get; set; } = 1;

        public Room(int id, int x, int y)
        {
            Id = id;
            X = x;
            Y = y;
        }

        public override string ToString()
        {
            return $"Room(Id={Id}, x={X}, y={Y}, type={Type}, stars={StarRating})";
        }
    }

    public class MapGraph
    {
        public Dictionary<int, Room> Rooms { get; } = new Dictionary<int, Room>();
        public const int Width = 7;
        public const int Height = 15;
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

            if (r.X >= 0 && r.X < Width && r.Y >= 0 && r.Y < Height)
            {
                Grid[r.X, r.Y] = -1;
            }

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
        private const int HEIGHT = 15;

        private readonly bool[,] template = new bool[WIDTH, HEIGHT];
        private readonly Random rng;
        private int nextRoomId = 1;
        private MapGraph graph;

        public int PathTracks = 7;

        private readonly Dictionary<RoomType, int> _locationWeights;
        private readonly float _monsterRatio;
        private readonly float _eliteRatio;

        public MapGenerator(int seed, Dictionary<RoomType, int> weights = null, float monsterRatio = 0.5f, float eliteRatio = 0.5f)
        {
            rng = new Random(seed);
            _monsterRatio = monsterRatio;
            _eliteRatio = eliteRatio;

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

            for (int y = 0; y < HEIGHT; y++)
            {
                for (int x = 0; x < WIDTH; x++)
                {
                    if (!template[x, y]) continue;
                    var r = new Room(nextRoomId++, x, y);
                    graph.AddRoom(r);
                }
            }

            GenerateSkeletonPaths();
            PruneUnconnectedRooms();
            PruneDeadEnds();
            AssignBaseLocations();
            AssignRemainingLocations();
            AllocateBossRoom();
            AssignStarRatings();

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

                    var shuffled = candidates.OrderBy(_ => rng.Next()).ToList();
                    Room chosen = null;

                    foreach (var cand in shuffled)
                    {
                        if (!WouldCross(current, cand))
                        {
                            chosen = cand;
                            break;
                        }
                    }

                    if (chosen == null)
                        chosen = shuffled.FirstOrDefault();

                    if (chosen == null) break;

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

            int[] bias = { -1, 1, 0 };

            foreach (int dx in bias)
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

        private bool LinesCross(int a1, int a2, int b1, int b2)
        {
            return (a1 < b1 && a2 > b2) || (a1 > b1 && a2 < b2);
        }

        private bool WouldCross(Room a, Room b)
        {
            int y = a.Y;

            foreach (var r in graph.RoomsOnFloor(y))
            {
                foreach (var outId in r.Outgoing)
                {
                    var dest = graph.Rooms[outId];
                    if (dest.Y != y + 1) continue;

                    if (LinesCross(a.X, b.X, r.X, dest.X))
                        return true;
                }
            }

            return false;
        }

        private void PruneUnconnectedRooms()
        {
            var reachableRoomIds = new HashSet<int>();
            var queue = new Queue<Room>();

            foreach (var startRoom in graph.RoomsOnFloor(0))
            {
                queue.Enqueue(startRoom);
                reachableRoomIds.Add(startRoom.Id);
            }

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var outId in current.Outgoing)
                {
                    if (!reachableRoomIds.Contains(outId))
                    {
                        reachableRoomIds.Add(outId);
                        queue.Enqueue(graph.Rooms[outId]);
                    }
                }
            }

            var roomsToRemove = graph.Rooms.Values
                .Where(r => !reachableRoomIds.Contains(r.Id))
                .Select(r => r.Id)
                .ToList();

            foreach (var id in roomsToRemove)
            {
                graph.RemoveRoom(id);
            }
        }

        private void PruneDeadEnds()
        {
            bool removed;

            do
            {
                removed = false;

                var toRemove = graph.Rooms.Values
                    .Where(r =>
                        r.Type != RoomType.Boss &&
                        r.Y < MapGraph.Height - 1 &&
                        r.Outgoing.Count == 0)
                    .Select(r => r.Id)
                    .ToList();

                if (toRemove.Count > 0)
                {
                    removed = true;
                    foreach (var id in toRemove)
                        graph.RemoveRoom(id);
                }

            } while (removed);
        }

        private void AssignBaseLocations()
        {
            foreach (var r in graph.RoomsOnFloor(0))
                r.Type = RoomType.Monster;

            foreach (var r in graph.RoomsOnFloor(HEIGHT - 1))
                r.Type = RoomType.Rest;
        }

        private void AssignRemainingLocations()
        {
            var roomsToAssign = graph.Rooms.Values
                .Where(r => r.Type == RoomType.None)
                .OrderBy(r => r.Y)
                .ThenBy(r => r.X)
                .ToList();

            foreach (var r in roomsToAssign)
                AssignRoomWithRules(r);
        }

        private void AssignRoomWithRules(Room r)
        {
            int tries = 0;
            do
            {
                r.Type = PickWeightedLocation(r);
                tries++;
                if (tries > 200) {
                    r.Type = RoomType.Monster;
                    break;
                }
            } while (!LocationRulesSatisfied(r));
        }

        private RoomType PickWeightedLocation(Room r)
        {
            var allowed = new List<KeyValuePair<RoomType, int>>();

            foreach (var kv in _locationWeights)
            {
                var type = kv.Key;
                if ((type == RoomType.Elite || type == RoomType.Rest) && r.Y < 5) continue;
                allowed.Add(kv);
            }

            int total = allowed.Sum(k => k.Value);
            if (total <= 0) return RoomType.Monster;

            int pick = rng.Next(total);
            int acc = 0;

            foreach (var kv in allowed)
            {
                acc += kv.Value;
                if (pick < acc) return kv.Key;
            }

            return allowed.Last().Key;
        }

        private bool IsSpecial(RoomType t)
        {
            return t == RoomType.Elite || t == RoomType.Shop || t == RoomType.Rest;
        }

        private bool LocationRulesSatisfied(Room r)
        {
            // Elite and Rest rooms must be on floor 5 or higher
            if ((r.Type == RoomType.Elite || r.Type == RoomType.Rest) && r.Y < 5)
                return false;

            // No two special rooms can be directly connected
            foreach (var incId in r.Incoming)
            {
                var inc = graph.Rooms[incId];
                if (IsSpecial(inc.Type) && IsSpecial(r.Type))
                    return false;
            }

            foreach (var outId in r.Outgoing)
            {
                var outR = graph.Rooms[outId];
                if (IsSpecial(outR.Type) && IsSpecial(r.Type))
                    return false;
            }

            // If a room has multiple outgoing paths, they should lead to different room types
            if (r.Outgoing.Count >= 2)
            {
                var types = r.Outgoing.Select(id => graph.Rooms[id].Type)
                    .Where(t => t != RoomType.None)
                    .ToList();

                if (types.Count != types.Distinct().Count())
                    return false;
            }

            // Elite spacing rule: only check within same floor and adjacent floors
            if (r.Type == RoomType.Elite)
            {
                var elitesOnSameFloor = graph.RoomsOnFloor(r.Y)
                    .Count(other => other.Id != r.Id && other.Type == RoomType.Elite);
                
                if (elitesOnSameFloor > 0)
                    return false;
                    
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dy == 0) continue;
                    
                    int checkY = r.Y + dy;
                    if (checkY < 0 || checkY >= MapGraph.Height) continue;
                    
                    var nearbyElites = graph.RoomsOnFloor(checkY)
                        .Where(other => other.Type == RoomType.Elite)
                        .ToList();
                        
                    foreach (var elite in nearbyElites)
                    {
                        if (Math.Abs(elite.X - r.X) <= 1)
                            return false;
                    }
                }
            }

            foreach (var parentId in r.Incoming)
            {
                var parent = graph.Rooms[parentId];

                var siblingTypes = parent.Outgoing
                    .Where(outId => outId != r.Id)
                    .Select(outId => graph.Rooms[outId].Type)
                    .Where(t => t != RoomType.None)
                    .ToList();

                if (siblingTypes.Contains(r.Type))
                    return false;
            }

            return true;
        }

        private void AllocateBossRoom()
        {
            var topRooms = graph.RoomsOnFloor(HEIGHT - 1).ToList();
            if (topRooms.Count == 0) return;

            var boss = new Room(nextRoomId++, 3, HEIGHT) { Type = RoomType.Boss };
            graph.AddRoom(boss);

            foreach (var r in topRooms)
                ConnectRooms(r, boss);
        }

        private void AssignStarRatings()
        {
            var monsters = graph.Rooms.Values
                .Where(r => r.Type == RoomType.Monster)
                .OrderBy(r => r.Y)
                .ThenBy(r => r.X)
                .ToList();

            if (monsters.Count > 0)
            {
                int splitIndex = (int)(monsters.Count * _monsterRatio);
                for (int i = 0; i < monsters.Count; i++)
                    monsters[i].StarRating = (i < splitIndex) ? 1 : 2;
            }

            var elites = graph.Rooms.Values
                .Where(r => r.Type == RoomType.Elite)
                .OrderBy(r => r.Y)
                .ThenBy(r => r.X)
                .ToList();

            if (elites.Count > 0)
            {
                int splitIndex = (int)(elites.Count * _eliteRatio);
                for (int i = 0; i < elites.Count; i++)
                    elites[i].StarRating = (i < splitIndex) ? 3 : 4;
            }

            var boss = graph.Rooms.Values.FirstOrDefault(r => r.Type == RoomType.Boss);
            if (boss != null)
                boss.StarRating = 5;
        }
    }
}
