from .i_room_handler import IRoomHandler
from ...data.enums import GameState, EventEffectType
import math


class EventRoomHandler(IRoomHandler):
    def execute(self, run, room):
        all_events = list(run.event_pool.events_by_id.values())
        if not all_events:
            return

        idx = run.rng.randint(0, len(all_events) - 1)
        run.current_event = all_events[idx]
        run.current_state = GameState.InEvent

    @staticmethod
    def resolve_choice(run, choice_index: int):
        if run.current_state != GameState.InEvent or run.current_event is None:
            return
        if choice_index < 0 or choice_index >= len(run.current_event.choices):
            return

        chosen = run.current_event.choices[choice_index]
        for effect in chosen.effects:
            EventRoomHandler._apply_effect(run, effect)

        run.current_event = None
        run.current_state = GameState.OnMap

    @staticmethod
    def _apply_effect(run, effect):
        if effect.type == EventEffectType.GainGold:
            run.the_hero.current_gold += effect.value
        elif effect.type == EventEffectType.LoseGold:
            run.the_hero.current_gold = max(0, run.the_hero.current_gold - effect.value)
        elif effect.type == EventEffectType.LoseHP:
            run.the_hero.take_piercing_damage(effect.value)
        elif effect.type == EventEffectType.HealHP:
            run.the_hero.heal(effect.value)
        elif effect.type == EventEffectType.GainCard:
            card = run.card_pool.get_card(effect.parameter)
            if card is not None:
                run.the_hero.deck.add_card_to_master_deck(card)
        elif effect.type == EventEffectType.RemoveCard:
            if run.the_hero.deck.master_deck:
                run.the_hero.deck.remove_card_from_master_deck(run.the_hero.deck.master_deck[0])
        elif effect.type == EventEffectType.GainRelic:
            relic = run.relic_pool.get_relic(effect.parameter)
            if relic is not None:
                run.the_hero.relics.append(relic)
        elif effect.type == EventEffectType.Quit:
            pass
