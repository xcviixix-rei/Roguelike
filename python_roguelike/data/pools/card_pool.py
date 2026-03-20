import random
from typing import Dict, List, Optional
from ..card_data import CardData


class CardPool:
    def __init__(self):
        self.cards_by_id: Dict[str, CardData] = {}
        self.cards_by_star: Dict[int, List[CardData]] = {}
        self.base_shop_costs: Dict[int, int] = {}

    def initialize(self, all_cards: List[CardData]):
        self.cards_by_id.clear()
        self.cards_by_star.clear()

        for card in all_cards:
            self.cards_by_id[card.id] = card
            if card.star_rating not in self.cards_by_star:
                self.cards_by_star[card.star_rating] = []
            self.cards_by_star[card.star_rating].append(card)

        self.base_shop_costs.clear()
        for i in range(1, 6):
            self.base_shop_costs[i] = 40 * i

    def get_card(self, id: str) -> Optional[CardData]:
        return self.cards_by_id.get(id)

    def get_random_card_of_star(self, star: int, rng: random.Random) -> Optional[CardData]:
        lst = self.cards_by_star.get(star)
        if lst:
            return rng.choice(lst)
        return None

    def get_random_card_up_to_star(self, max_star: int, rng: random.Random) -> Optional[CardData]:
        valid_stars = [k for k in self.cards_by_star if k <= max_star]
        if not valid_stars:
            return None
        chosen_star = rng.choice(valid_stars)
        return self.get_random_card_of_star(chosen_star, rng)
