import math
from .i_room_handler import IRoomHandler
from ...data.enums import GameState


class RestRoomHandler(IRoomHandler):
    def execute(self, run, room):
        base_heal_percentage = 0.30
        heal_amount = int(math.floor(run.the_hero.max_health * base_heal_percentage))
        run.the_hero.heal(heal_amount)
        run.current_state = GameState.OnMap
