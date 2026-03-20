from ..data.effect_data import EffectData
from ..data.enums import DecayType


class ActiveEffect:
    def __init__(self, source_data: EffectData):
        self.source_data: EffectData = source_data

        if source_data.decay == DecayType.AfterXTURNS:
            self.remaining_duration: int = source_data.duration
        else:
            self.remaining_duration: int = 2_147_483_647  # int max for permanent

    def tick_down(self) -> bool:
        """Returns True if the effect has expired."""
        if self.source_data.decay == DecayType.AfterXTURNS:
            self.remaining_duration -= 1
            return self.remaining_duration <= 0
        return False
