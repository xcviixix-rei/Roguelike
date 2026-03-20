from dataclasses import dataclass, field
from typing import List
from .combat_action_data import CombatActionData
from .enums import CardType


@dataclass
class CardData:
    id: str = ""
    name: str = ""
    description: str = ""
    mana_cost: int = 0
    star_rating: int = 1
    type: CardType = CardType.Attack
    actions: List[CombatActionData] = field(default_factory=list)
