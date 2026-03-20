from dataclasses import dataclass
from .enums import RoomType


@dataclass
class RoomData:
    type: RoomType = RoomType.Monster
    display_name: str = ""
    description: str = ""
    star_rating: int = 1
