from typing import Dict, Optional
from ..effect_data import EffectData


class EffectPool:
    def __init__(self):
        self.effects_by_id: Dict[str, EffectData] = {}

    def get_effect(self, id: str) -> Optional[EffectData]:
        return self.effects_by_id.get(id)
