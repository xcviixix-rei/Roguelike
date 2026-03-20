import random
import math
from typing import List, Dict, Callable, Optional, Iterable
from .hero import Hero
from .enemy import Enemy
from .combatant import Combatant
from .action_resolver import ActionResolver
from ..data.combatant.enemy_data import EnemyData
from ..data.card_data import CardData
from ..data.effect_data import EffectData
from ..data.status_effect_data import StatusEffectData
from ..data.deck_effect_data import DeckEffectData
from ..data.combat_action_data import CombatActionData
from ..data.enums import (
    CombatState, CardType, TargetType, ActionType,
    StatusEffectType, ApplyType, DecayType, IntensityType
)


class CombatManager:
    def __init__(
        self,
        hero: Hero,
        enemy_templates: List[EnemyData],
        rng: random.Random,
        effect_lookup: Callable[[str], Optional[EffectData]]
    ):
        self.the_hero: Hero = hero
        self.rng: random.Random = rng
        self.get_effect_by_id: Callable = effect_lookup
        self.state: CombatState = CombatState.Ongoing_PlayerTurn
        self.turn_number: int = 0
        self._cards_played_this_turn: int = 0
        self._attacks_played_this_turn: int = 0

        self.enemies: List[Enemy] = [Enemy(t, rng) for t in enemy_templates]
        self.current_enemy_intents: Dict[Enemy, CombatActionData] = {}

        self._on_card_played_callbacks = []

    def add_card_played_listener(self, callback):
        self._on_card_played_callbacks.append(callback)

    def start_combat(self):
        self.the_hero.reset_for_new_combat()
        self.the_hero.deck.start_combat()
        self._begin_player_turn()
        self._apply_relic_effects(self.the_hero, ApplyType.StartOfCombat)

    def play_card(self, card: CardData, target: Optional[Enemy]) -> bool:
        effective_mana_cost = card.mana_cost

        if self.state != CombatState.Ongoing_PlayerTurn or self.the_hero.current_mana < effective_mana_cost:
            return False

        self.the_hero.current_mana -= effective_mana_cost

        for action in card.actions:
            targets = list(self._get_targets(self.the_hero, target, action.target))
            for t in targets:
                ActionResolver.resolve(action, self.the_hero, t, self.get_effect_by_id)

        self.the_hero.deck.discard_card_from_hand(card)

        for cb in self._on_card_played_callbacks:
            cb(card)

        self._cards_played_this_turn += 1
        if card.type == CardType.Attack:
            self._attacks_played_this_turn += 1

        self._check_combat_status()
        return True

    def end_player_turn(self):
        if self.state != CombatState.Ongoing_PlayerTurn:
            return
        self.the_hero.deck.discard_hand()
        self.the_hero.tick_down_effects()
        self._begin_enemy_turn()

    def _begin_player_turn(self):
        self.turn_number += 1
        self.state = CombatState.Ongoing_PlayerTurn

        if self.turn_number > 100:
            self.state = CombatState.Defeat
            return

        self._check_combat_status()
        if self.state != CombatState.Ongoing_PlayerTurn:
            return

        self._cards_played_this_turn = 0
        self._attacks_played_this_turn = 0

        self.the_hero.start_turn()
        self._apply_relic_effects(self.the_hero, ApplyType.StartOfTurn)

        self.current_enemy_intents.clear()
        for enemy in self.enemies:
            if enemy.current_health > 0:
                self.current_enemy_intents[enemy] = enemy.peek_next_action()

    def _begin_enemy_turn(self):
        self.state = CombatState.Ongoing_EnemyTurn

        for enemy in self.enemies:
            if enemy.current_health <= 0:
                continue
            action = enemy.get_next_action()
            targets = list(self._get_targets(enemy, self.the_hero, action.target))
            for t in targets:
                ActionResolver.resolve(action, enemy, t, self.get_effect_by_id)
            self._check_combat_status()
            if self.state == CombatState.Defeat:
                return

        for enemy in self.enemies:
            enemy.tick_down_effects()
            enemy.tick_cooldowns()

        self._check_combat_status()

        if self.state == CombatState.Ongoing_EnemyTurn:
            self._begin_player_turn()

    def _check_combat_status(self):
        if self.state in (CombatState.Victory, CombatState.Defeat):
            return
        if self.the_hero.current_health <= 0:
            self.state = CombatState.Defeat
        elif all(e.current_health <= 0 for e in self.enemies):
            self.state = CombatState.Victory

    def _get_targets(
        self,
        source: Combatant,
        chosen_target: Optional[Combatant],
        target_type: TargetType
    ) -> Iterable[Combatant]:
        living_enemies = [e for e in self.enemies if e.current_health > 0]

        if target_type == TargetType.Self:
            return [source]

        elif target_type == TargetType.SingleOpponent:
            if isinstance(source, Hero):
                return [chosen_target] if chosen_target is not None else []
            return [self.the_hero]

        elif target_type == TargetType.AllOpponents:
            if isinstance(source, Hero):
                return list(living_enemies)
            return [self.the_hero]

        elif target_type == TargetType.RandomOpponent:
            if isinstance(source, Hero) and living_enemies:
                return [self.rng.choice(living_enemies)]
            return [self.the_hero]

        return []

    def _apply_relic_effects(self, hero: Hero, apply_type: ApplyType):
        for relic in hero.relics:
            for effect in relic.effects:
                if effect.apply_type != apply_type:
                    continue
                if isinstance(effect, StatusEffectData):
                    targets = list(self._get_targets_for_relic_effect(effect.target))
                    for t in targets:
                        if effect.effect_type == StatusEffectType.ImmediateBlock:
                            t.gain_block(effect.intensity)
                        else:
                            t.apply_effect(effect)
                elif isinstance(effect, DeckEffectData):
                    action = CombatActionData(
                        type=ActionType.ApplyDeckEffect,
                        value=effect.intensity,
                        target=TargetType.Self,
                        effect_id=effect.id
                    )
                    ActionResolver.resolve(action, hero, hero, self.get_effect_by_id)

    def _get_targets_for_relic_effect(self, target_type: TargetType) -> Iterable[Combatant]:
        if target_type == TargetType.Self:
            return [self.the_hero]
        elif target_type == TargetType.AllOpponents:
            return [e for e in self.enemies if e.current_health > 0]
        elif target_type in (TargetType.RandomOpponent, TargetType.SingleOpponent):
            living = [e for e in self.enemies if e.current_health > 0]
            if living:
                return [self.rng.choice(living)]
            return []
        return [self.the_hero]
