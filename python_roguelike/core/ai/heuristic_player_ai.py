import math
from typing import Optional, List
from .i_player_agent import IPlayerAgent, CombatDecision, ShopDecision
from ..hero import Hero
from ..enemy import Enemy
from ..active_effect import ActiveEffect
from ...data.status_effect_data import StatusEffectData
from ...data.enums import (
    CombatState, CombatActionType, ShopActionType, RoomType,
    ActionType, CardType, TargetType, StatusEffectType, IntensityType,
    EventEffectType, DecayType
)


class HeuristicPlayerAI(IPlayerAgent):

    # ─── MAP PATHING ─────────────────────────────────────────────────────────

    def choose_map_node(self, run) -> int:
        possible = run.the_map.get_possible_next_nodes()
        if not possible:
            return -1

        best_room = None
        best_score = float("-inf")
        for room in possible:
            score = self._evaluate_room(run, room)
            if score > best_score:
                best_score = score
                best_room = room

        return best_room.id if best_room else possible[0].id

    def _evaluate_room(self, run, room) -> float:
        hp_percent = run.the_hero.current_health / run.the_hero.max_health
        rt = room.type

        if rt == RoomType.Rest:
            return 50.0 if hp_percent < 0.5 else 10.0

        elif rt == RoomType.Elite:
            val = room.star_rating * 10.0
            if hp_percent > 0.5:
                return val
            if hp_percent < 0.35:
                return -val
            return val * 0.5

        elif rt == RoomType.Monster:
            val = room.star_rating * 10.0
            if hp_percent > 0.35:
                return val
            if hp_percent < 0.2:
                return -val
            return val * 0.5

        elif rt == RoomType.Shop:
            return 35.0 if run.the_hero.current_gold > 150 else 5.0

        elif rt == RoomType.Event:
            return 15.0

        elif rt == RoomType.Boss:
            return 1000.0

        return 0.0

    # ─── COMBAT TACTICS ──────────────────────────────────────────────────────

    def get_combat_decision(self, run) -> CombatDecision:
        hero = run.the_hero
        combat = run.current_combat

        if combat is None or combat.state != CombatState.Ongoing_PlayerTurn:
            return CombatDecision.end_turn()

        enemies = [e for e in combat.enemies if e.current_health > 0]

        if not enemies or not hero.deck.hand or hero.current_mana <= 0:
            return CombatDecision.end_turn()

        has_affordable = any(c.mana_cost <= hero.current_mana for c in hero.deck.hand)
        if not has_affordable:
            return CombatDecision.end_turn()

        total_incoming = self._calculate_incoming_damage(run, enemies)
        needed_block = max(0, total_incoming - hero.block)

        # 1st PRIORITY: SURVIVAL
        if needed_block > 0:
            defensive = self._find_best_defensive_move(run, hero, enemies, needed_block)
            if defensive is not None:
                return defensive

        # 2nd PRIORITY: LETHAL
        lethal = self._find_lethal_move(run, hero, enemies)
        if lethal is not None:
            return lethal

        # FALLBACK: BEST GENERAL MOVE
        final = self._get_best_general_move(run, hero, enemies)

        if final.type == CombatActionType.PlayCard:
            if (final.hand_index < 0 or final.hand_index >= len(hero.deck.hand) or
                    final.target_index < 0 or final.target_index >= len(combat.enemies)):
                return CombatDecision.end_turn()

        return final

    def _find_lethal_move(self, run, hero: Hero, enemies: List[Enemy]) -> Optional[CombatDecision]:
        for i, card in enumerate(hero.deck.hand):
            if card.mana_cost > hero.current_mana or card.type != CardType.Attack:
                continue
            dmg_action = next((a for a in card.actions if a.type == ActionType.DealDamage), None)
            if dmg_action:
                for e_idx, enemy in enumerate(enemies):
                    predicted = self._predict_damage(dmg_action.value, hero, enemy)
                    if enemy.current_health <= predicted:
                        actual_idx = run.current_combat.enemies.index(enemy)
                        return CombatDecision.play(i, actual_idx)
        return None

    def _get_best_general_move(self, run, hero: Hero, enemies: List[Enemy]) -> CombatDecision:
        best_card_idx = -1
        best_target_idx = 0
        best_score = -1.0

        for i, card in enumerate(hero.deck.hand):
            if card.mana_cost > hero.current_mana:
                continue

            if any(a.target == TargetType.SingleOpponent for a in card.actions):
                target_enemy = min(enemies, key=lambda e: e.current_health)
                target_idx = run.current_combat.enemies.index(target_enemy)
                score = self._score_card(card, hero, target_enemy)
                score_per_mana = score / max(card.mana_cost, 1)
                if score_per_mana > best_score:
                    best_score = score_per_mana
                    best_card_idx = i
                    best_target_idx = target_idx
            else:
                score = self._score_card(card, hero, None)
                score_per_mana = score / max(card.mana_cost, 1)
                if score_per_mana > best_score:
                    best_score = score_per_mana
                    best_card_idx = i
                    best_target_idx = 0

        if best_card_idx != -1:
            return CombatDecision.play(best_card_idx, best_target_idx)
        return CombatDecision.end_turn()

    def _score_card(self, card, hero: Hero, target: Optional[Enemy]) -> float:
        score = 1.0

        if card.type == CardType.Power:
            score += 20

        elif card.type == CardType.Attack:
            if target is not None:
                for action in card.actions:
                    if action.type == ActionType.DealDamage:
                        score += self._predict_damage(action.value, hero, target)

        elif card.type == CardType.Skill:
            for action in card.actions:
                if action.type == ActionType.GainBlock:
                    score += action.value * 0.5
                elif action.type == ActionType.ApplyDeckEffect and action.effect_id and "draw" in action.effect_id:
                    score += action.value * 10
                elif action.type == ActionType.ApplyStatusEffect:
                    score += 15

        return score

    def _predict_damage(self, base_damage: int, hero: Hero, target: Enemy) -> int:
        final = float(base_damage)

        strength_ae = next(
            (ae for ae in hero.active_effects
             if isinstance(ae.source_data, StatusEffectData) and ae.source_data.effect_type == StatusEffectType.Strength),
            None
        )
        if strength_ae:
            final += strength_ae.source_data.intensity

        weakened_ae = next(
            (ae for ae in hero.active_effects
             if isinstance(ae.source_data, StatusEffectData) and ae.source_data.effect_type == StatusEffectType.Weakened),
            None
        )
        if weakened_ae:
            final *= (1 - weakened_ae.source_data.intensity / 100.0)

        vulnerable_ae = next(
            (ae for ae in target.active_effects
             if isinstance(ae.source_data, StatusEffectData) and ae.source_data.effect_type == StatusEffectType.Vulnerable),
            None
        )
        if vulnerable_ae:
            final *= (1 + vulnerable_ae.source_data.intensity / 100.0)

        return int(math.floor(final))

    def _find_best_defensive_move(self, run, hero: Hero, enemies: List[Enemy], needed_block: int) -> Optional[CombatDecision]:
        frail_ae = next(
            (ae for ae in hero.active_effects
             if isinstance(ae.source_data, StatusEffectData) and ae.source_data.effect_type == StatusEffectType.Frail),
            None
        )

        best_weak_target = None
        max_damage_from_single = 0
        for enemy in enemies:
            intent = run.current_combat.current_enemy_intents.get(enemy)
            if intent and intent.type == ActionType.DealDamage:
                already_weak = any(
                    isinstance(ae.source_data, StatusEffectData) and ae.source_data.effect_type == StatusEffectType.Weakened
                    for ae in enemy.active_effects
                )
                if not already_weak:
                    dmg = int(math.floor(self._predict_enemy_damage(run, enemy, hero)))
                    if dmg > max_damage_from_single:
                        max_damage_from_single = dmg
                        best_weak_target = enemy

        best_card_idx = -1
        best_target_idx = 0
        best_efficiency = -1.0

        for i, card in enumerate(hero.deck.hand):
            if card.mana_cost > hero.current_mana:
                continue

            block_gain = 0
            block_action = next((a for a in card.actions if a.type == ActionType.GainBlock), None)
            if block_action:
                final_block = float(block_action.value)
                if frail_ae:
                    fd = frail_ae.source_data
                    final_block *= (1 - fd.intensity / 100.0)
                block_gain = int(math.floor(final_block))

            prevented_damage = 0
            potential_target = 0

            weak_action = next(
                (a for a in card.actions
                 if a.type == ActionType.ApplyStatusEffect and a.effect_id and "weakened" in a.effect_id),
                None
            )
            if weak_action and best_weak_target is not None:
                original_dmg = self._predict_enemy_damage(run, best_weak_target, hero)
                effect_data = run.effect_pool.get_effect(weak_action.effect_id)
                if effect_data and isinstance(effect_data, StatusEffectData):
                    from ..active_effect import ActiveEffect
                    from ...data.status_effect_data import StatusEffectData as SED
                    temp_effect = SED(
                        id="__temp__",
                        effect_type=StatusEffectType.Weakened,
                        intensity=effect_data.intensity,
                        intensity_type=IntensityType.Percentage,
                        duration=1,
                    )
                    from ...data.enums import DecayType
                    temp_effect.decay = DecayType.AfterXTURNS
                    temp_ae = ActiveEffect(temp_effect)
                    best_weak_target.active_effects.append(temp_ae)
                    weakened_dmg = self._predict_enemy_damage(run, best_weak_target, hero)
                    best_weak_target.active_effects.remove(temp_ae)
                    prevented_damage = int(math.floor(original_dmg)) - int(math.floor(weakened_dmg))
                    potential_target = run.current_combat.enemies.index(best_weak_target)

            total_def_value = block_gain + prevented_damage
            if total_def_value > 0:
                efficiency = total_def_value / max(1, card.mana_cost)
                if efficiency > best_efficiency:
                    best_efficiency = efficiency
                    best_card_idx = i
                    best_target_idx = potential_target

        if best_card_idx != -1:
            return CombatDecision.play(best_card_idx, best_target_idx)
        return None

    def _predict_enemy_damage(self, run, enemy: Enemy, hero: Hero) -> float:
        intent = run.current_combat.current_enemy_intents.get(enemy)
        if intent is None or intent.type != ActionType.DealDamage:
            return 0.0

        dmg = float(intent.value)

        strength_ae = next(
            (ae for ae in enemy.active_effects
             if isinstance(ae.source_data, StatusEffectData) and ae.source_data.effect_type == StatusEffectType.Strength),
            None
        )
        if strength_ae:
            dmg += strength_ae.source_data.intensity

        weakened_ae = next(
            (ae for ae in enemy.active_effects
             if isinstance(ae.source_data, StatusEffectData) and ae.source_data.effect_type == StatusEffectType.Weakened),
            None
        )
        if weakened_ae:
            dmg *= (1 - weakened_ae.source_data.intensity / 100.0)

        vulnerable_ae = next(
            (ae for ae in hero.active_effects
             if isinstance(ae.source_data, StatusEffectData) and ae.source_data.effect_type == StatusEffectType.Vulnerable),
            None
        )
        if vulnerable_ae:
            dmg *= (1 + vulnerable_ae.source_data.intensity / 100.0)

        return dmg

    def _calculate_incoming_damage(self, run, enemies: List[Enemy]) -> int:
        return int(sum(math.floor(self._predict_enemy_damage(run, e, run.the_hero)) for e in enemies))

    # ─── SHOPPING ────────────────────────────────────────────────────────────

    def get_shop_decision(self, run) -> ShopDecision:
        shop = run.current_shop
        gold = run.the_hero.current_gold

        for i, item in enumerate(shop.relics_for_sale):
            if not item.is_sold and item.price <= gold and item.item.star_rating >= 4:
                return ShopDecision.buy_relic(i)

        for i, item in enumerate(shop.cards_for_sale):
            if not item.is_sold and item.price <= gold and item.item.star_rating >= 4:
                return ShopDecision.buy_card(i)

        if gold > 200:
            for i, item in enumerate(shop.relics_for_sale):
                if not item.is_sold and item.price <= gold and item.item.star_rating >= 3:
                    return ShopDecision.buy_relic(i)

        return ShopDecision.leave()

    # ─── EVENTS & REWARDS ────────────────────────────────────────────────────

    def choose_event_option(self, run) -> int:
        choices = run.current_event.choices
        best_idx = 0
        best_score = float("-inf")

        for i, choice in enumerate(choices):
            score = sum(self._evaluate_event_effect(run, eff) for eff in choice.effects)
            if score > best_score:
                best_score = score
                best_idx = i

        return best_idx

    def _evaluate_event_effect(self, run, effect) -> float:
        hp_ratio = run.the_hero.current_health / run.the_hero.max_health
        t = effect.type
        if t == EventEffectType.GainGold:
            return effect.value * 0.5
        elif t == EventEffectType.LoseGold:
            return -effect.value * 0.5
        elif t == EventEffectType.HealHP:
            return effect.value * 2.0 if hp_ratio < 0.5 else effect.value * 1.0
        elif t == EventEffectType.LoseHP:
            return -effect.value * 3.0 if hp_ratio < 0.3 else -effect.value * 1.2
        elif t == EventEffectType.GainCard:
            return 25.0
        elif t == EventEffectType.RemoveCard:
            return 40.0
        elif t == EventEffectType.GainRelic:
            return 60.0
        return 0.0

    def choose_card_reward(self, run) -> int:
        choices = run.card_reward_choices
        if not choices:
            return -1

        best_idx = 0
        best_star = -1
        for i, card in enumerate(choices):
            if card.star_rating > best_star:
                best_star = card.star_rating
                best_idx = i

        return best_idx
