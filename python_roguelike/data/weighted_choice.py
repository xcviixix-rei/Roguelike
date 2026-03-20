import random
from dataclasses import dataclass
from typing import TypeVar, Generic, List, Optional

T = TypeVar("T")


@dataclass
class WeightedChoice(Generic[T]):
    item: T
    weight: int

    def __post_init__(self):
        if self.weight <= 0:
            import sys
            print("weight must be > 0", file=sys.stderr)


def weighted_random_pick(choices: List[WeightedChoice], rng: random.Random):
    if not choices:
        return None
    total_weight = sum(c.weight for c in choices)
    if total_weight <= 0:
        return None
    rand_num = rng.randint(0, total_weight - 1)
    cumulative = 0
    for choice in choices:
        cumulative += choice.weight
        if rand_num < cumulative:
            return choice.item
    return choices[-1].item
