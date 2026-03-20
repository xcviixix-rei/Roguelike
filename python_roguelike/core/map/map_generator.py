import random
from typing import Dict, List, Optional
from .map_graph import MapGraph, Room
from ...data.enums import RoomType

WIDTH = 7
HEIGHT = 15


class MapGenerator:
    def __init__(self, seed: int,
                 weights: Optional[Dict[RoomType, int]] = None,
                 monster_ratio: float = 0.5,
                 elite_ratio: float = 0.5):
        self.rng = random.Random(seed)
        self.monster_ratio = monster_ratio
        self.elite_ratio = elite_ratio
        self.path_tracks = 7
        self._next_room_id = 1
        self.graph: Optional[MapGraph] = None

        self._location_weights = weights or {
            RoomType.Monster: 45,
            RoomType.Elite: 12,
            RoomType.Event: 22,
            RoomType.Shop: 8,
            RoomType.Rest: 13,
        }

        self._template = [[True] * HEIGHT for _ in range(WIDTH)]
        self._build_template()

    def _build_template(self):
        self._template[0][14] = False
        self._template[6][14] = False
        self._template[0][13] = False
        self._template[6][13] = False
        self._template[0][0] = True

    def generate(self) -> MapGraph:
        self._next_room_id = 1
        self.graph = MapGraph()

        for y in range(HEIGHT):
            for x in range(WIDTH):
                if not self._template[x][y]:
                    continue
                r = Room(id=self._next_room_id, x=x, y=y)
                self._next_room_id += 1
                self.graph.add_room(r)

        self._generate_skeleton_paths()
        self._prune_unconnected_rooms()
        self._prune_dead_ends()
        self._assign_base_locations()
        self._assign_remaining_locations()
        self._allocate_boss_room()
        self._assign_star_ratings()

        return self.graph

    def _generate_skeleton_paths(self):
        starts = []
        floor0 = self.graph.rooms_on_floor(0)
        if not floor0:
            return

        for track in range(self.path_tracks):
            attempts = 0
            while True:
                start = self.rng.choice(floor0)
                attempts += 1
                if attempts > 100:
                    break
                if track == 1 and starts and start.id == starts[0]:
                    continue
                break

            starts.append(start.id)
            current = start

            for y in range(HEIGHT - 1):
                candidates = self._get_candidates_above(current)
                if not candidates:
                    break

                shuffled = sorted(candidates, key=lambda _: self.rng.random())
                chosen = None

                for cand in shuffled:
                    if not self._would_cross(current, cand):
                        chosen = cand
                        break

                if chosen is None:
                    chosen = shuffled[0] if shuffled else None

                if chosen is None:
                    break

                self._connect_rooms(current, chosen)
                current = chosen

    def _get_candidates_above(self, r: Room) -> List[Room]:
        result = []
        y = r.y + 1
        if y >= HEIGHT:
            return result
        for dx in [-1, 1, 0]:
            nx = r.x + dx
            if nx < 0 or nx >= WIDTH:
                continue
            nr = self.graph.get_room_at(nx, y)
            if nr is not None:
                result.append(nr)
        return result

    def _connect_rooms(self, a: Room, b: Room):
        if b.id not in a.outgoing:
            a.outgoing.append(b.id)
        if a.id not in b.incoming:
            b.incoming.append(a.id)

    def _lines_cross(self, a1: int, a2: int, b1: int, b2: int) -> bool:
        return (a1 < b1 and a2 > b2) or (a1 > b1 and a2 < b2)

    def _would_cross(self, a: Room, b: Room) -> bool:
        y = a.y
        for r in self.graph.rooms_on_floor(y):
            for out_id in r.outgoing:
                dest = self.graph.rooms.get(out_id)
                if dest is None or dest.y != y + 1:
                    continue
                if self._lines_cross(a.x, b.x, r.x, dest.x):
                    return True
        return False

    def _prune_unconnected_rooms(self):
        reachable = set()
        queue = list(self.graph.rooms_on_floor(0))
        for r in queue:
            reachable.add(r.id)

        i = 0
        while i < len(queue):
            current = queue[i]
            i += 1
            for out_id in current.outgoing:
                if out_id not in reachable:
                    reachable.add(out_id)
                    queue.append(self.graph.rooms[out_id])

        to_remove = [rid for rid in list(self.graph.rooms.keys()) if rid not in reachable]
        for rid in to_remove:
            self.graph.remove_room(rid)

    def _prune_dead_ends(self):
        removed = True
        while removed:
            removed = False
            to_remove = [
                r.id for r in self.graph.rooms.values()
                if r.type != RoomType.Boss and r.y < MapGraph.HEIGHT - 1 and len(r.outgoing) == 0
            ]
            if to_remove:
                removed = True
                for rid in to_remove:
                    self.graph.remove_room(rid)

    def _assign_base_locations(self):
        for r in self.graph.rooms_on_floor(0):
            r.type = RoomType.Monster
        for r in self.graph.rooms_on_floor(HEIGHT - 1):
            r.type = RoomType.Rest

    def _assign_remaining_locations(self):
        rooms_to_assign = sorted(
            [r for r in self.graph.rooms.values() if r.type == RoomType.NONE],
            key=lambda r: (r.y, r.x)
        )
        for r in rooms_to_assign:
            self._assign_room_with_rules(r)

    def _assign_room_with_rules(self, r: Room):
        tries = 0
        while True:
            r.type = self._pick_weighted_location(r)
            tries += 1
            if tries > 200:
                r.type = RoomType.Monster
                break
            if self._location_rules_satisfied(r):
                break

    def _pick_weighted_location(self, r: Room) -> RoomType:
        allowed = [
            (rt, w) for rt, w in self._location_weights.items()
            if not ((rt == RoomType.Elite or rt == RoomType.Rest) and r.y < 5)
        ]
        total = sum(w for _, w in allowed)
        if total <= 0:
            return RoomType.Monster
        pick = self.rng.randint(0, total - 1)
        acc = 0
        for rt, w in allowed:
            acc += w
            if pick < acc:
                return rt
        return allowed[-1][0]

    def _is_special(self, t: RoomType) -> bool:
        return t in (RoomType.Elite, RoomType.Shop, RoomType.Rest)

    def _location_rules_satisfied(self, r: Room) -> bool:
        if (r.type == RoomType.Elite or r.type == RoomType.Rest) and r.y < 5:
            return False

        for inc_id in r.incoming:
            inc = self.graph.rooms[inc_id]
            if self._is_special(inc.type) and self._is_special(r.type):
                return False

        for out_id in r.outgoing:
            out_r = self.graph.rooms[out_id]
            if self._is_special(out_r.type) and self._is_special(r.type):
                return False

        if len(r.outgoing) >= 2:
            types = [
                self.graph.rooms[oid].type for oid in r.outgoing
                if self.graph.rooms[oid].type != RoomType.NONE
            ]
            if len(types) != len(set(types)):
                return False

        if r.type == RoomType.Elite:
            same_floor_elites = sum(
                1 for other in self.graph.rooms_on_floor(r.y)
                if other.id != r.id and other.type == RoomType.Elite
            )
            if same_floor_elites > 0:
                return False

            for dy in [-1, 1]:
                check_y = r.y + dy
                if check_y < 0 or check_y >= MapGraph.HEIGHT:
                    continue
                for other in self.graph.rooms_on_floor(check_y):
                    if other.type == RoomType.Elite and abs(other.x - r.x) <= 1:
                        return False

        for parent_id in r.incoming:
            parent = self.graph.rooms[parent_id]
            sibling_types = [
                self.graph.rooms[oid].type for oid in parent.outgoing
                if oid != r.id and self.graph.rooms[oid].type != RoomType.NONE
            ]
            if r.type in sibling_types:
                return False

        return True

    def _allocate_boss_room(self):
        top_rooms = self.graph.rooms_on_floor(HEIGHT - 1)
        if not top_rooms:
            return

        boss = Room(id=self._next_room_id, x=3, y=HEIGHT, type=RoomType.Boss)
        self._next_room_id += 1
        self.graph.add_room(boss)

        for r in top_rooms:
            self._connect_rooms(r, boss)

    def _assign_star_ratings(self):
        monsters = sorted(
            [r for r in self.graph.rooms.values() if r.type == RoomType.Monster],
            key=lambda r: (r.y, r.x)
        )
        if monsters:
            split_idx = int(len(monsters) * self.monster_ratio)
            for i, m in enumerate(monsters):
                m.star_rating = 1 if i < split_idx else 2

        elites = sorted(
            [r for r in self.graph.rooms.values() if r.type == RoomType.Elite],
            key=lambda r: (r.y, r.x)
        )
        if elites:
            split_idx = int(len(elites) * self.elite_ratio)
            for i, e in enumerate(elites):
                e.star_rating = 3 if i < split_idx else 4

        boss = next((r for r in self.graph.rooms.values() if r.type == RoomType.Boss), None)
        if boss:
            boss.star_rating = 5
