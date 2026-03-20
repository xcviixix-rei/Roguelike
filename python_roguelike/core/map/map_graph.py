from dataclasses import dataclass, field
from typing import List, Dict
from ...data.enums import RoomType

WIDTH = 7
HEIGHT = 15


@dataclass
class Room:
    id: int
    x: int
    y: int
    outgoing: List[int] = field(default_factory=list)
    incoming: List[int] = field(default_factory=list)
    type: RoomType = RoomType.NONE
    star_rating: int = 1

    def __repr__(self):
        return f"Room(Id={self.id}, x={self.x}, y={self.y}, type={self.type}, stars={self.star_rating})"


class MapGraph:
    WIDTH = 7
    HEIGHT = 15

    def __init__(self):
        self.rooms: Dict[int, Room] = {}
        self.grid = [[-1] * self.HEIGHT for _ in range(self.WIDTH)]

    def add_room(self, r: Room):
        self.rooms[r.id] = r
        if 0 <= r.x < self.WIDTH and 0 <= r.y < self.HEIGHT:
            self.grid[r.x][r.y] = r.id

    def get_room_at(self, x: int, y: int):
        if x < 0 or x >= self.WIDTH or y < 0 or y >= self.HEIGHT:
            return None
        rid = self.grid[x][y]
        return None if rid == -1 else self.rooms.get(rid)

    def rooms_on_floor(self, y: int) -> List[Room]:
        return sorted(
            [r for r in self.rooms.values() if r.y == y],
            key=lambda r: r.x
        )

    def remove_room(self, id: int):
        if id not in self.rooms:
            return
        r = self.rooms[id]
        if 0 <= r.x < self.WIDTH and 0 <= r.y < self.HEIGHT:
            self.grid[r.x][r.y] = -1
        for other in self.rooms.values():
            if id in other.outgoing:
                other.outgoing.remove(id)
            if id in other.incoming:
                other.incoming.remove(id)
        del self.rooms[id]
