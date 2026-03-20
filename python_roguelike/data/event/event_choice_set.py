from dataclasses import dataclass, field
from typing import List
from .event_choice import EventChoice


@dataclass
class EventChoiceSet:
    id: str = ""
    event_title: str = ""
    event_description: str = ""
    choices: List[EventChoice] = field(default_factory=list)
