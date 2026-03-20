from enum import Enum, auto


class TargetType(Enum):
    Self = "Self"
    SingleOpponent = "SingleOpponent"
    AllOpponents = "AllOpponents"
    RandomOpponent = "RandomOpponent"


class ApplyType(Enum):
    RightAway = "RightAway"
    StartOfCombat = "StartOfCombat"
    StartOfTurn = "StartOfTurn"


class IntensityType(Enum):
    Flat = "Flat"
    Percentage = "Percentage"


class DecayType(Enum):
    AfterXTURNS = "AfterXTURNS"
    Permanent = "Permanent"


class CardType(Enum):
    Attack = "Attack"
    Skill = "Skill"
    Heal = "Heal"
    Power = "Power"


class ActionType(Enum):
    DealDamage = "DealDamage"
    GainBlock = "GainBlock"
    GainHealth = "GainHealth"
    ApplyStatusEffect = "ApplyStatusEffect"
    ApplyDeckEffect = "ApplyDeckEffect"


class StatusEffectType(Enum):
    Vulnerable = "Vulnerable"
    Weakened = "Weakened"
    Strength = "Strength"
    Frail = "Frail"
    Pierced = "Pierced"
    Philosophical = "Philosophical"
    ImmediateBlock = "ImmediateBlock"


class DeckEffectType(Enum):
    DrawCard = "DrawCard"
    DiscardCard = "DiscardCard"
    FreezeCard = "FreezeCard"
    DuplicateCard = "DuplicateCard"


class EventEffectType(Enum):
    GainGold = "GainGold"
    LoseGold = "LoseGold"
    LoseHP = "LoseHP"
    HealHP = "HealHP"
    RemoveCard = "RemoveCard"
    GainCard = "GainCard"
    GainRelic = "GainRelic"
    Quit = "Quit"


class RoomType(Enum):
    NONE = "None"
    Monster = "Monster"
    Elite = "Elite"
    Event = "Event"
    Shop = "Shop"
    Rest = "Rest"
    Boss = "Boss"


class GameState(Enum):
    PreRun = "PreRun"
    OnMap = "OnMap"
    InCombat = "InCombat"
    InEvent = "InEvent"
    InShop = "InShop"
    AwaitingReward = "AwaitingReward"
    GameOver = "GameOver"


class CombatState(Enum):
    Ongoing_PlayerTurn = "Ongoing_PlayerTurn"
    Ongoing_EnemyTurn = "Ongoing_EnemyTurn"
    Victory = "Victory"
    Defeat = "Defeat"


class CombatActionType(Enum):
    PlayCard = "PlayCard"
    EndTurn = "EndTurn"


class ShopActionType(Enum):
    BuyCard = "BuyCard"
    BuyRelic = "BuyRelic"
    Leave = "Leave"
