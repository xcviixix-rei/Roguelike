import random
from collections import deque
from typing import List, Optional
from .combatant import Combatant
from .active_effect import ActiveEffect
from ..data.combatant.enemy_data import EnemyData
from ..data.combat_action_data import CombatActionData
from ..data.enums import ActionType, DecayType
from ..data.status_effect_data import StatusEffectData


class Enemy(Combatant):
    def __init__(self, source_data: EnemyData, rng: random.Random):
        super().__init__(source_data)
        self.source_enemy_data: EnemyData = source_data
        self._rng = rng
        self.action_bucket: deque = deque()
        self._turns_since_last_special: int = 999
        self._initialize_action_bucket()

    def _initialize_action_bucket(self):
        self.action_bucket.clear()
        actions_to_shuffle: List[CombatActionData] = []

        for weighted_action in self.source_enemy_data.action_set:
            for _ in range(weighted_action.weight):
                actions_to_shuffle.append(weighted_action.item)

        self._shuffle(actions_to_shuffle)
        for action in actions_to_shuffle:
            self.action_bucket.append(action)

    def get_next_action(self) -> CombatActionData:
        if not self.action_bucket:
            self._initialize_action_bucket()

        checks = 0
        max_checks = len(self.action_bucket) + 1

        while checks < max_checks:
            candidate = self.action_bucket[0]
            is_special = (
                candidate.type == ActionType.ApplyStatusEffect or
                candidate.type == ActionType.ApplyDeckEffect
            )
            if is_special and self._turns_since_last_special < self.source_enemy_data.special_ability_cooldown:
                # Move to back
                self.action_bucket.append(self.action_bucket.popleft())
                checks += 1
            else:
                action = self.action_bucket.popleft()
                if is_special:
                    self._turns_since_last_special = 0
                return action

        # Fallback
        return self.action_bucket.popleft()

    def peek_next_action(self) -> CombatActionData:
        if not self.action_bucket:
            self._initialize_action_bucket()
        return self.action_bucket[0]

    def tick_cooldowns(self):
        self._turns_since_last_special += 1

    def _shuffle(self, lst: List):
        n = len(lst)
        while n > 1:
            n -= 1
            k = self._rng.randint(0, n)
            lst[k], lst[n] = lst[n], lst[k]
