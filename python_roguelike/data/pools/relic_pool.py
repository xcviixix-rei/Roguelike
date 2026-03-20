import random
from typing import Dict, List, Optional
from ..relic_data import RelicData


class RelicPool:
    def __init__(self):
        self.relics_by_id: Dict[str, RelicData] = {}
        self.relics_by_star: Dict[int, List[RelicData]] = {}
        self.base_shop_costs: Dict[int, int] = {}

    def initialize(self, all_relics: List[RelicData]):
        self.relics_by_id.clear()
        self.relics_by_star.clear()

        for relic in all_relics:
            self.relics_by_id[relic.id] = relic
            if relic.star_rating not in self.relics_by_star:
                self.relics_by_star[relic.star_rating] = []
            self.relics_by_star[relic.star_rating].append(relic)

        self.base_shop_costs.clear()
        for i in range(1, 6):
            self.base_shop_costs[i] = 60 * i

    def get_relic(self, id: str) -> Optional[RelicData]:
        return self.relics_by_id.get(id)

    def get_random_relic_of_star(self, star: int, rng: random.Random) -> Optional[RelicData]:
        lst = self.relics_by_star.get(star)
        if lst:
            return rng.choice(lst)
        return None
