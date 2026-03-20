from dataclasses import dataclass
from typing import TypeVar, Generic

T = TypeVar("T")


@dataclass
class ShopItem(Generic[T]):
    item: T
    price: int
    is_sold: bool = False
