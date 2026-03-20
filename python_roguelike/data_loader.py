"""
data_loader.py  –  Loads GameData.json and populates all data pools.
"""

import json
import os
from typing import Dict

from .data.enums import (
    TargetType, ApplyType, IntensityType, DecayType,
    CardType, ActionType, StatusEffectType, DeckEffectType,
    EventEffectType, RoomType
)
from .data.card_data import CardData
from .data.combat_action_data import CombatActionData
from .data.status_effect_data import StatusEffectData
from .data.deck_effect_data import DeckEffectData
from .data.relic_data import RelicData
from .data.room_data import RoomData
from .data.weighted_choice import WeightedChoice
from .data.combatant.enemy_data import EnemyData
from .data.combatant.hero_data import HeroData
from .data.event.event_effect import EventEffect
from .data.event.event_choice import EventChoice
from .data.event.event_choice_set import EventChoiceSet
from .data.pools.card_pool import CardPool
from .data.pools.effect_pool import EffectPool
from .data.pools.enemy_pool import EnemyPool
from .data.pools.event_pool import EventPool
from .data.pools.relic_pool import RelicPool


def _parse_action(d: dict) -> CombatActionData:
    return CombatActionData(
        type=ActionType(d["Type"]),
        value=d.get("Value", 0),
        target=TargetType(d["Target"]),
        effect_id=d.get("EffectId")
    )


def _parse_status_effect(d: dict) -> StatusEffectData:
    e = StatusEffectData()
    e.id = d["Id"]
    e.name = d.get("Name", "")
    e.description = d.get("Description", "")
    e.intensity = d.get("Intensity", 0)
    e.intensity_type = IntensityType(d.get("IntensityType", "Flat"))
    e.duration = d.get("Duration", 1)
    e.apply_type = ApplyType(d.get("ApplyType", "RightAway"))
    e.decay = DecayType(d.get("Decay", "AfterXTURNS"))
    e.target = TargetType(d.get("Target", "Self"))
    e.effect_type = StatusEffectType(d["EffectType"])
    return e


def _parse_deck_effect(d: dict) -> DeckEffectData:
    e = DeckEffectData()
    e.id = d["Id"]
    e.name = d.get("Name", "")
    e.description = d.get("Description", "")
    e.intensity = d.get("Intensity", 0)
    e.intensity_type = IntensityType(d.get("IntensityType", "Flat"))
    e.duration = d.get("Duration", 1)
    e.apply_type = ApplyType(d.get("ApplyType", "RightAway"))
    e.decay = DecayType(d.get("Decay", "AfterXTURNS"))
    e.target = TargetType(d.get("Target", "Self"))
    e.effect_type = DeckEffectType(d["EffectType"])
    return e


def load_game_data(json_path: str | None = None):
    """
    Load and parse GameData.json.

    Returns a tuple:
        (card_pool, relic_pool, enemy_pool, effect_pool, event_pool,
         room_configs, hero_data)
    """
    if json_path is None:
        json_path = os.path.join(os.path.dirname(__file__), "GameData.json")

    with open(json_path, "r", encoding="utf-8") as f:
        data = json.load(f)

    # ── Effect Pool ──────────────────────────────────────────────────────────
    effect_pool = EffectPool()

    for se_dict in data.get("StatusEffects", []):
        se = _parse_status_effect(se_dict)
        effect_pool.effects_by_id[se.id] = se

    for de_dict in data.get("DeckEffects", []):
        de = _parse_deck_effect(de_dict)
        effect_pool.effects_by_id[de.id] = de

    # ── Card Pool ────────────────────────────────────────────────────────────
    card_pool = CardPool()
    cards = []
    for cd in data.get("Cards", []):
        card = CardData()
        card.id = cd["Id"]
        card.name = cd.get("Name", "")
        card.description = cd.get("Description", "")
        card.mana_cost = cd.get("ManaCost", 0)
        card.star_rating = cd.get("StarRating", 1)
        card.type = CardType(cd.get("Type", "Attack"))
        card.actions = [_parse_action(a) for a in cd.get("Actions", [])]
        cards.append(card)
    card_pool.initialize(cards)

    # ── Enemy Pool ───────────────────────────────────────────────────────────
    enemy_pool = EnemyPool()
    enemies = []
    for ed in data.get("Enemies", []):
        enemy = EnemyData()
        enemy.id = ed["Id"]
        enemy.name = ed.get("Name", "")
        enemy.starting_health = ed.get("StartingHealth", 10)
        enemy.starting_strength = ed.get("StartingStrength", 0)
        enemy.star_rating = ed.get("StarRating", 1)
        enemy.is_boss = ed.get("IsBoss", False)
        enemy.special_ability_cooldown = ed.get("SpecialAbilityCooldown", 1)
        action_set = []
        for wa in ed.get("ActionSet", []):
            action = _parse_action(wa["Action"])
            action_set.append(WeightedChoice(item=action, weight=wa["Weight"]))
        enemy.action_set = action_set
        enemies.append(enemy)
    enemy_pool.initialize(enemies)

    # ── Relic Pool ───────────────────────────────────────────────────────────
    relic_pool = RelicPool()
    relics = []
    for rd in data.get("Relics", []):
        relic = RelicData()
        relic.id = rd["Id"]
        relic.name = rd.get("Name", "")
        relic.description = rd.get("Description", "")
        relic.star_rating = rd.get("StarRating", 1)
        relic.is_boss_relic = rd.get("IsBossRelic", False)
        relic.effects = [
            effect_pool.effects_by_id[eid]
            for eid in rd.get("EffectIds", [])
            if eid in effect_pool.effects_by_id
        ]
        relics.append(relic)
    relic_pool.initialize(relics)

    # ── Event Pool ───────────────────────────────────────────────────────────
    event_pool = EventPool()
    for ev in data.get("Events", []):
        choices = []
        for ch in ev.get("Choices", []):
            effects = []
            for eff in ch.get("Effects", []):
                ee = EventEffect(
                    type=EventEffectType(eff["Type"]),
                    value=eff.get("Value", 0),
                    parameter=eff.get("Parameter")
                )
                effects.append(ee)
            choices.append(EventChoice(choice_text=ch.get("ChoiceText", ""), effects=effects))
        ecs = EventChoiceSet(
            id=ev["Id"],
            event_title=ev.get("EventTitle", ""),
            event_description=ev.get("EventDescription", ""),
            choices=choices
        )
        event_pool.events_by_id[ecs.id] = ecs

    # ── Room Configs ─────────────────────────────────────────────────────────
    room_configs: Dict[RoomType, RoomData] = {}
    for key, rc in data.get("RoomConfigs", {}).items():
        rd = RoomData()
        rd.type = RoomType(rc["Type"])
        rd.display_name = rc.get("DisplayName", key)
        rd.description = rc.get("Description", "")
        rd.star_rating = rc.get("StarRating", 0)
        room_configs[rd.type] = rd

    # ── Hero Data ────────────────────────────────────────────────────────────
    hd = data.get("Hero", {})
    hero_data = HeroData()
    hero_data.id = hd.get("Id", "player")
    hero_data.name = hd.get("Name", "The Player")
    hero_data.starting_health = hd.get("StartingHealth", 60)
    hero_data.starting_strength = hd.get("StartingStrength", 0)
    hero_data.starting_gold = hd.get("StartingGold", 100)
    hero_data.starting_mana = hd.get("StartingMana", 3)
    hero_data.starting_hand_size = hd.get("StartingHandSize", 5)
    hero_data.starting_deck_card_ids = hd.get("StartingDeckCardIds", [])
    hero_data.starting_relic_id = hd.get("StartingRelicId", "")

    return card_pool, relic_pool, enemy_pool, effect_pool, event_pool, room_configs, hero_data
