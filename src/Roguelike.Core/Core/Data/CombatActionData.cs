namespace Roguelike.Data
{


    public class CombatActionData
    {


        public ActionType Type { get; set; }


        public int Value { get; set; }


        public TargetType Target { get; set; }


        public string EffectId { get; set; }

        public CombatActionData() { }

        public CombatActionData(ActionType type, int value, TargetType target, string effectId = null)
        {
            Type = type;
            Value = value;
            Target = target;
            EffectId = effectId;
        }
    }
}
