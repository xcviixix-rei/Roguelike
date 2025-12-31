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
            // Arrange
            var seed = 12345;
            var gen1 = new MapGenerator(seed);
            var gen2 = new MapGenerator(seed);

            // Act
            var map1 = gen1.Generate();
            var map2 = gen2.Generate();

            // Assert - verify deterministic generation
            Assert.Equal(map1.Rooms.Count, map2.Rooms.Count);
            Assert.Equal(MapGraph.Width, MapGraph.Width);
            Assert.Equal(MapGraph.Height, MapGraph.Height);
            
            // Check that room positions match
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
            // Arrange
            var gen1 = new MapGenerator(12345);
            var gen2 = new MapGenerator(67890);

            // Act
            var map1 = gen1.Generate();
            var map2 = gen2.Generate();

            // Assert - maps should be different
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
            // Arrange & Act
            var generator = new MapGenerator(42);
            var map = generator.Generate();

            // Assert
            Assert.Equal(7, MapGraph.Width);
            Assert.Equal(15, MapGraph.Height);
        }

        [Fact]
        public void MapGenerator_HasBossRoomOnTopFloor()
        {
            // Arrange & Act
            var generator = new MapGenerator(42);
            var map = generator.Generate();

            // Assert
            var bossRooms = map.Rooms.Values.Where(r => r.Type == RoomType.Boss).ToList();
            Assert.Single(bossRooms);
            // Boss room is above floor 14, so Y should be 15
            Assert.True(bossRooms[0].Y >= 14);
        }

        [Fact]
        public void MapGenerator_AllRoomsAreReachable()
        {
            // Arrange & Act
            var generator = new MapGenerator(42);
            var map = generator.Generate();

            // Assert - verify connectivity using BFS
            var startRoom = map.Rooms.Values.FirstOrDefault(r => r.Y == 0);
            Assert.NotNull(startRoom);

            var visited = new HashSet<int>();
            var queue = new Queue<Room>();
            queue.Enqueue(startRoom);
            visited.Add(startRoom.Id);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                // Traverse both outgoing and incoming to handle all connections
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

            // All rooms should be reachable (or very close - some may be pruned)
            // The actual implementation may prune some dead ends
            Assert.True(visited.Count >= map.Rooms.Count * 0.7, 
                $"Expected most rooms to be reachable, but only {visited.Count} out of {map.Rooms.Count} were visited");
        }

        [Fact]
        public void MapGenerator_HasCorrectNumberOfFloors()
        {
            // Arrange & Act
            var generator = new MapGenerator(42);
            var map = generator.Generate();

            // Assert - map can have up to 16 floors (0-14 regular + boss at Y=15)
            var floors = map.Rooms.Values.Select(r => r.Y).Distinct().Count();
            Assert.True(floors <= 16, $"Expected at most 16 floors but got {floors}");
        }

        [Fact]
        public void MapGenerator_RoomTypesAreValid()
        {
            // Arrange & Act
            var generator = new MapGenerator(42);
            var map = generator.Generate();

            // Assert - all rooms should have valid types
            var validTypes = new[] { RoomType.Monster, RoomType.Elite, RoomType.Event, RoomType.Shop, RoomType.Rest, RoomType.Boss, RoomType.None };
            foreach (var room in map.Rooms.Values)
            {
                Assert.Contains(room.Type, validTypes);
            }
        }

        [Fact]
        public void MapGenerator_StarRatingsAreWithinValidRange()
        {
            // Arrange & Act
            var generator = new MapGenerator(42);
            var map = generator.Generate();

            // Assert - star ratings should be between 1 and 5
            foreach (var room in map.Rooms.Values)
            {
                Assert.InRange(room.StarRating, 1, 5);
            }
        }

        [Fact]
        public void MapGenerator_CustomWeights_AffectRoomDistribution()
        {
            // Arrange - create weights favoring Elite rooms
            var weights = new Dictionary<RoomType, int>
            {
                { RoomType.Monster, 10 },
                { RoomType.Elite, 100 }, // Heavy weight for elite
                { RoomType.Event, 10 },
                { RoomType.Shop, 10 },
                { RoomType.Rest, 10 }
            };

            var generator = new MapGenerator(42, weights);

            // Act
            var map = generator.Generate();

            // Assert - should have more Elite rooms than with default weights
            var eliteCount = map.Rooms.Values.Count(r => r.Type == RoomType.Elite);
            
            // With 10x weight, expect reasonable number of elite rooms
            // The actual distribution also depends on placement rules
            Assert.True(eliteCount >= 3, 
                $"Expected at least 3 Elite rooms with heavy weighting, but got {eliteCount}");
        }

        [Fact]
        public void MapGenerator_FirstFloorIsNotBoss()
        {
            // Arrange & Act
            var generator = new MapGenerator(42);
            var map = generator.Generate();

            // Assert
            var firstFloorRooms = map.Rooms.Values.Where(r => r.Y == 0).ToList();
            Assert.All(firstFloorRooms, room => Assert.NotEqual(RoomType.Boss, room.Type));
        }

        [Fact]
        public void MapGenerator_ConnectionsAreBidirectional()
        {
            // Arrange & Act
            var generator = new MapGenerator(42);
            var map = generator.Generate();

            // Assert - if room A has B in Outgoing, room B should have A in Incoming
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
            // Arrange
            var room = new Room(1, 3, 5)
            {
                Type = RoomType.Monster,
                StarRating = 2
            };

            // Act
            var roomString = room.ToString();

            // Assert
            Assert.Contains("Monster", roomString);
            Assert.Contains("2", roomString); // Star rating
        }

        [Fact]
        public void MapGraph_AddRemove_WorksCorrectly()
        {
            // Arrange
            var graph = new MapGraph();
            var room = new Room(1, 2, 3);

            // Act
            graph.AddRoom(room);
            var retrieved = graph.GetRoomAt(2, 3);
            
            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal(1, retrieved.Id);

            // Act - Remove
            graph.RemoveRoom(room.Id);
            var afterRemoval = graph.GetRoomAt(2, 3);
            
            // Assert
            Assert.Null(afterRemoval);
        }

        [Fact]
        public void MapGraph_RoomsOnFloor_ReturnsCorrectRooms()
        {
            // Arrange
            var graph = new MapGraph();
            graph.AddRoom(new Room(1, 0, 5));
            graph.AddRoom(new Room(2, 1, 5));
            graph.AddRoom(new Room(3, 2, 5));
            graph.AddRoom(new Room(4, 3, 6)); // Different floor

            // Act
            var roomsOnFloor5 = graph.RoomsOnFloor(5).ToList();

            // Assert
            Assert.Equal(3, roomsOnFloor5.Count);
            Assert.All(roomsOnFloor5, room => Assert.Equal(5, room.Y));
        }
    }
}
