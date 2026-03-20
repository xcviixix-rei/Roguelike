from typing import Dict, List, Optional
from .map_graph import MapGraph, Room
from .map_generator import MapGenerator
from ...data.enums import RoomType


class MapManager:
    def __init__(self):
        self.current_map: Optional[MapGraph] = None
        self.current_node_id: int = -1

    def generate_new_map(self, seed: int,
                         room_weights: Optional[Dict[RoomType, float]] = None,
                         monster_star_ratio: float = 0.5,
                         elite_star_ratio: float = 0.5):
        int_weights = None
        if room_weights is not None:
            int_weights = {rt: int(round(v)) for rt, v in room_weights.items()}

        generator = MapGenerator(seed, int_weights, monster_star_ratio, elite_star_ratio)
        self.current_map = generator.generate()

    def get_current_room(self) -> Optional[Room]:
        if self.current_node_id == -1:
            return None
        return self.current_map.rooms.get(self.current_node_id)

    def get_possible_next_nodes(self) -> List[Room]:
        if self.current_map is None:
            return []

        if self.current_node_id == -1:
            return self.current_map.rooms_on_floor(0)

        current = self.get_current_room()
        if current is None:
            return []

        return [self.current_map.rooms[nid] for nid in current.outgoing if nid in self.current_map.rooms]

    def move_to_node(self, node_id: int) -> bool:
        possible = self.get_possible_next_nodes()
        if any(r.id == node_id for r in possible):
            self.current_node_id = node_id
            return True
        return False
