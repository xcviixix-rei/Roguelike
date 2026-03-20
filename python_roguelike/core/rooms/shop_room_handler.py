from .i_room_handler import IRoomHandler
from .shop_inventory import ShopInventory
from ...data.enums import GameState


class ShopRoomHandler(IRoomHandler):
    def execute(self, run, room):
        run.current_shop = ShopInventory(run.card_pool, run.relic_pool, run.rng)
        run.current_state = GameState.InShop

    @staticmethod
    def purchase_card(run, card_index: int) -> bool:
        if run.current_state != GameState.InShop or run.current_shop is None:
            return False
        if card_index < 0 or card_index >= len(run.current_shop.cards_for_sale):
            return False

        item = run.current_shop.cards_for_sale[card_index]
        if item.is_sold or run.the_hero.current_gold < item.price:
            return False

        run.the_hero.current_gold -= item.price
        run.the_hero.deck.add_card_to_master_deck(item.item)
        item.is_sold = True
        return True

    @staticmethod
    def purchase_relic(run, relic_index: int) -> bool:
        if run.current_state != GameState.InShop or run.current_shop is None:
            return False
        if relic_index < 0 or relic_index >= len(run.current_shop.relics_for_sale):
            return False

        item = run.current_shop.relics_for_sale[relic_index]
        if item.is_sold or run.the_hero.current_gold < item.price:
            return False

        run.the_hero.current_gold -= item.price
        run.the_hero.relics.append(item.item)
        item.is_sold = True
        return True

    @staticmethod
    def leave_shop(run):
        if run.current_state != GameState.InShop:
            return
        run.current_shop = None
        run.current_state = GameState.OnMap
