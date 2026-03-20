from dataclasses import dataclass, field
from typing import List
from .effect_data import EffectData


@dataclass
class RelicData:
    id: str = ""
    name: str = ""
    description: str = ""
    star_rating: int = 1
    is_boss_relic: bool = False
    effects: List[EffectData] = field(default_factory=list)
