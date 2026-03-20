from dataclasses import dataclass, field
from typing import List
from .event_effect import EventEffect


@dataclass
class EventChoice:
    choice_text: str = ""
    effects: List[EventEffect] = field(default_factory=list)
