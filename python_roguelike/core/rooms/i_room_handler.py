from abc import ABC, abstractmethod


class IRoomHandler(ABC):
    @abstractmethod
    def execute(self, run, room):
        pass
