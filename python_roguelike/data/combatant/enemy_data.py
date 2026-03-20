from dataclasses import dataclass, field
from typing import List
from .combatant_data import CombatantData
from ..weighted_choice import WeightedChoice
from ..combat_action_data import CombatActionData


@dataclass
class EnemyData(CombatantData):
    star_rating: int = 1
    is_boss: bool = False
    action_set: List[WeightedChoice] = field(default_factory=list)
    special_ability_cooldown: int = 1
