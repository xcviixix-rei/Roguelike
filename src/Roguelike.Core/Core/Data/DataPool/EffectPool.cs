using System.Collections.Generic;

namespace Roguelike.Data
{
    /// <summary>
    /// A data container that holds all possible EffectData templates for the game.
    /// </summary>
    public class EffectPool
    {
        public Dictionary<string, EffectData> EffectsById { get; set; } = new Dictionary<string, EffectData>();

        public EffectData GetEffect(string id)
        {
            EffectsById.TryGetValue(id, out var effect);
            return effect;
        }
    }
}
