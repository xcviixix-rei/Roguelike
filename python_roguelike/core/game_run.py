import random
from typing import Dict, List, Optional
from .hero import Hero
from .map.map_manager import MapManager
from ..data.combatant.hero_data import HeroData
from ..data.event.event_choice_set import EventChoiceSet
from ..data.card_data import CardData
from ..data.relic_data import RelicData
from ..data.room_data import RoomData
from ..data.pools.card_pool import CardPool
from ..data.pools.relic_pool import RelicPool
from ..data.pools.enemy_pool import EnemyPool
from ..data.pools.effect_pool import EffectPool
from ..data.pools.event_pool import EventPool
from ..data.enums import GameState, RoomType
from .rooms.shop_inventory import ShopInventory
from .combat_manager import CombatManager


class GameRun:
    def __init__(
        self,
        seed: int,
        hero_data: HeroData,
        card_pool: CardPool,
        relic_pool: RelicPool,
        enemy_pool: EnemyPool,
        effect_pool: EffectPool,
        event_pool: EventPool,
        room_configs: Dict[RoomType, RoomData]
    ):
        self.rng: random.Random = random.Random(seed)

        self.card_pool: CardPool = card_pool
        self.relic_pool: RelicPool = relic_pool
        self.enemy_pool: EnemyPool = enemy_pool
        self.effect_pool: EffectPool = effect_pool
        self.event_pool: EventPool = event_pool
        self.room_configs: Dict[RoomType, RoomData] = room_configs

        self.the_hero: Hero = Hero(hero_data, self.rng)
        self.the_map: MapManager = MapManager()

        self.the_hero.deck.initialize_master_deck(hero_data.starting_deck_card_ids, card_pool)
        starting_relic = relic_pool.get_relic(hero_data.starting_relic_id)
        if starting_relic is not None:
            self.the_hero.relics.append(starting_relic)

        self.the_map.generate_new_map(seed)
        self.current_state: GameState = GameState.OnMap

        self.current_combat: Optional[CombatManager] = None
        self.current_event: Optional[EventChoiceSet] = None
        self.card_reward_choices: List[CardData] = []
        self.relic_reward_choice: Optional[RelicData] = None
        self.current_shop: Optional[ShopInventory] = None

    @property
    def current_floor(self) -> int:
        room = self.the_map.get_current_room()
        return room.y if room is not None else -1
