from dataclasses import dataclass, field
from typing import Optional
from .enums import ActionType, TargetType


@dataclass
class CombatActionData:
    type: ActionType = ActionType.DealDamage
    value: int = 0
    target: TargetType = TargetType.Self
    effect_id: Optional[str] = None
