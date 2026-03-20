from .i_room_handler import IRoomHandler
from ..combat_manager import CombatManager
from ...data.enums import GameState


class BossRoomHandler(IRoomHandler):
    def execute(self, run, room):
        boss = run.enemy_pool.get_random_enemy_of_star(5, run.rng)
        if boss is None:
            return

        run.current_combat = CombatManager(
            run.the_hero,
            [boss],
            run.rng,
            run.effect_pool.get_effect
        )
        run.current_combat.start_combat()
        run.current_state = GameState.InCombat
