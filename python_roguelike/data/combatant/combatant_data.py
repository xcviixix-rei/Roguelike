from dataclasses import dataclass


@dataclass
class CombatantData:
    id: str = ""
    name: str = ""
    starting_health: int = 0
    starting_strength: int = 0
