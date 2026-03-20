from abc import ABC, abstractmethod
from dataclasses import dataclass
from ...data.enums import CombatActionType, ShopActionType


@dataclass
class CombatDecision:
    type: CombatActionType = CombatActionType.EndTurn
    hand_index: int = -1
    target_index: int = -1

    @staticmethod
    def end_turn() -> "CombatDecision":
        return CombatDecision(type=CombatActionType.EndTurn)

    @staticmethod
    def play(hand_index: int, target_index: int) -> "CombatDecision":
        return CombatDecision(type=CombatActionType.PlayCard, hand_index=hand_index, target_index=target_index)


@dataclass
class ShopDecision:
    type: ShopActionType = ShopActionType.Leave
    shop_index: int = -1

    @staticmethod
    def leave() -> "ShopDecision":
        return ShopDecision(type=ShopActionType.Leave)

    @staticmethod
    def buy_card(index: int) -> "ShopDecision":
        return ShopDecision(type=ShopActionType.BuyCard, shop_index=index)

    @staticmethod
    def buy_relic(index: int) -> "ShopDecision":
        return ShopDecision(type=ShopActionType.BuyRelic, shop_index=index)


class IPlayerAgent(ABC):
    @abstractmethod
    def choose_map_node(self, run) -> int:
        pass

    @abstractmethod
    def get_combat_decision(self, run) -> CombatDecision:
        pass

    @abstractmethod
    def choose_event_option(self, run) -> int:
        pass

    @abstractmethod
    def get_shop_decision(self, run) -> ShopDecision:
        pass

    @abstractmethod
    def choose_card_reward(self, run) -> int:
        pass
