from typing import Dict
from ..event.event_choice_set import EventChoiceSet


class EventPool:
    def __init__(self):
        self.events_by_id: Dict[str, EventChoiceSet] = {}
