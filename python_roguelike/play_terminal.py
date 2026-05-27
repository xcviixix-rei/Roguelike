"""
play_terminal.py  –  Simple terminal (text UI) front-end for the Roguelike engine.

Run from the repo root:
    python -m python_roguelike.play_terminal
"""

import os
import sys

# Allow running as a script from any directory
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from python_roguelike.data_loader import load_game_data
from python_roguelike.core.game_controller import GameController
from python_roguelike.data.enums import (
    GameState, CombatState, RoomType, ActionType, CardType
)
from python_roguelike.data.status_effect_data import StatusEffectData
from python_roguelike.data.deck_effect_data import DeckEffectData

# ─────────────────────────────────────────────
# Helpers
# ─────────────────────────────────────────────

ROOM_ICONS = {
    RoomType.Monster: "[M]",
    RoomType.Elite:   "[E]",
    RoomType.Boss:    "[B]",
    RoomType.Event:   "[?]",
    RoomType.Shop:    "[$]",
    RoomType.Rest:    "[R]",
    RoomType.NONE:    "[ ]",
}

CARD_TYPE_ICONS = {
    CardType.Attack: "ATK",
    CardType.Skill:  "SKL",
    CardType.Heal:   "HEL",
    CardType.Power:  "PWR",
}


def clear():
    os.system("cls" if os.name == "nt" else "clear")


def separator(char="─", width=60):
    print(char * width)


def prompt(msg="Your choice: ") -> str:
    try:
        return input(f"\n{msg}").strip()
    except (EOFError, KeyboardInterrupt):
        print("\nExiting.")
        sys.exit(0)


def pick(options: list, prompt_msg="Choose: ") -> int:
    """Print numbered options and return the validated 0-based index."""
    for i, opt in enumerate(options):
        print(f"  [{i}] {opt}")
    while True:
        raw = prompt(prompt_msg)
        if raw.isdigit():
            idx = int(raw)
            if 0 <= idx < len(options):
                return idx
        print(f"  Enter a number between 0 and {len(options) - 1}.")


# ─────────────────────────────────────────────
# Display helpers
# ─────────────────────────────────────────────

def fmt_effects(combatant) -> str:
    parts = []
    for ae in combatant.active_effects:
        sd = ae.source_data
        name = getattr(sd, "name", sd.id) or sd.id
        if isinstance(sd, StatusEffectData):
            parts.append(f"{name}({ae.remaining_duration}t)")
        else:
            parts.append(name)
    return ", ".join(parts) if parts else "—"


def show_hero(run):
    hero = run.the_hero
    hp_bar_len = 20
    filled = int(hp_bar_len * hero.current_health / max(hero.max_health, 1))
    bar = "█" * filled + "░" * (hp_bar_len - filled)
    print(f"  HP  [{bar}] {hero.current_health}/{hero.max_health}   "
          f"Block: {hero.block}   Mana: {hero.current_mana}/{hero.max_mana}   "
          f"Gold: {hero.current_gold}g")
    if hero.relics:
        relic_names = ", ".join(r.name or r.id for r in hero.relics)
        print(f"  Relics: {relic_names}")
    fx = fmt_effects(hero)
    if fx != "—":
        print(f"  Effects: {fx}")


def show_enemies(combat):
    for i, e in enumerate(combat.enemies):
        if e.current_health <= 0:
            print(f"  [{i}] {e.source_data.name or e.source_data.id}  *** DEAD ***")
            continue
        hp_bar_len = 15
        filled = int(hp_bar_len * e.current_health / max(e.max_health, 1))
        bar = "█" * filled + "░" * (hp_bar_len - filled)
        intent = combat.current_enemy_intents.get(e)
        intent_str = _fmt_intent(intent)
        fx = fmt_effects(e)
        print(f"  [{i}] {e.source_data.name or e.source_data.id:<20} "
              f"HP [{bar}] {e.current_health}/{e.max_health}  "
              f"Block: {e.block}   Intent: {intent_str}   Effects: {fx}")


def _fmt_intent(action) -> str:
    if action is None:
        return "?"
    if action.type == ActionType.DealDamage:
        return f"Attack {action.value}"
    if action.type == ActionType.GainBlock:
        return f"Block {action.value}"
    if action.type == ActionType.GainHealth:
        return f"Heal {action.value}"
    if action.type in (ActionType.ApplyStatusEffect, ActionType.ApplyDeckEffect):
        return f"Ability ({action.effect_id})"
    return str(action.type.value)


def show_hand(hero):
    if not hero.deck.hand:
        print("  (hand is empty)")
        return
    for i, card in enumerate(hero.deck.hand):
        ctype = CARD_TYPE_ICONS.get(card.type, "???")
        actions_str = _fmt_card_actions(card)
        print(f"  [{i}] {card.name:<22} [{ctype}]  Cost:{card.mana_cost}  {actions_str}")


def _fmt_card_actions(card) -> str:
    parts = []
    for a in card.actions:
        if a.type == ActionType.DealDamage:
            parts.append(f"Deal {a.value} dmg")
        elif a.type == ActionType.GainBlock:
            parts.append(f"Gain {a.value} block")
        elif a.type == ActionType.GainHealth:
            parts.append(f"Heal {a.value}")
        elif a.type == ActionType.ApplyStatusEffect:
            parts.append(f"Apply {a.effect_id}")
        elif a.type == ActionType.ApplyDeckEffect:
            parts.append(f"Deck:{a.effect_id}×{a.value}")
    return ", ".join(parts) if parts else card.description or ""


def show_map_options(run):
    nodes = run.the_map.get_possible_next_nodes()
    if not nodes:
        print("  (no nodes available)")
        return nodes
    for i, room in enumerate(nodes):
        icon = ROOM_ICONS.get(room.type, "[ ]")
        print(f"  [{i}] {icon} {room.type.value:<10}  ★{'★' * (room.star_rating - 1)}{' ' * (4 - room.star_rating)}  Floor {room.y + 1}")
    return nodes


# ─────────────────────────────────────────────
# State screens
# ─────────────────────────────────────────────

def screen_on_map(ctrl, run):
    clear()
    separator("═")
    print("  MAP  —  Choose your next room")
    separator("═")
    show_hero(run)
    separator()
    print(f"  Current floor: {run.current_floor + 1}")
    separator()
    nodes = show_map_options(run)
    if not nodes:
        return
    idx = pick(range(len(nodes)), "Enter room number: ")
    ctrl.choose_map_node(nodes[idx].id)


def screen_in_combat(ctrl, run):
    combat = run.current_combat
    while run.current_state == GameState.InCombat and combat.state == CombatState.Ongoing_PlayerTurn:
        clear()
        separator("═")
        print("  COMBAT")
        separator("═")
        show_hero(run)
        separator()
        show_enemies(combat)
        separator()
        print("  Hand:")
        show_hand(run.the_hero)
        separator()
        print("  [d] Deck info   [e] End turn")
        separator()

        raw = prompt("Action (card index or 'e'): ").lower()

        if raw == "e":
            ctrl.end_turn()
            break
        elif raw == "d":
            _show_deck_info(run)
        elif raw.isdigit():
            idx = int(raw)
            hand = run.the_hero.deck.hand
            if 0 <= idx < len(hand):
                card = hand[idx]
                _play_card(ctrl, run, idx, card)
                if run.current_state != GameState.InCombat:
                    break
            else:
                print("  Invalid card index.")
                prompt("Press Enter to continue...")
        else:
            print("  Unknown command.")
            prompt("Press Enter to continue...")


def _play_card(ctrl, run, hand_idx, card):
    from python_roguelike.data.enums import TargetType
    needs_target = any(
        a.target == TargetType.SingleOpponent for a in card.actions
    )
    target_idx = 0
    if needs_target:
        combat = run.current_combat
        living = [(i, e) for i, e in enumerate(combat.enemies) if e.current_health > 0]
        if not living:
            return
        if len(living) == 1:
            target_idx = living[0][0]
        else:
            print("  Choose target:")
            for li, (ei, e) in enumerate(living):
                print(f"    [{li}] [{ei}] {e.source_data.name or e.source_data.id}  "
                      f"HP:{e.current_health}/{e.max_health}")
            ti = pick(range(len(living)), "Target: ")
            target_idx = living[ti][0]

    ok = ctrl.play_card(hand_idx, target_idx)
    if not ok:
        print("  Cannot play that card (not enough mana?).")
        prompt("Press Enter...")


def _show_deck_info(run):
    hero = run.the_hero
    separator()
    print(f"  Master deck ({len(hero.deck.master_deck)} cards):")
    for c in hero.deck.master_deck:
        print(f"    {c.name}")
    print(f"  Draw pile: {len(hero.deck.draw_pile)}   Discard pile: {len(hero.deck.discard_pile)}")
    separator()
    prompt("Press Enter to continue...")


def screen_combat_result(run):
    clear()
    separator("═")
    combat = run.current_combat
    if combat is None or run.current_state == GameState.AwaitingReward:
        print("  *** VICTORY! ***")
    else:
        print("  *** DEFEAT ***")
    separator("═")
    show_hero(run)
    separator()
    prompt("Press Enter to continue...")


def screen_awaiting_reward(ctrl, run):
    clear()
    separator("═")
    print("  REWARDS")
    separator("═")
    show_hero(run)
    separator()
    choices = run.card_reward_choices
    if run.relic_reward_choice:
        r = run.relic_reward_choice
        print(f"  Relic reward: {r.name or r.id}  —  {r.description}")
        separator()
    if choices:
        print("  Choose a card (or skip):")
        for i, card in enumerate(choices):
            ctype = CARD_TYPE_ICONS.get(card.type, "???")
            print(f"  [{i}] {card.name:<22} [{ctype}]  Cost:{card.mana_cost}  "
                  f"{_fmt_card_actions(card)}")
        print(f"  [{len(choices)}] Skip (take no card)")
        idx = pick(range(len(choices) + 1), "Choose: ")
        if idx >= len(choices):
            idx = -1
    else:
        idx = -1
    ctrl.confirm_rewards(idx)


def screen_in_event(ctrl, run):
    clear()
    separator("═")
    ev = run.current_event
    print(f"  EVENT: {ev.event_title}")
    separator("═")
    show_hero(run)
    separator()
    print(f"  {ev.event_description}")
    separator()
    print("  Choices:")
    options = [c.choice_text for c in ev.choices]
    idx = pick(options, "Choose: ")
    ctrl.choose_event_option(idx)


def screen_in_shop(ctrl, run):
    while run.current_state == GameState.InShop:
        clear()
        separator("═")
        print("  SHOP")
        separator("═")
        show_hero(run)
        separator()
        shop = run.current_shop
        print("  Cards:")
        for i, item in enumerate(shop.cards_for_sale):
            sold = " (SOLD)" if item.is_sold else ""
            ctype = CARD_TYPE_ICONS.get(item.item.type, "???")
            can_afford = "✓" if run.the_hero.current_gold >= item.price and not item.is_sold else "✗"
            print(f"  [{i}] {item.item.name:<22} [{ctype}]  {item.price}g {can_afford}{sold}")
        separator()
        print("  Relics:")
        for i, item in enumerate(shop.relics_for_sale):
            sold = " (SOLD)" if item.is_sold else ""
            can_afford = "✓" if run.the_hero.current_gold >= item.price and not item.is_sold else "✗"
            print(f"  [{i}] {item.item.name or item.item.id:<22}  {item.price}g {can_afford}{sold}  {item.item.description}")
        separator()
        print("  [c<n>] Buy card n   [r<n>] Buy relic n   [l] Leave shop")
        raw = prompt("Action: ").lower()
        if raw == "l":
            ctrl.leave_shop()
        elif raw.startswith("c") and raw[1:].isdigit():
            idx = int(raw[1:])
            ok = ctrl.buy_shop_card(idx)
            if not ok:
                print("  Cannot buy that (sold or not enough gold).")
                prompt("Press Enter...")
        elif raw.startswith("r") and raw[1:].isdigit():
            idx = int(raw[1:])
            ok = ctrl.buy_shop_relic(idx)
            if not ok:
                print("  Cannot buy that (sold or not enough gold).")
                prompt("Press Enter...")
        else:
            print("  Unknown command.")
            prompt("Press Enter...")


def screen_rest(run):
    clear()
    separator("═")
    print("  REST SITE  —  You take a moment to recover.")
    separator("═")
    show_hero(run)
    separator()
    prompt("Press Enter to continue...")
    # RestRoomHandler already healed hero when the node was entered;
    # returning to map is handled by the rest handler too.
    if run.current_state not in (GameState.OnMap, GameState.GameOver):
        run.current_state = GameState.OnMap


def screen_game_over(run):
    clear()
    separator("═")
    hero = run.the_hero
    if hero.current_health > 0:
        print("  ╔══════════════════════╗")
        print("  ║   YOU WIN!             ║")
        print("  ╚══════════════════════╝")
    else:
        print("  ╔══════════════════════╗")
        print("  ║   GAME OVER          ║")
        print("  ╚══════════════════════╝")
    separator("═")
    show_hero(run)
    separator()
    print(f"  Final floor reached: {run.current_floor + 1}")
    print(f"  Deck size: {len(hero.deck.master_deck)}")
    separator()
    prompt("Press Enter to exit...")


# ─────────────────────────────────────────────
# Main game loop
# ─────────────────────────────────────────────

def run_game():
    clear()
    print("=" * 60)
    print("  ROGUELIKE — Terminal Edition")
    print("=" * 60)
    print()
    seed_raw = prompt("Enter seed (or press Enter for random): ")
    seed = int(seed_raw) if seed_raw.isdigit() else __import__("random").randint(0, 2**31)
    print(f"  Using seed: {seed}")

    card_pool, relic_pool, enemy_pool, effect_pool, event_pool, room_configs, hero_data = load_game_data()
    ctrl = GameController(card_pool, relic_pool, enemy_pool, effect_pool, event_pool, room_configs)
    ctrl.start_new_run(seed, hero_data)
    run = ctrl.current_run

    prev_state = None

    while run.current_state != GameState.GameOver:
        state = run.current_state

        # Show combat result screen when transitioning OUT of combat
        if prev_state == GameState.InCombat and state != GameState.InCombat:
            if state == GameState.AwaitingReward:
                pass  # reward screen is next
            elif state != GameState.GameOver:
                screen_combat_result(run)

        prev_state = state

        if state == GameState.OnMap:
            screen_on_map(ctrl, run)

        elif state == GameState.InCombat:
            screen_in_combat(ctrl, run)

        elif state == GameState.AwaitingReward:
            screen_awaiting_reward(ctrl, run)

        elif state == GameState.InEvent:
            screen_in_event(ctrl, run)

        elif state == GameState.InShop:
            screen_in_shop(ctrl, run)

        # Rest is handled immediately by handler; just show a message
        elif state == GameState.OnMap and prev_state != GameState.OnMap:
            pass

    screen_game_over(run)


if __name__ == "__main__":
    run_game()
