using Roguelike.Data;
using Roguelike.Core.Map;
using System.Linq;

namespace Roguelike.Core.Handlers
{
    public class EventRoomHandler : IRoomHandler
    {


        public void Execute(GameRun run, Room room)
        {
            var allEvents = run.EventPool.EventsById.Values.ToList();
            if (!allEvents.Any())
            {
                return;
            }

            int index = run.Rng.Next(allEvents.Count);
            var selectedEvent = allEvents[index];
            run.CurrentEvent = selectedEvent;
            run.CurrentState = GameState.InEvent;
        }


        public static void ResolveChoice(GameRun run, int choiceIndex)
        {
            if (run.CurrentState != GameState.InEvent || run.CurrentEvent == null) return;
            if (choiceIndex < 0 || choiceIndex >= run.CurrentEvent.Choices.Count) return;

            var chosenOption = run.CurrentEvent.Choices[choiceIndex];

            foreach (var effect in chosenOption.Effects)
            {
                ApplyEffect(run, effect);
            }

            run.CurrentEvent = null;
            run.CurrentState = GameState.OnMap;
        }

        private static void ApplyEffect(GameRun run, EventEffect effect)
        {
            switch (effect.Type)
            {
                case EventEffectType.GainGold:
                    run.TheHero.CurrentGold += effect.Value;
                    break;
                case EventEffectType.LoseGold:
                    run.TheHero.CurrentGold = System.Math.Max(0, run.TheHero.CurrentGold - effect.Value);
                    break;
                case EventEffectType.LoseHP:
                    run.TheHero.TakePiercingDamage(effect.Value);
                    break;
                case EventEffectType.HealHP:
                    run.TheHero.Heal(effect.Value);
                    break;
                case EventEffectType.GainCard:

                    var card = run.CardPool.GetCard(effect.Parameter);
                    if (card != null) run.TheHero.Deck.AddCardToMasterDeck(card);
                    break;
                case EventEffectType.RemoveCard:

                    if (run.TheHero.Deck.MasterDeck.Any())
                    {
                        run.TheHero.Deck.RemoveCardFromMasterDeck(run.TheHero.Deck.MasterDeck[0]);
                    }
                    break;
                case EventEffectType.GainRelic:

                    var relic = run.RelicPool.GetRelic(effect.Parameter);
                    if (relic != null) run.TheHero.Relics.Add(relic);
                    break;
                case EventEffectType.Quit:
                    break;
            }
        }
    }
}
