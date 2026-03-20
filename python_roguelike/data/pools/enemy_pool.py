import random
from typing import Dict, List, Optional
from ..combatant.enemy_data import EnemyData


class EnemyPool:
    def __init__(self):
        self.enemies_by_id: Dict[str, EnemyData] = {}
        self.enemies_by_star: Dict[int, List[EnemyData]] = {}

    def initialize(self, all_enemies: List[EnemyData]):
        self.enemies_by_id.clear()
        self.enemies_by_star.clear()

        for enemy in all_enemies:
            self.enemies_by_id[enemy.id] = enemy
            if enemy.star_rating not in self.enemies_by_star:
                self.enemies_by_star[enemy.star_rating] = []
            self.enemies_by_star[enemy.star_rating].append(enemy)

    def get_enemy(self, id: str) -> Optional[EnemyData]:
        return self.enemies_by_id.get(id)

    def get_random_enemy_of_star(self, star: int, rng: random.Random) -> Optional[EnemyData]:
        lst = self.enemies_by_star.get(star)
        if lst:
            return rng.choice(lst)
        if star > 1:
            return self.get_random_enemy_of_star(star - 1, rng)
        return None

    def get_random_enemy_below_star(self, star_limit: int, rng: random.Random) -> Optional[EnemyData]:
        valid_stars = [k for k in self.enemies_by_star if k < star_limit]
        if not valid_stars:
            if self.enemies_by_star:
                min_star = min(self.enemies_by_star.keys())
                return self.get_random_enemy_of_star(min_star, rng)
            return None
        chosen_star = rng.choice(valid_stars)
        return self.get_random_enemy_of_star(chosen_star, rng)
