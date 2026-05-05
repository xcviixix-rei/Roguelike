"""
hierarchical_genome.py  –  Python port of HierarchicalGenome.cs

A hierarchical genome that reduces the parameter space from ~500 to ~50
parameters by using multiplicative layers and only overriding specific outliers.
"""

import copy
import math
import random
from dataclasses import dataclass, field
from enum import Enum
from typing import Dict


class ScalingType(Enum):
    Damage = "Damage"
    Health = "Health"
    Block = "Block"


CARD_TYPE_ATTACK = "Attack"
CARD_TYPE_SKILL = "Skill"
CARD_TYPE_POWER = "Power"


@dataclass
class HierarchicalGenome:
    global_damage_multiplier: float = 1.0
    global_health_multiplier: float = 1.0
    global_block_multiplier: float = 1.0
    global_mana_cost_multiplier: float = 1.0
    global_gold_multiplier: float = 1.0

    early_game_damage_scaling: float = 1.0
    mid_game_damage_scaling: float = 1.0
    late_game_damage_scaling: float = 1.0

    early_game_health_scaling: float = 1.0
    mid_game_health_scaling: float = 1.0
    late_game_health_scaling: float = 1.0

    early_game_block_scaling: float = 1.0
    mid_game_block_scaling: float = 1.0
    late_game_block_scaling: float = 1.0

    card_type_scalars: Dict[str, float] = field(default_factory=lambda: {
        CARD_TYPE_ATTACK: 1.0,
        CARD_TYPE_SKILL: 1.0,
        CARD_TYPE_POWER: 1.0,
    })

    card_star_scalars: Dict[int, float] = field(default_factory=lambda: {
        1: 1.0, 2: 1.0, 3: 1.0, 4: 1.0, 5: 1.0,
    })

    enemy_star_scalars: Dict[int, float] = field(default_factory=lambda: {
        1: 1.0, 2: 1.0, 3: 1.0, 4: 1.0, 5: 1.0,
    })

    room_type_weights: Dict[str, float] = field(default_factory=lambda: {
        "Monster": 45.0,
        "Elite": 12.0,
        "Event": 22.0,
        "Shop": 8.0,
        "Rest": 13.0,
    })

    monster_star_ratio: float = 0.5
    elite_star_ratio: float = 0.5
    rest_healing_scalar: float = 1.0

    hero_health_scalar: float = 1.0
    hero_start_gold_scalar: float = 1.0
    hero_mana_offset: int = 0

    difficulty_progression_rate: float = 0.03

    card_damage_overrides: Dict[str, float] = field(default_factory=dict)
    card_mana_cost_overrides: Dict[str, float] = field(default_factory=dict)
    enemy_health_overrides: Dict[str, float] = field(default_factory=dict)

    def randomize(self, rng: random.Random) -> None:
        self.global_damage_multiplier = 0.7 + rng.random() * 0.6
        self.global_health_multiplier = 0.7 + rng.random() * 0.6
        self.global_block_multiplier = 0.7 + rng.random() * 0.6
        self.global_mana_cost_multiplier = 0.85 + rng.random() * 0.3
        self.global_gold_multiplier = 0.7 + rng.random() * 0.6

        self.early_game_damage_scaling = 0.8 + rng.random() * 0.3
        self.mid_game_damage_scaling = 1.0 + rng.random() * 0.3
        self.late_game_damage_scaling = 1.2 + rng.random() * 0.3

        self.early_game_health_scaling = 0.8 + rng.random() * 0.3
        self.mid_game_health_scaling = 1.0 + rng.random() * 0.3
        self.late_game_health_scaling = 1.2 + rng.random() * 0.3

        self.early_game_block_scaling = 0.9 + rng.random() * 0.2
        self.mid_game_block_scaling = 1.0 + rng.random() * 0.2
        self.late_game_block_scaling = 1.0 + rng.random() * 0.2

        for key in list(self.card_type_scalars):
            self.card_type_scalars[key] = 0.85 + rng.random() * 0.3

        for key in list(self.card_star_scalars):
            self.card_star_scalars[key] = 0.9 + rng.random() * 0.2

        for key in list(self.enemy_star_scalars):
            self.enemy_star_scalars[key] = 0.9 + rng.random() * 0.2

        self.room_type_weights["Monster"] = 30 + rng.random() * 30
        self.room_type_weights["Elite"] = 5 + rng.random() * 20
        self.room_type_weights["Event"] = 10 + rng.random() * 25
        self.room_type_weights["Shop"] = 5 + rng.random() * 15
        self.room_type_weights["Rest"] = 5 + rng.random() * 20

        self.monster_star_ratio = 0.3 + rng.random() * 0.4
        self.elite_star_ratio = 0.3 + rng.random() * 0.4
        self.rest_healing_scalar = 0.6 + rng.random() * 0.8

        self.hero_health_scalar = 0.7 + rng.random() * 0.6
        self.hero_start_gold_scalar = 0.8 + rng.random() * 0.4
        self.difficulty_progression_rate = rng.random() * 0.06

        mana_roll = rng.randint(0, 99)
        if mana_roll < 5:
            self.hero_mana_offset = -1
        elif mana_roll > 95:
            self.hero_mana_offset = 1
        else:
            self.hero_mana_offset = 0

    def clone(self) -> "HierarchicalGenome":
        return copy.deepcopy(self)


    def get_progression_scalar(self, floor: int, scaling_type: ScalingType) -> float:
        if scaling_type == ScalingType.Damage:
            early, mid, late = (self.early_game_damage_scaling,
                                self.mid_game_damage_scaling,
                                self.late_game_damage_scaling)
        elif scaling_type == ScalingType.Health:
            early, mid, late = (self.early_game_health_scaling,
                                self.mid_game_health_scaling,
                                self.late_game_health_scaling)
        elif scaling_type == ScalingType.Block:
            early, mid, late = (self.early_game_block_scaling,
                                self.mid_game_block_scaling,
                                self.late_game_block_scaling)
        else:
            return 1.0

        if floor <= 5:
            return early
        if floor <= 10:
            return mid
        return late

    def get_floor_difficulty_multiplier(self, floor: int) -> float:
        t = (floor - 1) / 14.0
        s_curve = 1.0 / (1.0 + math.exp(-10 * (t - 0.5)))
        return 1.0 + self.difficulty_progression_rate * 5.0 * s_curve


    PARAM_SPECS = [
        ("global_damage_multiplier", 0.7, 1.3),
        ("global_health_multiplier", 0.7, 1.3),
        ("global_block_multiplier", 0.7, 1.3),
        ("global_mana_cost_multiplier", 0.85, 1.15),
        ("global_gold_multiplier", 0.7, 1.3),
        ("early_game_damage_scaling", 0.7, 1.2),
        ("mid_game_damage_scaling", 0.9, 1.4),
        ("late_game_damage_scaling", 1.1, 1.6),
        ("early_game_health_scaling", 0.7, 1.2),
        ("mid_game_health_scaling", 0.9, 1.4),
        ("late_game_health_scaling", 1.1, 1.6),
        ("early_game_block_scaling", 0.8, 1.1),
        ("mid_game_block_scaling", 0.9, 1.2),
        ("late_game_block_scaling", 0.9, 1.2),
        ("card_type_Attack", 0.85, 1.15),
        ("card_type_Skill", 0.85, 1.15),
        ("card_type_Power", 0.85, 1.15),
        ("card_star_1", 0.9, 1.1),
        ("card_star_2", 0.9, 1.1),
        ("card_star_3", 0.9, 1.1),
        ("card_star_4", 0.9, 1.1),
        ("card_star_5", 0.9, 1.1),
        ("enemy_star_1", 0.9, 1.1),
        ("enemy_star_2", 0.9, 1.1),
        ("enemy_star_3", 0.9, 1.1),
        ("enemy_star_4", 0.9, 1.1),
        ("enemy_star_5", 0.9, 1.1),
        ("room_Monster", 30.0, 60.0),
        ("room_Elite", 5.0, 25.0),
        ("room_Event", 10.0, 35.0),
        ("room_Shop", 5.0, 20.0),
        ("room_Rest", 5.0, 25.0),
        ("monster_star_ratio", 0.2, 0.8),
        ("elite_star_ratio", 0.2, 0.8),
        ("rest_healing_scalar", 0.5, 1.5),
        ("hero_health_scalar", 0.7, 1.3),
        ("hero_start_gold_scalar", 0.8, 1.2),
        ("difficulty_progression_rate", 0.0, 0.06),
    ]

    N_CONTINUOUS = len(PARAM_SPECS)

    @classmethod
    def get_bounds(cls):
        """Returns (xl, xu) arrays for pymoo."""
        import numpy as np
        xl = np.array([s[1] for s in cls.PARAM_SPECS], dtype=np.float64)
        xu = np.array([s[2] for s in cls.PARAM_SPECS], dtype=np.float64)
        return xl, xu

    def to_vector(self):
        """Flatten genome into a numpy array for pymoo."""
        import numpy as np
        x = np.zeros(self.N_CONTINUOUS, dtype=np.float64)
        i = 0
        x[i] = self.global_damage_multiplier; i += 1
        x[i] = self.global_health_multiplier; i += 1
        x[i] = self.global_block_multiplier; i += 1
        x[i] = self.global_mana_cost_multiplier; i += 1
        x[i] = self.global_gold_multiplier; i += 1
        x[i] = self.early_game_damage_scaling; i += 1
        x[i] = self.mid_game_damage_scaling; i += 1
        x[i] = self.late_game_damage_scaling; i += 1
        x[i] = self.early_game_health_scaling; i += 1
        x[i] = self.mid_game_health_scaling; i += 1
        x[i] = self.late_game_health_scaling; i += 1
        x[i] = self.early_game_block_scaling; i += 1
        x[i] = self.mid_game_block_scaling; i += 1
        x[i] = self.late_game_block_scaling; i += 1
        x[i] = self.card_type_scalars[CARD_TYPE_ATTACK]; i += 1
        x[i] = self.card_type_scalars[CARD_TYPE_SKILL]; i += 1
        x[i] = self.card_type_scalars[CARD_TYPE_POWER]; i += 1
        for s in range(1, 6):
            x[i] = self.card_star_scalars[s]; i += 1
        for s in range(1, 6):
            x[i] = self.enemy_star_scalars[s]; i += 1
        for rt in ["Monster", "Elite", "Event", "Shop", "Rest"]:
            x[i] = self.room_type_weights[rt]; i += 1
        x[i] = self.monster_star_ratio; i += 1
        x[i] = self.elite_star_ratio; i += 1
        x[i] = self.rest_healing_scalar; i += 1
        x[i] = self.hero_health_scalar; i += 1
        x[i] = self.hero_start_gold_scalar; i += 1
        x[i] = self.difficulty_progression_rate; i += 1
        return x

    @classmethod
    def from_vector(cls, x) -> "HierarchicalGenome":
        """Reconstruct genome from a numpy vector (pymoo decision variable)."""
        g = cls()
        i = 0
        g.global_damage_multiplier = float(x[i]); i += 1
        g.global_health_multiplier = float(x[i]); i += 1
        g.global_block_multiplier = float(x[i]); i += 1
        g.global_mana_cost_multiplier = float(x[i]); i += 1
        g.global_gold_multiplier = float(x[i]); i += 1

        g.early_game_damage_scaling = float(x[i]); i += 1
        g.mid_game_damage_scaling = float(x[i]); i += 1
        g.late_game_damage_scaling = float(x[i]); i += 1
        g.early_game_health_scaling = float(x[i]); i += 1
        g.mid_game_health_scaling = float(x[i]); i += 1
        g.late_game_health_scaling = float(x[i]); i += 1
        g.early_game_block_scaling = float(x[i]); i += 1
        g.mid_game_block_scaling = float(x[i]); i += 1
        g.late_game_block_scaling = float(x[i]); i += 1

        g.card_type_scalars[CARD_TYPE_ATTACK] = float(x[i]); i += 1
        g.card_type_scalars[CARD_TYPE_SKILL] = float(x[i]); i += 1
        g.card_type_scalars[CARD_TYPE_POWER] = float(x[i]); i += 1

        for s in range(1, 6):
            g.card_star_scalars[s] = float(x[i]); i += 1
        for s in range(1, 6):
            g.enemy_star_scalars[s] = float(x[i]); i += 1

        for rt in ["Monster", "Elite", "Event", "Shop", "Rest"]:
            g.room_type_weights[rt] = float(x[i]); i += 1

        g.monster_star_ratio = float(x[i]); i += 1
        g.elite_star_ratio = float(x[i]); i += 1
        g.rest_healing_scalar = float(x[i]); i += 1

        g.hero_health_scalar = float(x[i]); i += 1
        g.hero_start_gold_scalar = float(x[i]); i += 1
        g.difficulty_progression_rate = float(x[i]); i += 1
        return g
