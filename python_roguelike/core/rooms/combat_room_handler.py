import math
from typing import List
from .i_room_handler import IRoomHandler
from ..combat_manager import CombatManager
from ...data.combatant.enemy_data import EnemyData
from ...data.enums import GameState


class CombatRoomHandler(IRoomHandler):
    def execute(self, run, room):
        enemy_templates = self._generate_encounter(run, room.star_rating)
        if not enemy_templates:
            run.current_state = GameState.OnMap
            return

        run.current_combat = CombatManager(
            run.the_hero,
            enemy_templates,
            run.rng,
            run.effect_pool.get_effect
        )
        run.current_combat.start_combat()
        run.current_state = GameState.InCombat

    def _generate_encounter(self, run, star_rating: int) -> List[EnemyData]:
        encounter = []

        leader = run.enemy_pool.get_random_enemy_of_star(star_rating, run.rng)
        if leader is None:
            print(f"CRITICAL ERROR: No enemies found for Star Rating {star_rating}.")
            return encounter
        encounter.append(leader)

        minion_count = 0
        max_minion_star = 1

        if star_rating == 1:
            minion_count = 0
            max_minion_star = 1
        elif star_rating == 2:
            minion_count = run.rng.randint(1, 2)
            max_minion_star = 1
        elif star_rating == 3:
            minion_count = run.rng.randint(1, 1)
            max_minion_star = 2
        elif star_rating == 4:
            minion_count = run.rng.randint(1, 2)
            max_minion_star = 3

        for _ in range(minion_count):
            minion = run.enemy_pool.get_random_enemy_below_star(max_minion_star + 1, run.rng)
            if minion is not None:
                encounter.append(minion)

        return encounter

    @staticmethod
    def generate_victory_rewards(run):
        room = run.the_map.get_current_room()
        hero = run.the_hero
        n = room.star_rating

        gold_calc = math.exp(3 + 0.5 * n) + 10
        gold_reward = int(math.floor(gold_calc))
        hero.current_gold += gold_reward

        run.card_reward_choices.clear()

        top_card = run.card_pool.get_random_card_of_star(n, run.rng)
        if top_card:
            run.card_reward_choices.append(top_card)

        for _ in range(2):
            limit = 1 if n == 1 else n - 1
            extra = run.card_pool.get_random_card_up_to_star(limit, run.rng)
            if extra:
                run.card_reward_choices.append(extra)

        run.card_reward_choices = [c for c in run.card_reward_choices if c is not None]
        run.relic_reward_choice = run.relic_pool.get_random_relic_of_star(n, run.rng)

        run.current_state = GameState.AwaitingReward
        run.current_combat = None
