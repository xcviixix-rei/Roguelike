import random
from typing import List
from .shop_item import ShopItem
from ...data.card_data import CardData
from ...data.relic_data import RelicData
from ...data.pools.card_pool import CardPool
from ...data.pools.relic_pool import RelicPool


class ShopInventory:
    def __init__(self, card_pool: CardPool, relic_pool: RelicPool, rng: random.Random):
        self.cards_for_sale: List[ShopItem] = []
        self.relics_for_sale: List[ShopItem] = []

        for star in range(1, 6):
            card = card_pool.get_random_card_of_star(star, rng)
            if card is not None and star in card_pool.base_shop_costs:
                base_card_cost = card_pool.base_shop_costs[star]
                price = rng.randint(base_card_cost, base_card_cost + 29)
                self.cards_for_sale.append(ShopItem(item=card, price=price))

            relic = relic_pool.get_random_relic_of_star(star, rng)
            if relic is not None and star in relic_pool.base_shop_costs:
                base_relic_cost = relic_pool.base_shop_costs[star]
                price = rng.randint(base_relic_cost, base_relic_cost + 49)
                self.relics_for_sale.append(ShopItem(item=relic, price=price))
