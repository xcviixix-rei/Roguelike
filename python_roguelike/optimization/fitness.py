"""
fitness.py  –  Python port of MultiObjectiveFitness.cs + MultiObjectiveEvaluator.

Three independent objectives (pure Pareto, no weighted sum):
  1. Balance  – targets win rate ~45%, victory HP ~30%, avg floor on death ~10
  2. Engagement – card diversity + build variety
  3. Coherence – no trap cards + consistent win rate
"""

import math
from dataclasses import dataclass, field
from typing import List, Tuple

from .simulation_stats import SimulationStats


STARTING_CARDS = {"strike", "defend", "quick_jab", "cycle"}


@dataclass
class FitnessResult:
    """Multi-objective fitness for a single evaluated genome."""
    balance_score: float = 0.0
    engagement_score: float = 0.0
    coherence_score: float = 0.0

    win_rate: float = 0.0
    victory_hp: float = 0.0
    avg_floor_on_death: float = 0.0
    viable_cards: int = 0
    trap_cards: int = 0
    build_variety: float = 0.0
    unique_cards_acquired: int = 0

    is_feasible: bool = True
    constraint_violations: int = 0


class MultiObjectiveEvaluator:
    """Calculates the 3 fitness objectives from simulation results."""

    def __init__(
        self,
        target_win_rate: float = 0.45,
        target_victory_hp: float = 0.30,
        target_avg_floor_on_death: float = 10.0,
        min_acceptable_win_rate: float = 0.25,
        max_acceptable_win_rate: float = 0.65,
        min_viable_cards: int = 2,
        max_trap_cards: int = 2,
    ):
        self.target_win_rate = target_win_rate
        self.target_victory_hp = target_victory_hp
        self.target_avg_floor_on_death = target_avg_floor_on_death
        self.min_acceptable_win_rate = min_acceptable_win_rate
        self.max_acceptable_win_rate = max_acceptable_win_rate
        self.min_viable_cards = min_viable_cards
        self.max_trap_cards = max_trap_cards

    def evaluate(self, results: List[SimulationStats]) -> FitnessResult:
        f = FitnessResult()
        if not results:
            f.is_feasible = False
            f.constraint_violations = 999
            return f

        n = len(results)
        wins = [r for r in results if r.is_victory]
        losses = [r for r in results if not r.is_victory]

        f.win_rate = len(wins) / n
        f.victory_hp = (sum(r.final_hp_percent for r in wins) / len(wins)) if wins else 0.0
        f.avg_floor_on_death = (
            sum(r.final_floor_reached for r in losses) / len(losses)
        ) if losses else 15.0

        viable, traps, variety = self._analyze_card_diversity(results)
        f.viable_cards = viable
        f.trap_cards = traps
        f.build_variety = variety

        all_acquired = set()
        for r in results:
            all_acquired.update(cid for cid in r.master_deck_ids if cid not in STARTING_CARDS)
        f.unique_cards_acquired = len(all_acquired)

        if f.win_rate < self.min_acceptable_win_rate or f.win_rate > self.max_acceptable_win_rate:
            f.is_feasible = False
            f.constraint_violations += 1
        if viable < self.min_viable_cards:
            f.is_feasible = False
            f.constraint_violations += 1
        if traps > self.max_trap_cards:
            f.is_feasible = False
            f.constraint_violations += 1

        f.balance_score = self._balance(f)
        f.engagement_score = self._engagement(f)
        f.coherence_score = self._coherence(f)
        return f


    def _balance(self, f: FitnessResult) -> float:
        WIN_RATE_SENS = 10.0
        VICTORY_HP_SENS = 8.0
        FLOOR_DEATH_SENS = 0.05

        wr_score = math.exp(-WIN_RATE_SENS * (f.win_rate - self.target_win_rate) ** 2)
        hp_score = math.exp(-VICTORY_HP_SENS * (f.victory_hp - self.target_victory_hp) ** 2)
        floor_score = math.exp(
            -FLOOR_DEATH_SENS * (f.avg_floor_on_death - self.target_avg_floor_on_death) ** 2
        )
        return (wr_score + hp_score + floor_score) / 3.0


    def _engagement(self, f: FitnessResult) -> float:
        diversity = min(1.0, f.viable_cards / 8.0)
        return (diversity + f.build_variety) / 2.0


    def _coherence(self, f: FitnessResult) -> float:
        trap_penalty = max(0.0, 1.0 - f.trap_cards * 0.2)
        wr_consistency = 1.0 if 0.35 <= f.win_rate <= 0.55 else 0.5
        return (trap_penalty + wr_consistency) / 2.0


    def _analyze_card_diversity(
        self, results: List[SimulationStats]
    ) -> Tuple[int, int, float]:
        n = len(results)
        pick_counts: dict[str, int] = {}
        pick_wins: dict[str, int] = {}

        for run in results:
            picked = set(cid for cid in run.master_deck_ids if cid not in STARTING_CARDS)
            for cid in picked:
                pick_counts[cid] = pick_counts.get(cid, 0) + 1
                if cid not in pick_wins:
                    pick_wins[cid] = 0
                if run.is_victory:
                    pick_wins[cid] += 1

        viable = sum(1 for cnt in pick_counts.values() if cnt / n >= 0.10)

        MIN_PICKS = 10
        traps = 0
        for cid, cnt in pick_counts.items():
            if cnt < MIN_PICKS:
                continue
            pick_rate = cnt / n
            card_wr = pick_wins[cid] / cnt
            if pick_rate > 0.10 and card_wr < 0.30:
                traps += 1

        variety = self._build_variety(results)
        return viable, traps, variety

    def _build_variety(self, results: List[SimulationStats]) -> float:
        winning = [r for r in results if r.is_victory]
        if len(winning) < 2:
            return 0.0

        total_sim = 0.0
        comparisons = 0
        max_comp = min(50, len(winning) * (len(winning) - 1) // 2)

        for i in range(len(winning) - 1):
            if comparisons >= max_comp:
                break
            for j in range(i + 1, len(winning)):
                if comparisons >= max_comp:
                    break
                d1 = set(c for c in winning[i].master_deck_ids if c not in STARTING_CARDS)
                d2 = set(c for c in winning[j].master_deck_ids if c not in STARTING_CARDS)
                if not d1 or not d2:
                    continue
                intersection = len(d1 & d2)
                union = len(d1 | d2)
                total_sim += intersection / union
                comparisons += 1

        if comparisons == 0:
            return 0.5
        return 1.0 - total_sim / comparisons
