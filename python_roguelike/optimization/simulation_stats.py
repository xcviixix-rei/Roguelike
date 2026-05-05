"""
simulation_stats.py  –  Python port of SimulationStats.cs

Records raw telemetry from a single game simulation run.
"""

from dataclasses import dataclass, field
from typing import Dict, List


@dataclass
class SimulationStats:
    is_victory: bool = False
    final_floor_reached: int = 0
    final_hp_percent: float = 0.0

    master_deck_ids: List[str] = field(default_factory=list)
    relic_ids: List[str] = field(default_factory=list)

    elites_defeated: int = 0
    elites_encountered: int = 0
    total_damage_taken_at_elites: float = 0.0

    gold_collected: int = 0
    gold_spent: int = 0

    card_play_counts: Dict[str, int] = field(default_factory=dict)
