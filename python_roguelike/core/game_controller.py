from typing import Dict, Optional
from .game_run import GameRun
from .rooms.combat_room_handler import CombatRoomHandler
from .rooms.boss_room_handler import BossRoomHandler
from .rooms.event_room_handler import EventRoomHandler
from .rooms.rest_room_handler import RestRoomHandler
from .rooms.shop_room_handler import ShopRoomHandler
from .rooms.i_room_handler import IRoomHandler
from ..data.combatant.hero_data import HeroData
from ..data.room_data import RoomData
from ..data.pools.card_pool import CardPool
from ..data.pools.relic_pool import RelicPool
from ..data.pools.enemy_pool import EnemyPool
from ..data.pools.effect_pool import EffectPool
from ..data.pools.event_pool import EventPool
from ..data.enums import GameState, RoomType, CombatState


class GameController:
    def __init__(
        self,
        card_pool: CardPool,
        relic_pool: RelicPool,
        enemy_pool: EnemyPool,
        effect_pool: EffectPool,
        event_pool: EventPool,
        room_configs: Dict[RoomType, RoomData]
    ):
        self._card_pool = card_pool
        self._relic_pool = relic_pool
        self._enemy_pool = enemy_pool
        self._effect_pool = effect_pool
        self._event_pool = event_pool
        self._room_configs = room_configs

        self._handlers: Dict[RoomType, IRoomHandler] = {
            RoomType.Monster: CombatRoomHandler(),
            RoomType.Elite: CombatRoomHandler(),
            RoomType.Boss: BossRoomHandler(),
            RoomType.Event: EventRoomHandler(),
            RoomType.Shop: ShopRoomHandler(),
            RoomType.Rest: RestRoomHandler(),
        }

        self.current_run: Optional[GameRun] = None

    def start_new_run(self, seed: int, hero_data: HeroData):
        self.current_run = GameRun(
            seed, hero_data,
            self._card_pool, self._relic_pool, self._enemy_pool,
            self._effect_pool, self._event_pool, self._room_configs
        )

    # ─── MAP ACTIONS ────────────────────────────────────────────────────────

    def choose_map_node(self, node_id: int) -> bool:
        if self.current_run.current_state != GameState.OnMap:
            return False
        if not self.current_run.the_map.move_to_node(node_id):
            return False

        room = self.current_run.the_map.get_current_room()
        handler = self._handlers.get(room.type)
        if handler:
            handler.execute(self.current_run, room)
        return True

    # ─── COMBAT ACTIONS ─────────────────────────────────────────────────────

    def play_card(self, hand_index: int, target_enemy_index: int) -> bool:
        run = self.current_run
        if run.current_state != GameState.InCombat or run.current_combat is None:
            return False

        hero = run.the_hero
        if hand_index < 0 or hand_index >= len(hero.deck.hand):
            return False

        card = hero.deck.hand[hand_index]

        target = None
        if 0 <= target_enemy_index < len(run.current_combat.enemies):
            candidate = run.current_combat.enemies[target_enemy_index]
            if candidate.current_health > 0:
                target = candidate

        success = run.current_combat.play_card(card, target)
        if success:
            self._check_combat_result()
        return success

    def end_turn(self) -> bool:
        run = self.current_run
        if run.current_state != GameState.InCombat or run.current_combat is None:
            return False
        run.current_combat.end_player_turn()
        self._check_combat_result()
        return True

    def _check_combat_result(self):
        run = self.current_run
        if run.current_combat is None:
            return
        if run.current_combat.state == CombatState.Victory:
            CombatRoomHandler.generate_victory_rewards(run)
        elif run.current_combat.state == CombatState.Defeat:
            run.current_state = GameState.GameOver

    # ─── EVENT & SHOP ACTIONS ───────────────────────────────────────────────

    def choose_event_option(self, choice_index: int):
        if self.current_run.current_state != GameState.InEvent:
            return
        EventRoomHandler.resolve_choice(self.current_run, choice_index)

    def buy_shop_card(self, index: int) -> bool:
        return ShopRoomHandler.purchase_card(self.current_run, index)

    def buy_shop_relic(self, index: int) -> bool:
        return ShopRoomHandler.purchase_relic(self.current_run, index)

    def leave_shop(self):
        ShopRoomHandler.leave_shop(self.current_run)

    # ─── REWARD ACTIONS ─────────────────────────────────────────────────────

    def confirm_rewards(self, card_index: int):
        run = self.current_run
        if run.current_state != GameState.AwaitingReward:
            return

        if 0 <= card_index < len(run.card_reward_choices):
            chosen_card = run.card_reward_choices[card_index]
            run.the_hero.deck.add_card_to_master_deck(chosen_card)

        if run.relic_reward_choice is not None:
            run.the_hero.relics.append(run.relic_reward_choice)

        run.card_reward_choices.clear()
        run.relic_reward_choice = None

        room = run.the_map.get_current_room()
        if room is not None and room.type == RoomType.Boss:
            run.current_state = GameState.GameOver
        else:
            run.current_state = GameState.OnMap
