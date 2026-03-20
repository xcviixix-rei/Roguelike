import math
from typing import Callable, Optional
from .combatant import Combatant
from .active_effect import ActiveEffect
from ..data.combat_action_data import CombatActionData
from ..data.effect_data import EffectData
from ..data.status_effect_data import StatusEffectData
from ..data.deck_effect_data import DeckEffectData
from ..data.enums import (
    ActionType, StatusEffectType, DeckEffectType, IntensityType, DecayType
)


class ActionResolver:

    @staticmethod
    def resolve(
        action: CombatActionData,
        source: Combatant,
        target: Combatant,
        get_effect_by_id: Callable[[str], Optional[EffectData]]
    ):
        if action.type == ActionType.DealDamage:
            ActionResolver._apply_damage(action.value, source, target)
        elif action.type == ActionType.GainBlock:
            ActionResolver._apply_block(action.value, source, target)
        elif action.type == ActionType.GainHealth:
            ActionResolver._apply_heal(action.value, target)
        elif action.type == ActionType.ApplyStatusEffect:
            ActionResolver._apply_status_effect(action.effect_id, target, get_effect_by_id)
        elif action.type == ActionType.ApplyDeckEffect:
            ActionResolver._apply_deck_effect(action.effect_id, action.value, target, get_effect_by_id)

    @staticmethod
    def _apply_damage(base_damage: int, source: Combatant, target: Combatant):
        final_damage = float(base_damage)

        # Apply Strength
        for ae in source.active_effects:
            if isinstance(ae.source_data, StatusEffectData) and ae.source_data.effect_type == StatusEffectType.Strength:
                if ae.source_data.intensity_type == IntensityType.Flat:
                    final_damage += ae.source_data.intensity
                else:
                    final_damage *= (1 + ae.source_data.intensity / 100.0)

        # Apply Weakened (source)
        weak_ae = next(
            (ae for ae in source.active_effects
             if isinstance(ae.source_data, StatusEffectData) and ae.source_data.effect_type == StatusEffectType.Weakened),
            None
        )
        if weak_ae is not None:
            wd = weak_ae.source_data
            if wd.intensity_type == IntensityType.Percentage:
                final_damage *= (1 - wd.intensity / 100.0)

        # Apply Vulnerable (target)
        vul_ae = next(
            (ae for ae in target.active_effects
             if isinstance(ae.source_data, StatusEffectData) and ae.source_data.effect_type == StatusEffectType.Vulnerable),
            None
        )
        if vul_ae is not None:
            vd = vul_ae.source_data
            if vd.intensity_type == IntensityType.Percentage:
                final_damage *= (1 + vd.intensity / 100.0)

        damage_int = int(math.floor(final_damage))
        if damage_int < 0:
            damage_int = 0

        # Check Pierced
        has_pierced = any(
            isinstance(ae.source_data, StatusEffectData) and ae.source_data.effect_type == StatusEffectType.Pierced
            for ae in target.active_effects
        )
        if has_pierced:
            target.take_piercing_damage(damage_int)
        else:
            target.take_damage(damage_int)

    @staticmethod
    def _apply_block(base_block: int, source: Combatant, target: Combatant):
        final_block = float(base_block)

        frail_ae = next(
            (ae for ae in target.active_effects
             if isinstance(ae.source_data, StatusEffectData) and ae.source_data.effect_type == StatusEffectType.Frail),
            None
        )
        if frail_ae is not None:
            fd = frail_ae.source_data
            if fd.intensity_type == IntensityType.Percentage:
                final_block *= (1 - fd.intensity / 100.0)

        block_int = int(math.floor(final_block))
        target.gain_block(block_int)

    @staticmethod
    def _apply_heal(value: int, target: Combatant):
        target.heal(value)

    @staticmethod
    def _apply_status_effect(
        effect_id: str,
        target: Combatant,
        get_effect_by_id: Callable[[str], Optional[EffectData]]
    ):
        effect_data = get_effect_by_id(effect_id)
        if effect_data is not None and isinstance(effect_data, StatusEffectData):
            target.apply_effect(effect_data)

    @staticmethod
    def _apply_deck_effect(
        effect_id: str,
        value: int,
        target: Combatant,
        get_effect_by_id: Callable[[str], Optional[EffectData]]
    ):
        from .hero import Hero
        if not isinstance(target, Hero):
            return

        hero = target
        effect_data = get_effect_by_id(effect_id)
        if effect_data is not None and isinstance(effect_data, DeckEffectData):
            if effect_data.effect_type == DeckEffectType.DrawCard:
                hero.deck.draw_cards(value)
            elif effect_data.effect_type == DeckEffectType.DiscardCard:
                if hero.deck.hand:
                    hero.deck.discard_card_from_hand(hero.deck.hand[0])
            elif effect_data.effect_type == DeckEffectType.FreezeCard:
                pass  # TODO
            elif effect_data.effect_type == DeckEffectType.DuplicateCard:
                pass  # TODO
