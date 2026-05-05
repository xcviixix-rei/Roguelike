namespace Roguelike.Data
{


    public abstract class CombatantData
    {


        public string Id { get; set; }


        public string Name { get; set; }


        public int StartingHealth { get; set; }


        public int StartingStrength { get; set; } = 0;
    }
}
