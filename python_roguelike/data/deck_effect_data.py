from dataclasses import dataclass
from .effect_data import EffectData
from .enums import DeckEffectType


@dataclass
class DeckEffectData(EffectData):
    effect_type: DeckEffectType = DeckEffectType.DrawCard
