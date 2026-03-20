from dataclasses import dataclass
from .effect_data import EffectData
from .enums import StatusEffectType


@dataclass
class StatusEffectData(EffectData):
    effect_type: StatusEffectType = StatusEffectType.Vulnerable
