using Roguelike.Data;
using System;
using System.Collections.Generic;

namespace Roguelike.Logic
{
    /// <summary>
    /// Represents the player's character, holding all their run-time state.
    /// </summary>
    public class Hero : Combatant
    {
        public HeroData SourceHeroData => (HeroData)SourceData;

        public DeckManager Deck { get; }
        public int CurrentMana { get; set; }
        public int MaxMana { get; set; }
        public int CurrentGold { get; set; }
        public List<RelicData> Relics { get; } = new List<RelicData>();

        public Hero(HeroData sourceData, Random rng) : base(sourceData)
        {
            Deck = new DeckManager(rng);
            MaxMana = sourceData.StartingMana;
            CurrentMana = MaxMana;
            CurrentGold = sourceData.StartingGold;
        }

        public void StartTurn()
        {
            Block = 0;
            CurrentMana = MaxMana;

            var philosophicalEffect = ActiveEffects.FirstOrDefault(e => e.SourceData is StatusEffectData s && s.EffectType == StatusEffectType.Philosophical);
            if (philosophicalEffect != null)
            {
                CurrentMana += (philosophicalEffect.SourceData.Value * philosophicalEffect.Stacks);
            }

            if (Relics.Any(r => r.Id == "cursed_key" || r.Id == "sozu" || r.Id == "fusion_hammer" || r.Id == "busted_crown"))
            {
                CurrentMana += 1;
            }

            Deck.DrawCards(SourceHeroData.StartingHandSize);
        }
    }
}