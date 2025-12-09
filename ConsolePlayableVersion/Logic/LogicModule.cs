using System;
using System.Collections.Generic;
using System.Linq;
using Roguelike.Data;
using RoguelikeMapGen;

namespace Roguelike.Logic
{
    public class ActiveEffect
    {
        public EffectData SourceData { get; }
        public int Stacks { get; set; }
        public int Duration { get; set; }
        public ActiveEffect(EffectData data, int stacks) { SourceData = data; Stacks = stacks; if (data.Decay == DecayType.AfterXTURNS) Duration = stacks; }
        public bool TickDown() { if (SourceData.Decay == DecayType.AfterXTURNS) { Duration--; return Duration <= 0; } return false; }
    }

    public class DeckManager
    {
        private readonly Random rng;
        public List<CardData> MasterDeck { get; private set; } = new List<CardData>();
        public List<CardData> DrawPile { get; private set; } = new List<CardData>();
        public List<CardData> Hand { get; private set; } = new List<CardData>();
        public List<CardData> DiscardPile { get; private set; } = new List<CardData>();
        public DeckManager(Random r) { rng = r; }
        public void InitializeMasterDeck(IEnumerable<string> ids, CardPool pool) { MasterDeck.Clear(); foreach (var id in ids) { var c = pool.GetCard(id); if (c != null) MasterDeck.Add(c); } }
        public void AddCardToMasterDeck(CardData c) => MasterDeck.Add(c);
        public void RemoveCardFromMasterDeck(CardData c) => MasterDeck.Remove(c);
        public void StartCombat() { DrawPile.Clear(); Hand.Clear(); DiscardPile.Clear(); DrawPile.AddRange(MasterDeck); Shuffle(DrawPile); }
        public void DrawCards(int amt) {
            for (int i = 0; i < amt; i++) {
                if (DrawPile.Count == 0) { if (DiscardPile.Count == 0) break; Reshuffle(); }
                Hand.Add(DrawPile[0]); DrawPile.RemoveAt(0);
            }
        }
        public void DiscardCardFromHand(CardData c) { if (Hand.Remove(c)) DiscardPile.Add(c); }
        public void DiscardHand() { DiscardPile.AddRange(Hand); Hand.Clear(); }
        private void Reshuffle() { DrawPile.AddRange(DiscardPile); DiscardPile.Clear(); Shuffle(DrawPile); }
        private void Shuffle(List<CardData> l) { int n = l.Count; while (n > 1) { n--; int k = rng.Next(n + 1); var v = l[k]; l[k] = l[n]; l[n] = v; } }
    }

    public abstract class Combatant
    {
        public CombatantData SourceData { get; }
        public int CurrentHealth { get; set; }
        public int MaxHealth { get; set; }
        public int Block { get; set; }
        public List<ActiveEffect> ActiveEffects { get; } = new List<ActiveEffect>();
        protected Combatant(CombatantData d) { SourceData = d; MaxHealth = d.StartingHealth; CurrentHealth = MaxHealth; }
        public void TakeDamage(int amt) { if(amt<=0) return; int dmgToBlock = Math.Min(amt, Block); Block -= dmgToBlock; int rem = amt - dmgToBlock; if(rem>0) CurrentHealth = Math.Max(0, CurrentHealth - rem); }
        public void TakePiercingDamage(int amt) { if(amt>0) CurrentHealth = Math.Max(0, CurrentHealth - amt); }
        public void GainBlock(int amt) { if(amt>0) Block += amt; }
        public void ApplyEffect(EffectData d, int s) { var e = ActiveEffects.FirstOrDefault(x => x.SourceData.Id == d.Id); if(e!=null) { e.Stacks+=s; if(d.Decay==DecayType.AfterXTURNS) e.Duration+=s; } else ActiveEffects.Add(new ActiveEffect(d,s)); }
        public void TickDownEffects() { var exp = new List<ActiveEffect>(); foreach(var e in ActiveEffects) { if(e.SourceData.Decay == DecayType.EndOfTurn || e.TickDown()) exp.Add(e); } foreach(var x in exp) ActiveEffects.Remove(x); }
        public void ResetForNewCombat() { Block = 0; ActiveEffects.RemoveAll(e => e.SourceData.Decay != DecayType.Permanent); }
    }

public class Hero : Combatant
    {
        public HeroData HeroData => (HeroData)SourceData;
        public DeckManager Deck { get; }
        public int CurrentMana { get; set; }
        public int MaxMana { get; set; }
        public int CurrentGold { get; set; }
        public List<RelicData> Relics { get; } = new List<RelicData>();

        public Hero(HeroData d, Random r) : base(d) { Deck = new DeckManager(r); MaxMana = d.StartingMana; CurrentGold = d.StartingGold; }

        public void StartTurn() { Block = 0; CurrentMana = MaxMana; Deck.DrawCards(HeroData.StartingHandSize); }
        public void EquipRelic(RelicData r)
        {
            Relics.Add(r);
            foreach(var effect in r.Effects)
            {
                if(effect is StatusEffectData sd && sd.Decay == DecayType.Permanent)
                {
                    ApplyEffect(sd, sd.Value); 
                }
            }
        }
        public void OnCombatEnd()
        {
            ResetForNewCombat(); 
            if(Relics.Any(r => r.Id == "burning_blood"))
            {
                int heal = 6;
                CurrentHealth = Math.Min(MaxHealth, CurrentHealth + heal);
            }
        }
    }

    public class Enemy : Combatant
    {
        public EnemyData EnemyData => (EnemyData)SourceData;
        private readonly Random rng;
        private Queue<CombatActionData> ActionBucket { get; } = new Queue<CombatActionData>();
        public Enemy(EnemyData d, Random r) : base(d) { rng = r; InitializeActionBucket(); }
        public void InitializeActionBucket() { 
            ActionBucket.Clear(); var list = new List<CombatActionData>();
            foreach(var w in EnemyData.ActionSet) for(int i=0; i<w.Weight; i++) list.Add(w.Item);
            int n = list.Count; while (n > 1) { n--; int k = rng.Next(n + 1); var v = list[k]; list[k] = list[n]; list[n] = v; }
            foreach(var a in list) ActionBucket.Enqueue(a);
        }
        public CombatActionData GetNextAction() { if(ActionBucket.Count == 0) InitializeActionBucket(); return ActionBucket.Dequeue(); }
        public CombatActionData PeekNextAction() { if(ActionBucket.Count == 0) InitializeActionBucket(); return ActionBucket.Peek(); }
    }

    public static class ActionResolver
    {
        public static void Resolve(CombatActionData action, Combatant source, Combatant target, Func<string, EffectData> getEffectById)
        {
            switch (action.Type)
            {
                case ActionType.DealDamage: ApplyDamage(action.Value, source, target); break;
                case ActionType.GainBlock: ApplyBlock(action.Value, source, target); break;
                case ActionType.ApplyStatusEffect: ApplyStatusEffect(action.EffectId, action.Value, target, getEffectById); break;
                case ActionType.ApplyDeckEffect: ApplyDeckEffect(action.EffectId, action.Value, target, getEffectById); break;
            }
        }
        private static void ApplyDamage(int baseDmg, Combatant s, Combatant t) {
            float final = baseDmg;
            var str = s.ActiveEffects.FirstOrDefault(e => e.SourceData is StatusEffectData sd && sd.EffectType == StatusEffectType.Strength);
            if(str!=null) final += (str.Stacks * str.SourceData.Value);
            var weak = s.ActiveEffects.FirstOrDefault(e => e.SourceData is StatusEffectData sd && sd.EffectType == StatusEffectType.Weakened);
            if(weak!=null) final *= (weak.SourceData.Value / 100f);
            var vuln = t.ActiveEffects.FirstOrDefault(e => e.SourceData is StatusEffectData sd && sd.EffectType == StatusEffectType.Vulnerable);
            if(vuln!=null) final *= (vuln.SourceData.Value / 100f);
            int d = (int)Math.Floor(final);
            var pierce = t.ActiveEffects.FirstOrDefault(e => e.SourceData is StatusEffectData sd && sd.EffectType == StatusEffectType.Pierced);
            if(pierce!=null) t.TakePiercingDamage(d); else t.TakeDamage(d);
        }
        private static void ApplyBlock(int baseBlk, Combatant s, Combatant t) {
            float final = baseBlk;
            var frail = t.ActiveEffects.FirstOrDefault(e => e.SourceData is StatusEffectData sd && sd.EffectType == StatusEffectType.Frail);
            if(frail!=null) final *= (frail.SourceData.Value / 100f);
            t.GainBlock((int)Math.Floor(final));
        }
        private static void ApplyStatusEffect(string id, int v, Combatant t, Func<string, EffectData> f) { var d = f(id); if(d is StatusEffectData) t.ApplyEffect(d,v); }
        private static void ApplyDeckEffect(string id, int v, Combatant t, Func<string, EffectData> f) {
            if(t is Hero h) { var d = f(id); if(d is DeckEffectData dd && dd.EffectType == DeckEffectType.DrawCard) h.Deck.DrawCards(v);
            else if(d is DeckEffectData dd2 && dd2.EffectType == DeckEffectType.DiscardCard && h.Deck.Hand.Any()) h.Deck.DiscardCardFromHand(h.Deck.Hand[0]); }
        }
    }

    public enum CombatState { Ongoing_PlayerTurn, Ongoing_EnemyTurn, Victory, Defeat }
    public class CombatManager
    {
        public Hero TheHero; public List<Enemy> Enemies; public CombatState State; private Random rng; private Func<string, EffectData> efLookup;
        public Dictionary<Enemy, CombatActionData> CurrentEnemyIntents = new Dictionary<Enemy, CombatActionData>();
        public CombatManager(Hero h, List<EnemyData> et, Random r, Func<string, EffectData> f) { TheHero = h; Enemies = et.Select(e=>new Enemy(e,r)).ToList(); rng = r; efLookup=f; }
        public void StartCombat() { TheHero.ResetForNewCombat(); TheHero.Deck.StartCombat(); BeginPlayerTurn(); }
        public bool PlayCard(CardData c, Enemy t) {
            if(State!=CombatState.Ongoing_PlayerTurn || TheHero.CurrentMana < c.ManaCost) return false;
            TheHero.CurrentMana -= c.ManaCost;
            foreach(var a in c.Actions) {
                var targets = GetTargets(TheHero, t, a.Target);
                foreach(var tar in targets) ActionResolver.Resolve(a, TheHero, tar, efLookup);
            }
            TheHero.Deck.DiscardCardFromHand(c); CheckStatus(); return true;
        }
        public void EndPlayerTurn() { if(State==CombatState.Ongoing_PlayerTurn) { TheHero.Deck.DiscardHand(); TheHero.TickDownEffects(); BeginEnemyTurn(); } }
        private void BeginPlayerTurn() { State=CombatState.Ongoing_PlayerTurn; TheHero.StartTurn(); CurrentEnemyIntents.Clear(); foreach(var e in Enemies.Where(x=>x.CurrentHealth>0)) CurrentEnemyIntents[e]=e.PeekNextAction(); }
        private void BeginEnemyTurn() {
            State=CombatState.Ongoing_EnemyTurn;
            foreach(var e in Enemies.Where(x=>x.CurrentHealth>0)) {
                var a = e.GetNextAction(); var tars = GetTargets(e, TheHero, a.Target);
                foreach(var t in tars) ActionResolver.Resolve(a, e, t, efLookup);
                CheckStatus(); if(State==CombatState.Defeat) return;
            }
            foreach(var e in Enemies) e.TickDownEffects();
            CheckStatus(); if(State==CombatState.Ongoing_EnemyTurn) BeginPlayerTurn();
        }
        private void CheckStatus() { if(TheHero.CurrentHealth<=0) State=CombatState.Defeat; else if(Enemies.All(e=>e.CurrentHealth<=0)) State=CombatState.Victory; }
        private IEnumerable<Combatant> GetTargets(Combatant s, Combatant t, TargetType tt) {
            var live = Enemies.Where(e=>e.CurrentHealth>0).ToList();
            if(tt==TargetType.Self) return new[]{s};
            if(tt==TargetType.SingleOpponent) return s is Hero ? new[]{t} : new[]{TheHero};
            if(tt==TargetType.AllOpponents) return s is Hero ? live.Cast<Combatant>() : new[]{TheHero};
            if(tt==TargetType.RandomOpponent && s is Hero && live.Any()) return new[]{live[rng.Next(live.Count)]};
            return new[]{TheHero};
        }
    }

    public enum GameState { PreRun, OnMap, InCombat, InEvent, InShop, AwaitingReward, GameOver }
    public class ShopItem<T> { public T Item; public int Price; public bool IsSold; public ShopItem(T i, int p) { Item=i; Price=p; } }
    public class ShopInventory {
        public List<ShopItem<CardData>> CardsForSale = new List<ShopItem<CardData>>();
        public List<ShopItem<RelicData>> RelicsForSale = new List<ShopItem<RelicData>>();
        public ShopInventory(CardPool cp, RelicPool rp, Random rng) {
            foreach(Rarity r in Enum.GetValues(typeof(Rarity))) {
                if (r == Rarity.Boss) continue;
                var c = cp.GetRandomCardOfRarity(r, rng); if(c!=null) CardsForSale.Add(new ShopItem<CardData>(c, rng.Next(cp.CostRangesByRarity[r].MinCost, cp.CostRangesByRarity[r].MaxCost)));
                var rel = rp.GetRandomRelicOfRarity(r, rng); if(rel!=null) RelicsForSale.Add(new ShopItem<RelicData>(rel, rng.Next(rp.CostRangesByRarity[r].MinCost, rp.CostRangesByRarity[r].MaxCost)));
            }
        }
    }

    public class MapManager {
        public MapGraph CurrentMap; public int CurrentNodeId = -1;
        public void GenerateNewMap(int seed) { CurrentMap = new MapGenerator(seed).Generate(); }
        public Room GetCurrentRoom() => CurrentNodeId == -1 ? null : CurrentMap.Rooms[CurrentNodeId];
        public List<Room> GetPossibleNextNodes() {
            if(CurrentNodeId==-1) return CurrentMap.RoomsOnFloor(0).ToList();
            var r = GetCurrentRoom(); return r==null ? new List<Room>() : r.Outgoing.Select(id=>CurrentMap.Rooms[id]).ToList();
        }
        public bool MoveToNode(int id) { if(GetPossibleNextNodes().Any(r=>r.Id==id)) { CurrentNodeId=id; return true; } return false; }
    }

    public class GameRun {
        public Hero TheHero; public MapManager TheMap; public Random Rng;
        public GameState CurrentState; public CombatManager CurrentCombat; public EventChoiceSet CurrentEvent; public ShopInventory CurrentShop;
        public List<CardData> CardRewardChoices = new List<CardData>(); public RelicData RelicRewardChoice;
        public CardPool CardPool; public RelicPool RelicPool; public EnemyPool EnemyPool; public EffectPool EffectPool; public EventPool EventPool;
        public Dictionary<RoomType, RoomData> RoomConfigs;
        public int CurrentFloor => TheMap.GetCurrentRoom()?.Y ?? -1;
        public GameRun(int seed, HeroData hd, CardPool cp, RelicPool rp, EnemyPool ep, EffectPool efp, EventPool evp, Dictionary<RoomType, RoomData> rc) {
            Rng = new Random(seed); CardPool=cp; RelicPool=rp; EnemyPool=ep; EffectPool=efp; EventPool=evp; RoomConfigs=rc;
            TheHero = new Hero(hd, Rng); TheMap = new MapManager(); TheMap.GenerateNewMap(seed); CurrentState=GameState.OnMap;
            TheHero.Deck.InitializeMasterDeck(hd.StartingDeckCardIds, cp);
        }
    }
    namespace Handlers {
        public interface IRoomHandler { void Execute(GameRun r, Room rm); }
        public class RestRoomHandler : IRoomHandler {
            public void Execute(GameRun r, Room rm) {
                int heal = (int)Math.Floor(r.TheHero.MaxHealth * 0.3f);
                r.TheHero.CurrentHealth = Math.Min(r.TheHero.MaxHealth, r.TheHero.CurrentHealth + heal);
            }
        }
        public class EventRoomHandler : IRoomHandler {
            public void Execute(GameRun r, Room rm) {
                var l = r.EventPool.EventsById.Values.ToList(); if(!l.Any()) return;
                r.CurrentEvent = l[r.Rng.Next(l.Count)]; r.CurrentState = GameState.InEvent;
            }
            public static void ResolveChoice(GameRun r, int idx) {
                if(r.CurrentEvent==null || idx<0 || idx>=r.CurrentEvent.Choices.Count) return;
                var c = r.CurrentEvent.Choices[idx];
                foreach(var e in c.Effects) {
                    if(e.Type==EventEffectType.GainGold) r.TheHero.CurrentGold+=e.Value;
                    else if(e.Type==EventEffectType.LoseGold) r.TheHero.CurrentGold = Math.Max(0, r.TheHero.CurrentGold-e.Value);
                    else if(e.Type==EventEffectType.LoseHP) r.TheHero.TakePiercingDamage(e.Value);
                    else if(e.Type==EventEffectType.HealHP) r.TheHero.CurrentHealth = Math.Min(r.TheHero.MaxHealth, r.TheHero.CurrentHealth+e.Value);
                    else if(e.Type==EventEffectType.GainCard) { var cd = r.CardPool.GetCard(e.Parameter); if(cd!=null) r.TheHero.Deck.AddCardToMasterDeck(cd); }
                    else if(e.Type==EventEffectType.RemoveCard && r.TheHero.Deck.MasterDeck.Any()) r.TheHero.Deck.RemoveCardFromMasterDeck(r.TheHero.Deck.MasterDeck[0]);
                    else if(e.Type==EventEffectType.GainRelic) { var rel = r.RelicPool.GetRelic(e.Parameter); if(rel!=null) r.TheHero.Relics.Add(rel); }
                }
                r.CurrentEvent=null; r.CurrentState=GameState.OnMap;
            }
        }
        public class ShopRoomHandler : IRoomHandler {
            public void Execute(GameRun r, Room rm) { r.CurrentShop = new ShopInventory(r.CardPool, r.RelicPool, r.Rng); r.CurrentState = GameState.InShop; }
            public static bool PurchaseCard(GameRun r, int i) {
                if(i<0||i>=r.CurrentShop.CardsForSale.Count) return false; var it = r.CurrentShop.CardsForSale[i];
                if(it.IsSold || r.TheHero.CurrentGold < it.Price) return false;
                r.TheHero.CurrentGold-=it.Price; r.TheHero.Deck.AddCardToMasterDeck(it.Item); it.IsSold=true; return true;
            }
            public static bool PurchaseRelic(GameRun r, int i) {
                if(i<0||i>=r.CurrentShop.RelicsForSale.Count) return false; var it = r.CurrentShop.RelicsForSale[i];
                if(it.IsSold || r.TheHero.CurrentGold < it.Price) return false;
                r.TheHero.CurrentGold-=it.Price; r.TheHero.EquipRelic(it.Item); it.IsSold=true; return true;
            }
            public static void LeaveShop(GameRun r) { r.CurrentShop=null; r.CurrentState=GameState.OnMap; }
        }
        public class CombatRoomHandler : IRoomHandler {
            public void Execute(GameRun r, Room rm) {
                var conf = r.RoomConfigs[rm.Type];
                var ens = r.EnemyPool.GetEnemiesInDifficultyRange(conf.MinValue, conf.MaxValue);
                if(!ens.Any()) return;
                var picked = new List<EnemyData>();
                float budget = (conf.MinValue + conf.MaxValue)/2f;
                while(picked.Count<4 && budget>0) {
                    var cands = ens.Where(e=>e.Difficulty<=budget).ToList(); if(!cands.Any()) break;
                    var pick = cands[r.Rng.Next(cands.Count)]; picked.Add(pick); budget-=pick.Difficulty;
                }
                r.CurrentCombat = new CombatManager(r.TheHero, picked, r.Rng, r.EffectPool.GetEffect);
                r.CurrentCombat.StartCombat(); r.CurrentState=GameState.InCombat;
            }
            public static void GenerateVictoryRewards(GameRun r) {
                float diff = r.CurrentCombat.Enemies.Sum(e=>e.EnemyData.Difficulty);
                r.TheHero.CurrentGold += (int)Math.Floor(200.0 / (6.0 - diff));
                r.CardRewardChoices.Clear();
                int tier = (int)Math.Round(diff);
                if(tier<=1) for(int i=0; i<3; i++) r.CardRewardChoices.Add(r.CardPool.GetRandomCardOfRarity(Rarity.Common, r.Rng));
                else if(tier==2) { r.CardRewardChoices.Add(r.CardPool.GetRandomCardOfRarity(Rarity.Uncommon, r.Rng)); for(int i=0;i<2;i++) r.CardRewardChoices.Add(r.CardPool.GetRandomCardOfRarity(Rarity.Common, r.Rng)); }
                else if(tier==3) { r.CardRewardChoices.Add(r.CardPool.GetRandomCardOfRarity(Rarity.Rare, r.Rng)); for(int i=0;i<2;i++) r.CardRewardChoices.Add(r.CardPool.GetRandomCardUpToRarity(Rarity.Uncommon, r.Rng)); }
                else if(tier>=4) { r.CardRewardChoices.Add(r.CardPool.GetRandomCardOfRarity(Rarity.Legendary, r.Rng)); for(int i=0;i<2;i++) r.CardRewardChoices.Add(r.CardPool.GetRandomCardUpToRarity(Rarity.Rare, r.Rng)); }
                r.CardRewardChoices.RemoveAll(c=>c==null);
                Rarity rr = Rarity.Common;
                if(tier==2 && r.Rng.Next(3)==0) rr=Rarity.Uncommon;
                else if(tier==3) rr = r.Rng.Next(3)==0 ? Rarity.Rare : Rarity.Uncommon;
                else if(tier>=4) rr = r.Rng.Next(3)==0 ? Rarity.Legendary : Rarity.Rare;
                r.RelicRewardChoice = r.RelicPool.GetRandomRelicOfRarity(rr, r.Rng);
                r.CurrentState = GameState.AwaitingReward; r.CurrentCombat=null;
            }
        }
        public class BossRoomHandler : IRoomHandler {
            public void Execute(GameRun r, Room rm) {
                var boss = r.EnemyPool.GetEnemiesInDifficultyRange(5f, 10f); if(!boss.Any()) return;
                r.CurrentCombat = new CombatManager(r.TheHero, new List<EnemyData>{ boss[r.Rng.Next(boss.Count)] }, r.Rng, r.EffectPool.GetEffect);
                r.CurrentCombat.StartCombat(); r.CurrentState=GameState.InCombat;
            }
            public static void GenerateBossRewards(GameRun r) {
                r.TheHero.CurrentGold+=100; r.CardRewardChoices.Clear();
                r.CardRewardChoices.Add(r.CardPool.GetRandomCardOfRarity(Rarity.Rare, r.Rng));
                r.CardRewardChoices.Add(r.CardPool.GetRandomCardOfRarity(Rarity.Rare, r.Rng));
                r.CardRewardChoices.Add(r.CardPool.GetRandomCardOfRarity(Rarity.Uncommon, r.Rng));
                r.RelicRewardChoice = r.RelicPool.GetRandomRelicOfRarity(Rarity.Boss, r.Rng);
                r.CurrentState = GameState.AwaitingReward; r.CurrentCombat=null;
            }
        }
    }

    public class GameController
    {
        public GameRun CurrentRun { get; private set; }
        private CardPool _cp; private RelicPool _rp; private EnemyPool _ep; private EffectPool _ef; private EventPool _ev; private Dictionary<RoomType, RoomData> _rc;
        private Dictionary<RoomType, Handlers.IRoomHandler> _h;
        public GameController(CardPool cp, RelicPool rp, EnemyPool ep, EffectPool ef, EventPool ev, Dictionary<RoomType, RoomData> rc) {
            _cp=cp; _rp=rp; _ep=ep; _ef=ef; _ev=ev; _rc=rc;
            _h = new Dictionary<RoomType, Handlers.IRoomHandler> {
                { RoomType.Monster, new Handlers.CombatRoomHandler() }, { RoomType.Elite, new Handlers.CombatRoomHandler() },
                { RoomType.Boss, new Handlers.BossRoomHandler() }, { RoomType.Event, new Handlers.EventRoomHandler() },
                { RoomType.Shop, new Handlers.ShopRoomHandler() }, { RoomType.Rest, new Handlers.RestRoomHandler() }
            };
        }
        public void StartNewRun(int seed, HeroData hd) => CurrentRun = new GameRun(seed, hd, _cp, _rp, _ep, _ef, _ev, _rc);
        public bool ChooseMapNode(int id) {
            if(CurrentRun.CurrentState!=GameState.OnMap || !CurrentRun.TheMap.MoveToNode(id)) return false;
            var r = CurrentRun.TheMap.GetCurrentRoom();
            if(_h.ContainsKey(r.Type)) _h[r.Type].Execute(CurrentRun, r);
            return true;
        }
        public bool PlayCard(int hIdx, int tIdx) {
            if(CurrentRun.CurrentState!=GameState.InCombat) return false;
            Enemy t = (tIdx>=0 && tIdx < CurrentRun.CurrentCombat.Enemies.Count) ? CurrentRun.CurrentCombat.Enemies[tIdx] : null;
            if(hIdx<0 || hIdx >= CurrentRun.TheHero.Deck.Hand.Count) return false;
            var c = CurrentRun.TheHero.Deck.Hand[hIdx];
            if(CurrentRun.CurrentCombat.PlayCard(c, t)) { CheckCombat(); return true; } return false;
        }
        public void EndTurn() { if(CurrentRun.CurrentState==GameState.InCombat) { CurrentRun.CurrentCombat.EndPlayerTurn(); CheckCombat(); } }
        private void CheckCombat() {
            if(CurrentRun.CurrentCombat.State==CombatState.Victory) {
                CurrentRun.TheHero.OnCombatEnd(); 
                if(CurrentRun.TheMap.GetCurrentRoom().Type==RoomType.Boss) Handlers.BossRoomHandler.GenerateBossRewards(CurrentRun);
                else Handlers.CombatRoomHandler.GenerateVictoryRewards(CurrentRun);
            } else if (CurrentRun.CurrentCombat.State==CombatState.Defeat) CurrentRun.CurrentState=GameState.GameOver;
        }
        public void ChooseEventOption(int i) => Handlers.EventRoomHandler.ResolveChoice(CurrentRun, i);
        public void BuyShopCard(int i) => Handlers.ShopRoomHandler.PurchaseCard(CurrentRun, i);
        public void BuyShopRelic(int i) => Handlers.ShopRoomHandler.PurchaseRelic(CurrentRun, i);
        public void LeaveShop() => Handlers.ShopRoomHandler.LeaveShop(CurrentRun);
        public void ConfirmRewards(int i) {
            if(CurrentRun.CurrentState!=GameState.AwaitingReward) return;
            if(i>=0 && i<CurrentRun.CardRewardChoices.Count) CurrentRun.TheHero.Deck.AddCardToMasterDeck(CurrentRun.CardRewardChoices[i]);
            if(CurrentRun.RelicRewardChoice!=null) CurrentRun.TheHero.EquipRelic(CurrentRun.RelicRewardChoice);
            CurrentRun.CardRewardChoices.Clear(); CurrentRun.RelicRewardChoice=null;
            if(CurrentRun.TheMap.GetCurrentRoom().Type==RoomType.Boss) CurrentRun.CurrentState=GameState.GameOver;
            else CurrentRun.CurrentState=GameState.OnMap;
        }
    }
}