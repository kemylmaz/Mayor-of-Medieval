using MayorOfMedieval.Economy;
using UnityEngine;

namespace MayorOfMedieval.Core
{
    /// <summary>Every building the player can unlock, in progression order.</summary>
    public enum BuildingKind
    {
        Market,      // Pazar        - opens Wood/Stone orders
        LumberCamp,  // Oduncu Kulubesi - workers auto-chop wood
        Quarry,      // Tas Ocagi    - workers auto-mine stone
        Farm,        // Ciftlik      - opens Meat orders, workers auto-hunt
        CropField,   // Tarla        - opens Grain orders, needs Well to run
        Well,        // Kuyu         - enables CropField production
        Mill         // Degirmen     - converts Grain into Bread
    }

    /// <summary>
    /// Single source of truth for economy tuning. Everything the designer would want to
    /// rebalance lives here rather than being scattered across prefabs.
    /// </summary>
    public static class GameConfig
    {
        public const int StartingGold = 100;

        public const int WorkerCost = 100;
        public const int MaxWorkersPerStation = 3;

        public const int PlayerCarryCapacity = 8;
        public const int WorkerCarryCapacity = 4;

        /// <summary>Gold price of each build pad. Doubles as the gold threshold that reveals it.</summary>
        public static int CostOf(BuildingKind kind)
        {
            switch (kind)
            {
                case BuildingKind.Market: return 100;
                case BuildingKind.LumberCamp: return 150;
                case BuildingKind.Quarry: return 200;
                case BuildingKind.Farm: return 300;
                case BuildingKind.CropField: return 400;
                case BuildingKind.Well: return 100;
                case BuildingKind.Mill: return 1000;
                default: return 100;
            }
        }

        public static string DisplayName(BuildingKind kind)
        {
            switch (kind)
            {
                case BuildingKind.Market: return "Pazar";
                case BuildingKind.LumberCamp: return "Oduncu Kulubesi";
                case BuildingKind.Quarry: return "Tas Ocagi";
                case BuildingKind.Farm: return "Ciftlik";
                case BuildingKind.CropField: return "Tarla";
                case BuildingKind.Well: return "Kuyu";
                case BuildingKind.Mill: return "Degirmen";
                default: return kind.ToString();
            }
        }

        /// <summary>Gold a customer pays per unit delivered. Each tier is clearly richer.</summary>
        public static int SellPrice(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Wood: return 8;
                case ResourceType.Stone: return 10;
                case ResourceType.Grain: return 18;
                case ResourceType.Meat: return 25;
                case ResourceType.Bread: return 40;
                default: return 0;
            }
        }

        public static Color ColorOf(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Gold: return new Color(1f, 0.82f, 0.15f);
                case ResourceType.Wood: return new Color(0.55f, 0.35f, 0.16f);
                case ResourceType.Stone: return new Color(0.66f, 0.66f, 0.70f);
                case ResourceType.Seed: return new Color(0.35f, 0.80f, 0.30f);
                case ResourceType.Meat: return new Color(0.80f, 0.28f, 0.26f);
                case ResourceType.Grain: return new Color(0.90f, 0.76f, 0.30f);
                case ResourceType.Bread: return new Color(0.78f, 0.55f, 0.25f);
                default: return Color.white;
            }
        }

        public static string DisplayName(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Wood: return "Odun";
                case ResourceType.Stone: return "Tas";
                case ResourceType.Seed: return "Tohum";
                case ResourceType.Meat: return "Et";
                case ResourceType.Grain: return "Tahil";
                case ResourceType.Bread: return "Ekmek";
                default: return "Altin";
            }
        }
    }
}
