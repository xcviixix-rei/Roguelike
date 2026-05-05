"""
hierarchical_applicator.py  –  Python port of HierarchicalApplicator.cs

Applies a HierarchicalGenome to cloned game data using multiplicative scaling.
"""

import copy
import math
from typing import Dict

from .hierarchical_genome import HierarchicalGenome, ScalingType
from ..data.enums import ActionType, RoomType, CardType
from ..data.pools.card_pool import CardPool
from ..data.pools.relic_pool import RelicPool
from ..data.pools.enemy_pool import EnemyPool
from ..data.combatant.hero_data import HeroData


def apply(
    genome: HierarchicalGenome,
    card_pool: CardPool,
    enemy_pool: EnemyPool,
    relic_pool: RelicPool,
    hero: HeroData,
    current_floor: int = 1,
) -> None:
    """Apply all genome scalars to cloned game data (mutates in place)."""
    _apply_to_hero(genome, hero)
    _apply_to_cards(genome, card_pool, current_floor)
    _apply_to_enemies(genome, enemy_pool, current_floor)
    _apply_to_economy(genome, card_pool, relic_pool)


def apply_for_floor(
    genome: HierarchicalGenome,
    card_pool: CardPool,
    enemy_pool: EnemyPool,
    floor: int,
) -> None:
    """Apply genome for a specific floor (dynamic scaling)."""
    _apply_to_cards(genome, card_pool, floor)
    _apply_to_enemies(genome, enemy_pool, floor)


def _apply_to_hero(genome: HierarchicalGenome, hero: HeroData) -> None:
    hero.starting_health = max(30, round(hero.starting_health * genome.hero_health_scalar))
    hero.starting_gold = max(50, round(hero.starting_gold * genome.hero_start_gold_scalar))
    hero.starting_mana = max(2, min(4, hero.starting_mana + genome.hero_mana_offset))


def _apply_to_cards(genome: HierarchicalGenome, card_pool: CardPool, floor: int) -> None:
    for card in card_pool.cards_by_id.values():
        for action in card.actions:
            if action.type == ActionType.DealDamage:
                action.value = _calc_card_damage(genome, card, action.value, floor)
            elif action.type == ActionType.GainBlock:
                action.value = _calc_card_block(genome, card, action.value, floor)
            elif action.type == ActionType.ApplyStatusEffect:
                action.value = _calc_card_effect_stacks(genome, card, action.value)

        card.mana_cost = _calc_card_cost(genome, card, card.mana_cost)


def _calc_card_damage(genome, card, base_value: int, floor: int) -> int:
    scalar = genome.global_damage_multiplier
    scalar *= genome.card_type_scalars.get(card.type.value, 1.0)
    scalar *= genome.card_star_scalars.get(card.star_rating, 1.0)
    scalar *= genome.get_progression_scalar(floor, ScalingType.Damage)

    ovr = genome.card_damage_overrides.get(card.id)
    if ovr is not None:
        scalar *= ovr

    return max(1, round(base_value * scalar))


def _calc_card_block(genome, card, base_value: int, floor: int) -> int:
    scalar = genome.global_block_multiplier
    scalar *= genome.card_type_scalars.get(card.type.value, 1.0)
    scalar *= genome.card_star_scalars.get(card.star_rating, 1.0)
    scalar *= genome.get_progression_scalar(floor, ScalingType.Block)
    return max(1, round(base_value * scalar))


def _calc_card_effect_stacks(genome, card, base_value: int) -> int:
    scalar = genome.card_star_scalars.get(card.star_rating, 1.0)
    return max(1, round(base_value * scalar))


def _calc_card_cost(genome, card, base_cost: int) -> int:
    scalar = genome.global_mana_cost_multiplier
    ovr = genome.card_mana_cost_overrides.get(card.id)
    if ovr is not None:
        scalar *= ovr
    return max(0, round(base_cost * scalar))


def _apply_to_enemies(genome: HierarchicalGenome, enemy_pool: EnemyPool, floor: int) -> None:
    floor_diff = genome.get_floor_difficulty_multiplier(floor)

    for enemy in enemy_pool.enemies_by_id.values():
        health_scalar = genome.global_health_multiplier
        health_scalar *= genome.enemy_star_scalars.get(enemy.star_rating, 1.0)
        health_scalar *= genome.get_progression_scalar(floor, ScalingType.Health)
        health_scalar *= floor_diff

        hp_ovr = genome.enemy_health_overrides.get(enemy.id)
        if hp_ovr is not None:
            health_scalar *= hp_ovr

        enemy.starting_health = max(1, round(enemy.starting_health * health_scalar))

        for weighted_action in enemy.action_set:
            action = weighted_action.item
            if action.type == ActionType.DealDamage:
                dmg_scalar = genome.global_damage_multiplier
                dmg_scalar *= genome.enemy_star_scalars.get(enemy.star_rating, 1.0)
                dmg_scalar *= genome.get_progression_scalar(floor, ScalingType.Damage)
                dmg_scalar *= floor_diff
                action.value = max(1, round(action.value * dmg_scalar))
            elif action.type == ActionType.GainBlock:
                blk_scalar = genome.global_block_multiplier
                blk_scalar *= genome.enemy_star_scalars.get(enemy.star_rating, 1.0)
                blk_scalar *= genome.get_progression_scalar(floor, ScalingType.Block)
                action.value = max(1, round(action.value * blk_scalar))


def _apply_to_economy(
    genome: HierarchicalGenome,
    card_pool: CardPool,
    relic_pool: RelicPool,
) -> None:
    for star in range(1, 6):
        if star in card_pool.base_shop_costs:
            card_pool.base_shop_costs[star] = round(
                card_pool.base_shop_costs[star] * genome.global_gold_multiplier
            )
        if star in relic_pool.base_shop_costs:
            relic_pool.base_shop_costs[star] = round(
                relic_pool.base_shop_costs[star] * genome.global_gold_multiplier
            )
