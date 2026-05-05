import math
from .i_room_handler import IRoomHandler
from ...data.enums import GameState


class RestRoomHandler(IRoomHandler):
    def execute(self, run, room):
        heal_pct = getattr(run, 'rest_heal_pct', 0.30)
        heal_amount = int(math.floor(run.the_hero.max_health * heal_pct))
        run.the_hero.heal(heal_amount)
        run.current_state = GameState.OnMap
