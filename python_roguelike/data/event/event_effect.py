from dataclasses import dataclass, field
from typing import List, Optional
from ..enums import EventEffectType


@dataclass
class EventEffect:
    type: EventEffectType = EventEffectType.Quit
    value: int = 0
    parameter: Optional[str] = None
