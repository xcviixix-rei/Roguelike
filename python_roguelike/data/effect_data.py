from dataclasses import dataclass, field
from typing import Optional
from .enums import IntensityType, ApplyType, DecayType, TargetType


@dataclass
class EffectData:
    id: str = ""
    name: str = ""
    description: str = ""
    intensity: int = 0
    intensity_type: IntensityType = IntensityType.Flat
    duration: int = 1
    apply_type: ApplyType = ApplyType.RightAway
    decay: DecayType = DecayType.AfterXTURNS
    target: TargetType = TargetType.Self
