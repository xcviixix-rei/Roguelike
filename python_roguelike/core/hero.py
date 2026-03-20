import random
from typing import List, Optional
from .combatant import Combatant
from .deck_manager import DeckManager
from .active_effect import ActiveEffect
from ..data.combatant.hero_data import HeroData
from ..data.relic_data import RelicData
from ..data.status_effect_data import StatusEffectData
from ..data.enums import StatusEffectType


class Hero(Combatant):
    def __init__(self, source_data: HeroData, rng: random.Random):
        super().__init__(source_data)
        self.source_hero_data: HeroData = source_data
        self.deck: DeckManager = DeckManager(rng)
        self.max_mana: int = source_data.starting_mana
        self.current_mana: int = self.max_mana
        self.current_gold: int = source_data.starting_gold
        self.relics: List[RelicData] = []

    def start_turn(self):
        self.block = 0
        self.current_mana = self.max_mana

        for ae in self.active_effects:
            if (isinstance(ae.source_data, StatusEffectData) and
                    ae.source_data.effect_type == StatusEffectType.Philosophical):
                self.current_mana += ae.source_data.intensity
                break

        self.deck.draw_cards(self.source_hero_data.starting_hand_size)
