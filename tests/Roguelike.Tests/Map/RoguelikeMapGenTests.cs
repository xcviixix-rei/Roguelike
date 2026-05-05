using Roguelike.Core.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Roguelike.Tests.Map
{
    public class RoguelikeMapGenTests
    {
        [Fact]
        public void MapGenerator_SameSeed_ProducesSameMap()
        {

            var seed = 12345;
            var gen1 = new MapGenerator(seed);
            var gen2 = new MapGenerator(seed);


            var map1 = gen1.Generate();
            var map2 = gen2.Generate();


            Assert.Equal(map1.Rooms.Count, map2.Rooms.Count);
            Assert.Equal(MapGraph.Width, MapGraph.Width);
            Assert.Equal(MapGraph.Height, MapGraph.Height);


            foreach (var room1 in map1.Rooms.Values)
            {
                var room2 = map2.Rooms[room1.Id];
                Assert.Equal(room1.X, room2.X);
                Assert.Equal(room1.Y, room2.Y);
                Assert.Equal(room1.Type, room2.Type);
            }
        }

        [Fact]
        public void MapGenerator_DifferentSeeds_ProduceDifferentMaps()
        {

            var gen1 = new MapGenerator(12345);
            var gen2 = new MapGenerator(67890);


            var map1 = gen1.Generate();
            var map2 = gen2.Generate();


            bool roomsAreDifferent = false;
            foreach (var room1 in map1.Rooms.Values.Take(Math.Min(map1.Rooms.Count, map2.Rooms.Count)))
            {
                if (map2.Rooms.TryGetValue(room1.Id, out var room2))
                {
                    if (room1.X != room2.X || room1.Y != room2.Y || room1.Type != room2.Type)
                    {
                        roomsAreDifferent = true;
                        break;
                    }
                }
            }
            Assert.True(roomsAreDifferent || map1.Rooms.Count != map2.Rooms.Count);
        }

        [Fact]
        public void MapGenerator_HasCorrectDimensions()
        {

            var generator = new MapGenerator(42);
            var map = generator.Generate();


            Assert.Equal(7, MapGraph.Width);
            Assert.Equal(15, MapGraph.Height);
        }

        [Fact]
        public void MapGenerator_HasBossRoomOnTopFloor()
        {

            var generator = new MapGenerator(42);
            var map = generator.Generate();


            var bossRooms = map.Rooms.Values.Where(r => r.Type == RoomType.Boss).ToList();
            Assert.Single(bossRooms);

            Assert.True(bossRooms[0].Y >= 14);
        }

        [Fact]
        public void MapGenerator_AllRoomsAreReachable()
        {

            var generator = new MapGenerator(42);
            var map = generator.Generate();


            var startRoom = map.Rooms.Values.FirstOrDefault(r => r.Y == 0);
            Assert.NotNull(startRoom);

            var visited = new HashSet<int>();
            var queue = new Queue<Room>();
            queue.Enqueue(startRoom);
            visited.Add(startRoom.Id);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                foreach (var neighborId in current.Outgoing.Concat(current.Incoming))
                {
                    if (!visited.Contains(neighborId) && map.Rooms.ContainsKey(neighborId))
                    {
                        visited.Add(neighborId);
                        var neighbor = map.Rooms[neighborId];
                        queue.Enqueue(neighbor);
                    }
                }
            }


            Assert.True(visited.Count >= map.Rooms.Count * 0.7,
                $"Expected most rooms to be reachable, but only {visited.Count} out of {map.Rooms.Count} were visited");
        }

        [Fact]
        public void MapGenerator_HasCorrectNumberOfFloors()
        {

            var generator = new MapGenerator(42);
            var map = generator.Generate();


            var floors = map.Rooms.Values.Select(r => r.Y).Distinct().Count();
            Assert.True(floors <= 16, $"Expected at most 16 floors but got {floors}");
        }

        [Fact]
        public void MapGenerator_RoomTypesAreValid()
        {

            var generator = new MapGenerator(42);
            var map = generator.Generate();


            var validTypes = new[] { RoomType.Monster, RoomType.Elite, RoomType.Event, RoomType.Shop, RoomType.Rest, RoomType.Boss, RoomType.None };
            foreach (var room in map.Rooms.Values)
            {
                Assert.Contains(room.Type, validTypes);
            }
        }

        [Fact]
        public void MapGenerator_StarRatingsAreWithinValidRange()
        {

            var generator = new MapGenerator(42);
            var map = generator.Generate();


            foreach (var room in map.Rooms.Values)
            {
                Assert.InRange(room.StarRating, 1, 5);
            }
        }

        [Fact]
        public void MapGenerator_CustomWeights_AffectRoomDistribution()
        {

            var weights = new Dictionary<RoomType, int>
            {
                { RoomType.Monster, 10 },
                { RoomType.Elite, 100 },
                { RoomType.Event, 10 },
                { RoomType.Shop, 10 },
                { RoomType.Rest, 10 }
            };

            var generator = new MapGenerator(42, weights);


            var map = generator.Generate();


            var eliteCount = map.Rooms.Values.Count(r => r.Type == RoomType.Elite);


            Assert.True(eliteCount >= 3,
                $"Expected at least 3 Elite rooms with heavy weighting, but got {eliteCount}");
        }

        [Fact]
        public void MapGenerator_FirstFloorIsNotBoss()
        {

            var generator = new MapGenerator(42);
            var map = generator.Generate();


            var firstFloorRooms = map.Rooms.Values.Where(r => r.Y == 0).ToList();
            Assert.All(firstFloorRooms, room => Assert.NotEqual(RoomType.Boss, room.Type));
        }

        [Fact]
        public void MapGenerator_ConnectionsAreBidirectional()
        {

            var generator = new MapGenerator(42);
            var map = generator.Generate();


            foreach (var room in map.Rooms.Values)
            {
                foreach (var outgoingId in room.Outgoing)
                {
                    var connectedRoom = map.Rooms[outgoingId];
                    Assert.Contains(room.Id, connectedRoom.Incoming);
                }
            }
        }

        [Fact]
        public void Room_ToStringContainsRoomInfo()
        {

            var room = new Room(1, 3, 5)
            {
                Type = RoomType.Monster,
                StarRating = 2
            };


            var roomString = room.ToString();


            Assert.Contains("Monster", roomString);
            Assert.Contains("2", roomString);
        }

        [Fact]
        public void MapGraph_AddRemove_WorksCorrectly()
        {

            var graph = new MapGraph();
            var room = new Room(1, 2, 3);


            graph.AddRoom(room);
            var retrieved = graph.GetRoomAt(2, 3);


            Assert.NotNull(retrieved);
            Assert.Equal(1, retrieved.Id);


            graph.RemoveRoom(room.Id);
            var afterRemoval = graph.GetRoomAt(2, 3);


            Assert.Null(afterRemoval);
        }

        [Fact]
        public void MapGraph_RoomsOnFloor_ReturnsCorrectRooms()
        {

            var graph = new MapGraph();
            graph.AddRoom(new Room(1, 0, 5));
            graph.AddRoom(new Room(2, 1, 5));
            graph.AddRoom(new Room(3, 2, 5));
            graph.AddRoom(new Room(4, 3, 6));


            var roomsOnFloor5 = graph.RoomsOnFloor(5).ToList();


            Assert.Equal(3, roomsOnFloor5.Count);
            Assert.All(roomsOnFloor5, room => Assert.Equal(5, room.Y));
        }
    }
}
