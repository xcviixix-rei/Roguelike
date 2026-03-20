"""
roguelike_env.py  –  Gymnasium wrapper for the Python Roguelike core.

Observation space  : flat numpy array encoding the full game state
Action space       : Discrete — see _decode_action() for the full mapping
Reward             : shaped per step + large terminal rewards

Action encoding (in order):
  0                    → end_turn
  1 .. H*E             → play card h targeting enemy e  (H=hand size cap, E=enemy cap)
  H*E+1 .. H*E+N_NODES → choose map node (index in possible list)
  H*E+N_NODES+1..+3    → choose event option (0..2)
  H*E+N_NODES+4..+8    → confirm reward picking card 0..3, or -1 (index 4 = skip)
  H*E+N_NODES+9..+14   → buy shop card slot 0..4
  H*E+N_NODES+15..+20  → buy shop relic slot 0..4
  H*E+N_NODES+21       → leave shop
"""

import os
import math
import numpy as np

try:
    import gymnasium as gym
    from gymnasium import spaces
except ImportError:
    raise ImportError("gymnasium is required. Install with:  pip install gymnasium")

import sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from ..data_loader import load_game_data
from ..core.game_controller import GameController
from ..core.game_run import GameRun
from ..data.enums import GameState, CombatState, RoomType, ActionType


# ── Observation / Action constants ──────────────────────────────────────────
MAX_HAND        = 10   # max cards in hand we encode
MAX_DECK        = 40   # max cards in master deck we encode
MAX_ENEMIES     = 4    # max enemies in a combat
MAX_RELICS      = 10   # max relics we encode
N_CARD_FEATURES = 5    # (mana_cost, star, type_id, n_actions, is_affordable)
N_ENEMY_FEATURES= 5    # (hp_pct, block_pct, star, n_effects, next_action_type)
N_EFFECTS       = 7    # number of distinct StatusEffectType values

# Node pool: map nodes the player can move to (capped)
MAX_NODES       = 7    # max 7 paths at a time

# Total observation dimension
OBS_DIM = (
    4                                   # hero: hp_pct, block_pct, mana_pct, gold_norm
    + N_EFFECTS                         # hero active status effects (binary presence)
    + MAX_HAND  * N_CARD_FEATURES       # hand
    + MAX_DECK                          # deck star histogram (just star ratings)
    + MAX_ENEMIES * N_ENEMY_FEATURES    # enemies
    + MAX_RELICS                        # relic star ratings
    + 7                                 # game state one-hot (7 states)
    + MAX_NODES * 2                     # map nodes (type_id, star) for each node
    + 1                                 # current floor normalised
)

# Action space size
N_PLAY_ACTIONS  = MAX_HAND * MAX_ENEMIES
N_MAP_ACTIONS   = MAX_NODES
N_EVENT_ACTIONS = 3
N_REWARD_ACTIONS= 5   # pick card 0-3 or skip (-1)
N_SHOP_CARD     = 5
N_SHOP_RELIC    = 5
N_SHOP_LEAVE    = 1
N_END_TURN      = 1

ACT_DIM = (
    N_END_TURN
    + N_PLAY_ACTIONS
    + N_MAP_ACTIONS
    + N_EVENT_ACTIONS
    + N_REWARD_ACTIONS
    + N_SHOP_CARD
    + N_SHOP_RELIC
    + N_SHOP_LEAVE
)

# Offsets
OFF_END_TURN    = 0
OFF_PLAY        = OFF_END_TURN   + N_END_TURN
OFF_MAP         = OFF_PLAY       + N_PLAY_ACTIONS
OFF_EVENT       = OFF_MAP        + N_MAP_ACTIONS
OFF_REWARD      = OFF_EVENT      + N_EVENT_ACTIONS
OFF_SHOP_CARD   = OFF_REWARD     + N_REWARD_ACTIONS
OFF_SHOP_RELIC  = OFF_SHOP_CARD  + N_SHOP_CARD
OFF_SHOP_LEAVE  = OFF_SHOP_RELIC + N_SHOP_RELIC

from ..data.enums import StatusEffectType, CardType

_EFFECT_ORDER = list(StatusEffectType)
_CARD_TYPE_ORDER = list(CardType)
_ACTION_TYPE_ORDER = list(ActionType)

_ROOM_TYPE_IDS = {rt: i for i, rt in enumerate(RoomType)}
_ACTION_TYPE_IDS = {at: i for i, at in enumerate(ActionType)}


class RoguelikeEnv(gym.Env):
    """
    Single-player Roguelike Gymnasium environment.

    Parameters
    ----------
    seed : int
        Master seed.  Each reset() call increments an internal counter
        so every episode gets a reproducible but distinct map.
    json_path : str | None
        Path to GameData.json.  Defaults to the bundled copy.
    max_steps : int
        Hard limit per episode (prevents infinite loops).
    render_mode : str | None
        Currently only 'ansi' is supported.
    """

    metadata = {"render_modes": ["ansi"]}

    def __init__(
        self,
        seed: int = 42,
        json_path: str | None = None,
        max_steps: int = 10_000,
        render_mode: str | None = None,
    ):
        super().__init__()
        self._base_seed = seed
        self._episode_count = 0
        self._max_steps = max_steps
        self._step_count = 0
        self.render_mode = render_mode

        if json_path is None:
            json_path = os.path.join(os.path.dirname(os.path.dirname(__file__)), "GameData.json")

        (
            self._card_pool,
            self._relic_pool,
            self._enemy_pool,
            self._effect_pool,
            self._event_pool,
            self._room_configs,
            self._hero_data,
        ) = load_game_data(json_path)

        self._controller: GameController | None = None
        self._prev_hp = 0
        self._prev_floor = 0

        self.observation_space = spaces.Box(
            low=0.0, high=1.0, shape=(OBS_DIM,), dtype=np.float32
        )
        self.action_space = spaces.Discrete(ACT_DIM)

    # ── Public API ─────────────────────────────────────────────────────────

    def reset(self, *, seed=None, options=None):
        super().reset(seed=seed)
        if seed is not None:
            self._base_seed = seed

        run_seed = self._base_seed + self._episode_count
        self._episode_count += 1
        self._step_count = 0

        self._controller = GameController(
            self._card_pool, self._relic_pool, self._enemy_pool,
            self._effect_pool, self._event_pool, self._room_configs
        )
        self._controller.start_new_run(run_seed, self._hero_data)

        run = self._controller.current_run
        self._prev_hp = run.the_hero.current_health
        self._prev_floor = run.current_floor

        obs = self._get_obs()
        info = self._get_info()
        return obs, info

    def step(self, action: int):
        run = self._controller.current_run
        self._step_count += 1

        prev_hp = run.the_hero.current_health
        prev_gold = run.the_hero.current_gold
        prev_floor = run.current_floor

        self._apply_action(action, run)

        run = self._controller.current_run  # re-fetch (same object but re-query)
        reward = self._compute_reward(run, prev_hp, prev_gold, prev_floor)

        terminated = run.current_state == GameState.GameOver
        truncated = self._step_count >= self._max_steps

        if terminated:
            # Large terminal reward based on floors cleared
            floors_cleared = run.current_floor + 1
            hp_remaining_pct = run.the_hero.current_health / run.the_hero.max_health
            if run.the_hero.current_health > 0:
                reward += 200.0 + floors_cleared * 10.0 + hp_remaining_pct * 50.0
            else:
                reward -= 50.0

        obs = self._get_obs()
        info = self._get_info()
        return obs, float(reward), terminated, truncated, info

    def render(self):
        if self.render_mode != "ansi":
            return
        run = self._controller.current_run
        hero = run.the_hero
        lines = [
            f"═══ ROGUELIKE ═══  State={run.current_state.value}  Floor={run.current_floor}",
            f"HP {hero.current_health}/{hero.max_health}  Block {hero.block}"
            f"  Mana {hero.current_mana}/{hero.max_mana}  Gold {hero.current_gold}",
        ]
        if run.current_state == GameState.InCombat and run.current_combat:
            combat = run.current_combat
            for i, e in enumerate(combat.enemies):
                if e.current_health > 0:
                    intent = combat.current_enemy_intents.get(e)
                    intent_str = f"{intent.type.value}({intent.value})" if intent else "?"
                    lines.append(f"  Enemy[{i}] {e.source_data.name}  HP {e.current_health}  Block {e.block}  Intent:{intent_str}")
            hand_str = ", ".join(f"{c.name}({c.mana_cost})" for c in hero.deck.hand)
            lines.append(f"  Hand: [{hand_str}]")
        print("\n".join(lines))

    def close(self):
        pass

    # ── Observation ────────────────────────────────────────────────────────

    def _get_obs(self) -> np.ndarray:
        run = self._controller.current_run
        hero = run.the_hero
        obs = np.zeros(OBS_DIM, dtype=np.float32)
        idx = 0

        # Hero scalars
        obs[idx] = hero.current_health / hero.max_health;   idx += 1
        obs[idx] = hero.block / max(hero.max_health, 1);    idx += 1
        obs[idx] = hero.current_mana / max(hero.max_mana, 1); idx += 1
        obs[idx] = min(hero.current_gold / 500.0, 1.0);     idx += 1

        # Hero active status effects
        from ..data.status_effect_data import StatusEffectData as SED
        for eff_type in _EFFECT_ORDER:
            present = any(
                isinstance(ae.source_data, SED) and ae.source_data.effect_type == eff_type
                for ae in hero.active_effects
            )
            obs[idx] = 1.0 if present else 0.0
            idx += 1

        # Hand
        for i in range(MAX_HAND):
            if i < len(hero.deck.hand):
                c = hero.deck.hand[i]
                obs[idx]   = c.mana_cost / 5.0
                obs[idx+1] = c.star_rating / 5.0
                ct_idx = _CARD_TYPE_ORDER.index(c.type) if c.type in _CARD_TYPE_ORDER else 0
                obs[idx+2] = ct_idx / max(len(_CARD_TYPE_ORDER) - 1, 1)
                obs[idx+3] = min(len(c.actions) / 3.0, 1.0)
                obs[idx+4] = 1.0 if c.mana_cost <= hero.current_mana else 0.0
            idx += N_CARD_FEATURES

        # Deck star histogram (normalised)
        for i in range(MAX_DECK):
            if i < len(hero.deck.master_deck):
                obs[idx] = hero.deck.master_deck[i].star_rating / 5.0
            idx += 1

        # Enemies
        combat = run.current_combat
        for i in range(MAX_ENEMIES):
            if combat and i < len(combat.enemies):
                e = combat.enemies[i]
                obs[idx]   = e.current_health / max(e.max_health, 1)
                obs[idx+1] = e.block / max(e.max_health, 1)
                obs[idx+2] = e.source_enemy_data.star_rating / 5.0
                obs[idx+3] = min(len(e.active_effects) / 5.0, 1.0)
                intent = combat.current_enemy_intents.get(e)
                obs[idx+4] = _ACTION_TYPE_ORDER.index(intent.type) / max(len(_ACTION_TYPE_ORDER) - 1, 1) if intent else 0.0
            idx += N_ENEMY_FEATURES

        # Relics
        for i in range(MAX_RELICS):
            if i < len(hero.relics):
                obs[idx] = hero.relics[i].star_rating / 5.0
            idx += 1

        # Game state one-hot
        state_order = list(GameState)
        s_idx = state_order.index(run.current_state)
        obs[idx + s_idx] = 1.0
        idx += len(state_order)

        # Possible map nodes
        nodes = run.the_map.get_possible_next_nodes()
        for i in range(MAX_NODES):
            if i < len(nodes):
                n = nodes[i]
                obs[idx]   = _ROOM_TYPE_IDS.get(n.type, 0) / max(len(_ROOM_TYPE_IDS) - 1, 1)
                obs[idx+1] = n.star_rating / 5.0
            idx += 2

        # Floor
        obs[idx] = max(run.current_floor, 0) / 15.0
        idx += 1

        assert idx == OBS_DIM, f"OBS_DIM mismatch: wrote {idx}, expected {OBS_DIM}"
        return obs

    # ── Action Dispatch ────────────────────────────────────────────────────

    def _apply_action(self, action: int, run: GameRun):
        state = run.current_state

        if action == OFF_END_TURN:
            if state == GameState.InCombat:
                self._controller.end_turn()
            return

        # Play card
        if OFF_PLAY <= action < OFF_PLAY + N_PLAY_ACTIONS:
            if state != GameState.InCombat:
                return
            local = action - OFF_PLAY
            hand_idx = local // MAX_ENEMIES
            target_idx = local % MAX_ENEMIES
            self._controller.play_card(hand_idx, target_idx)
            return

        # Map node
        if OFF_MAP <= action < OFF_MAP + N_MAP_ACTIONS:
            if state != GameState.OnMap:
                return
            node_slot = action - OFF_MAP
            nodes = run.the_map.get_possible_next_nodes()
            if node_slot < len(nodes):
                self._controller.choose_map_node(nodes[node_slot].id)
            return

        # Event
        if OFF_EVENT <= action < OFF_EVENT + N_EVENT_ACTIONS:
            if state != GameState.InEvent:
                return
            choice_idx = action - OFF_EVENT
            if run.current_event and choice_idx < len(run.current_event.choices):
                self._controller.choose_event_option(choice_idx)
            return

        # Reward
        if OFF_REWARD <= action < OFF_REWARD + N_REWARD_ACTIONS:
            if state != GameState.AwaitingReward:
                return
            local = action - OFF_REWARD
            card_idx = local - 1  # slot 0 = skip (-1), slots 1-4 = card index 0-3
            self._controller.confirm_rewards(card_idx)
            return

        # Shop card
        if OFF_SHOP_CARD <= action < OFF_SHOP_CARD + N_SHOP_CARD:
            if state != GameState.InShop:
                return
            self._controller.buy_shop_card(action - OFF_SHOP_CARD)
            return

        # Shop relic
        if OFF_SHOP_RELIC <= action < OFF_SHOP_RELIC + N_SHOP_RELIC:
            if state != GameState.InShop:
                return
            self._controller.buy_shop_relic(action - OFF_SHOP_RELIC)
            return

        # Leave shop
        if action == OFF_SHOP_LEAVE:
            if state == GameState.InShop:
                self._controller.leave_shop()
            return

    # ── Reward Shaping ─────────────────────────────────────────────────────

    def _compute_reward(
        self,
        run: GameRun,
        prev_hp: int,
        prev_gold: int,
        prev_floor: int,
    ) -> float:
        reward = 0.0
        hero = run.the_hero

        # HP change
        hp_delta = hero.current_health - prev_hp
        reward += hp_delta * 0.1

        # Gold gained
        gold_delta = hero.current_gold - prev_gold
        if gold_delta > 0:
            reward += gold_delta * 0.02

        # Floor progress
        floor_delta = run.current_floor - prev_floor
        if floor_delta > 0:
            reward += floor_delta * 5.0

        # Small cost per step to encourage speed
        reward -= 0.01

        return reward

    # ── Info ───────────────────────────────────────────────────────────────

    def _get_info(self) -> dict:
        run = self._controller.current_run
        hero = run.the_hero
        return {
            "game_state":   run.current_state.value,
            "floor":        run.current_floor,
            "hp":           hero.current_health,
            "max_hp":       hero.max_health,
            "gold":         hero.current_gold,
            "deck_size":    len(hero.deck.master_deck),
            "relic_count":  len(hero.relics),
            "step":         self._step_count,
        }

    # ── Action mask helper (optional, for masked PPO) ───────────────────────

    def action_masks(self) -> np.ndarray:
        """Returns a boolean mask of valid actions given the current state."""
        run = self._controller.current_run
        hero = run.the_hero
        state = run.current_state
        mask = np.zeros(ACT_DIM, dtype=bool)

        if state == GameState.InCombat:
            mask[OFF_END_TURN] = True
            combat = run.current_combat
            if combat and combat.state == CombatState.Ongoing_PlayerTurn:
                for h_i, card in enumerate(hero.deck.hand[:MAX_HAND]):
                    if card.mana_cost <= hero.current_mana:
                        for e_i in range(min(len(combat.enemies), MAX_ENEMIES)):
                            if combat.enemies[e_i].current_health > 0:
                                mask[OFF_PLAY + h_i * MAX_ENEMIES + e_i] = True

        elif state == GameState.OnMap:
            nodes = run.the_map.get_possible_next_nodes()
            for i in range(min(len(nodes), MAX_NODES)):
                mask[OFF_MAP + i] = True

        elif state == GameState.InEvent:
            if run.current_event:
                for i in range(min(len(run.current_event.choices), N_EVENT_ACTIONS)):
                    mask[OFF_EVENT + i] = True

        elif state == GameState.AwaitingReward:
            mask[OFF_REWARD] = True   # skip card
            for i in range(min(len(run.card_reward_choices), N_REWARD_ACTIONS - 1)):
                mask[OFF_REWARD + 1 + i] = True

        elif state == GameState.InShop:
            if run.current_shop:
                shop = run.current_shop
                for i, item in enumerate(shop.cards_for_sale[:N_SHOP_CARD]):
                    if not item.is_sold and hero.current_gold >= item.price:
                        mask[OFF_SHOP_CARD + i] = True
                for i, item in enumerate(shop.relics_for_sale[:N_SHOP_RELIC]):
                    if not item.is_sold and hero.current_gold >= item.price:
                        mask[OFF_SHOP_RELIC + i] = True
            mask[OFF_SHOP_LEAVE] = True

        return mask
