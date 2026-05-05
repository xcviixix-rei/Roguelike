"""
simulation_runner.py  –  Runs game simulations with PPO agents.

Port of HierarchicalSimulationRunner.cs, adapted to use PyTorch PPO models
instead of HeuristicPlayerAI.
"""

import copy
import os
from typing import Dict, List, Optional

import numpy as np

from .hierarchical_genome import HierarchicalGenome
from .simulation_stats import SimulationStats
from . import hierarchical_applicator

from ..data_loader import load_game_data
from ..data.enums import GameState, CombatState, RoomType
from ..core.game_controller import GameController
from ..env.roguelike_env import (
    RoguelikeEnv, OBS_DIM, ACT_DIM,
    OFF_END_TURN, OFF_PLAY, OFF_MAP, OFF_EVENT,
    OFF_REWARD, OFF_SHOP_CARD, OFF_SHOP_RELIC, OFF_SHOP_LEAVE,
    N_PLAY_ACTIONS, N_MAP_ACTIONS, N_EVENT_ACTIONS, N_REWARD_ACTIONS,
    N_SHOP_CARD, N_SHOP_RELIC,
    MAX_HAND, MAX_ENEMIES, MAX_NODES,
)


class PPOAgent:
    """
    Wraps a trained stable-baselines3 PPO model for inference.
    Supports both MaskablePPO (sb3_contrib) and regular PPO.
    """

    def __init__(self, model_path: str, use_masking: bool = True):
        self.use_masking = use_masking
        self._model = None
        self._model_path = model_path

    def load(self):
        """Lazy-load the model. Call once before running simulations."""
        if self._model is not None:
            return

        if self.use_masking:
            try:
                from sb3_contrib import MaskablePPO
                self._model = MaskablePPO.load(self._model_path)
                return
            except ImportError:
                pass

        from stable_baselines3 import PPO
        self._model = PPO.load(self._model_path)
        self.use_masking = False

    def predict(self, obs: np.ndarray, action_mask: Optional[np.ndarray] = None) -> int:
        """Pick an action given observation (and optionally an action mask)."""
        if self._model is None:
            raise RuntimeError("Model not loaded. Call agent.load() first.")

        if self.use_masking and action_mask is not None:
            action, _ = self._model.predict(obs, action_masks=action_mask, deterministic=True)
        else:
            action, _ = self._model.predict(obs, deterministic=True)

        return int(action)


class SimulationRunner:
    """
    Runs full game simulations with genome-modified data and PPO agent(s).

    Mirrors HierarchicalSimulationRunner.cs but uses the Python game engine
    and PPO models instead of C# HeuristicPlayerAI.
    """

    def __init__(
        self,
        agents: List[PPOAgent],
        json_path: Optional[str] = None,
        max_steps_per_game: int = 5_000,
    ):
        self.agents = agents
        self.max_steps = max_steps_per_game

        if json_path is None:
            json_path = os.path.join(
                os.path.dirname(os.path.dirname(__file__)), "GameData.json"
            )
        self._json_path = json_path

        (
            self._base_card_pool,
            self._base_relic_pool,
            self._base_enemy_pool,
            self._base_effect_pool,
            self._base_event_pool,
            self._base_room_configs,
            self._base_hero_data,
        ) = load_game_data(json_path)

    def run_batch(
        self,
        genome: HierarchicalGenome,
        n_runs: int,
        base_seed: int = 0,
    ) -> List[SimulationStats]:
        """
        Run ``n_runs`` simulations with each agent in ``self.agents``,
        returning all SimulationStats combined.
        """
        all_stats: List[SimulationStats] = []
        for agent in self.agents:
            agent.load()
            for i in range(n_runs):
                seed = base_seed + i
                stats = self._run_single(genome, agent, seed)
                all_stats.append(stats)
        return all_stats

    def _run_single(
        self,
        genome: HierarchicalGenome,
        agent: PPOAgent,
        seed: int,
    ) -> SimulationStats:
        """Run one complete game and collect telemetry."""
        card_pool = copy.deepcopy(self._base_card_pool)
        relic_pool = copy.deepcopy(self._base_relic_pool)
        enemy_pool = copy.deepcopy(self._base_enemy_pool)
        effect_pool = copy.deepcopy(self._base_effect_pool)
        event_pool = copy.deepcopy(self._base_event_pool)
        room_configs = copy.deepcopy(self._base_room_configs)
        hero_data = copy.deepcopy(self._base_hero_data)

        hierarchical_applicator.apply(
            genome, card_pool, enemy_pool, relic_pool, hero_data
        )

        env = RoguelikeEnv(seed=seed, max_steps=self.max_steps)
        env._card_pool = card_pool
        env._relic_pool = relic_pool
        env._enemy_pool = enemy_pool
        env._effect_pool = effect_pool
        env._event_pool = event_pool
        env._room_configs = room_configs
        env._hero_data = hero_data

        obs, info = env.reset()
        env._controller.current_run.rest_heal_pct = 0.30 * genome.rest_healing_scalar
        stats = SimulationStats()

        done = False
        step_count = 0
        while not done and step_count < self.max_steps:
            mask = env.action_masks()
            action = agent.predict(obs, action_mask=mask)

            if not mask[action]:
                valid = np.where(mask)[0]
                action = int(valid[0]) if len(valid) > 0 else 0

            run = env._controller.current_run
            if run.current_state == GameState.InCombat:
                if OFF_PLAY <= action < OFF_PLAY + N_PLAY_ACTIONS:
                    local = action - OFF_PLAY
                    hand_idx = local // MAX_ENEMIES
                    hero = run.the_hero
                    if hand_idx < len(hero.deck.hand):
                        card = hero.deck.hand[hand_idx]
                        stats.card_play_counts[card.id] = (
                            stats.card_play_counts.get(card.id, 0) + 1
                        )

            prev_state = run.current_state
            obs, reward, terminated, truncated, info = env.step(action)
            done = terminated or truncated
            step_count += 1

            run = env._controller.current_run
            if (prev_state == GameState.OnMap
                    and run.current_state == GameState.InCombat):
                current_room = run.the_map.get_current_room()
                if current_room and current_room.type == RoomType.Elite:
                    stats.elites_encountered += 1

        run = env._controller.current_run
        hero = run.the_hero
        stats.is_victory = hero.current_health > 0 and run.current_state == GameState.GameOver
        stats.final_floor_reached = run.current_floor
        stats.final_hp_percent = hero.current_health / max(hero.max_health, 1)
        stats.master_deck_ids = [c.id for c in hero.deck.master_deck]
        stats.relic_ids = [r.id for r in hero.relics]

        env.close()
        return stats
