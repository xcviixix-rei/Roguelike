from typing import List
from .active_effect import ActiveEffect
from ..data.combatant.combatant_data import CombatantData
from ..data.effect_data import EffectData
from ..data.enums import DecayType


class Combatant:
    def __init__(self, source_data: CombatantData):
        self.source_data: CombatantData = source_data
        self.max_health: int = source_data.starting_health
        self.current_health: int = self.max_health
        self.block: int = 0
        self.active_effects: List[ActiveEffect] = []

    def increase_max_health(self, amount: int):
        if amount > 0:
            self.max_health += amount
            self.heal(amount)

    def take_damage(self, amount: int):
        if amount <= 0:
            return
        damage_to_block = min(amount, self.block)
        self.block -= damage_to_block
        remaining = amount - damage_to_block
        if remaining > 0:
            self.current_health -= remaining
            if self.current_health < 0:
                self.current_health = 0

    def take_piercing_damage(self, amount: int):
        if amount <= 0:
            return
        self.current_health -= amount
        if self.current_health < 0:
            self.current_health = 0

    def gain_block(self, amount: int):
        if amount > 0:
            self.block += amount

    def apply_effect(self, effect_data: EffectData):
        existing = next(
            (e for e in self.active_effects if e.source_data.id == effect_data.id),
            None
        )
        if existing is not None:
            if effect_data.decay == DecayType.AfterXTURNS:
                existing.remaining_duration = effect_data.duration
        else:
            self.active_effects.append(ActiveEffect(effect_data))

    def heal(self, amount: int):
        if amount > 0:
            self.current_health = min(self.current_health + amount, self.max_health)

    def tick_down_effects(self):
        expired = [e for e in self.active_effects if e.tick_down()]
        for e in expired:
            self.active_effects.remove(e)

    def reset_for_new_combat(self):
        self.block = 0
        self.active_effects = [
            e for e in self.active_effects
            if e.source_data.decay == DecayType.Permanent
        ]
