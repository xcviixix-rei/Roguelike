from dataclasses import dataclass, field
from typing import List
from .combatant_data import CombatantData


@dataclass
class HeroData(CombatantData):
    starting_gold: int = 0
    starting_mana: int = 3
    starting_hand_size: int = 5
    starting_deck_card_ids: List[str] = field(default_factory=list)
    starting_relic_id: str = ""
