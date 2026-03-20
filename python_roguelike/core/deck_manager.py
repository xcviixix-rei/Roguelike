import random
from typing import List, Optional
from .combatant import Combatant
from ..data.card_data import CardData
from ..data.pools.card_pool import CardPool


class DeckManager:
    def __init__(self, rng: random.Random):
        self._rng = rng
        self.master_deck: List[CardData] = []
        self.draw_pile: List[CardData] = []
        self.hand: List[CardData] = []
        self.discard_pile: List[CardData] = []
        self.exhaust_pile: List[CardData] = []

    def initialize_master_deck(self, card_ids: List[str], pool: CardPool):
        self.master_deck.clear()
        for cid in card_ids:
            card = pool.get_card(cid)
            if card is not None:
                self.master_deck.append(card)

    def add_card_to_master_deck(self, card: CardData):
        self.master_deck.append(card)

    def remove_card_from_master_deck(self, card: CardData):
        if card in self.master_deck:
            self.master_deck.remove(card)

    def start_combat(self):
        self.draw_pile.clear()
        self.hand.clear()
        self.discard_pile.clear()
        self.exhaust_pile.clear()
        self.draw_pile.extend(self.master_deck)
        self._shuffle(self.draw_pile)

    def draw_cards(self, amount: int):
        for _ in range(amount):
            if not self.draw_pile:
                if not self.discard_pile:
                    break
                self._reshuffle_discard_into_draw()
            if not self.draw_pile:
                break
            card = self.draw_pile.pop(0)
            self.hand.append(card)

    def discard_card_from_hand(self, card: CardData):
        if card in self.hand:
            self.hand.remove(card)
            self.discard_pile.append(card)

    def discard_hand(self):
        self.discard_pile.extend(self.hand)
        self.hand.clear()

    def _reshuffle_discard_into_draw(self):
        self.draw_pile.extend(self.discard_pile)
        self.discard_pile.clear()
        self._shuffle(self.draw_pile)

    def _shuffle(self, lst: List[CardData]):
        n = len(lst)
        while n > 1:
            n -= 1
            k = self._rng.randint(0, n)
            lst[k], lst[n] = lst[n], lst[k]
