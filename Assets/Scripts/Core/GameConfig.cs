using MayorOfMedieval.Economy;
using UnityEngine;

namespace MayorOfMedieval.Core
{
    /// <summary>Every building the player can unlock, in progression order.</summary>
    public enum BuildingKind
    {
        Market,        // Pazar          - sells Wood/Stone off its shelf
        LumberCamp,    // Oduncu Kulubesi- harvester + carrier to the Market
        Quarry,        // Tas Ocagi      - harvester + carrier to the Market
        Farm,          // Ciftlik        - hunts meat, sells it
        CropField,     // Tarla          - auto grain (needs Well)
        Well,          // Kuyu           - water source, enables the field
        Mill,          // Degirmen       - Grain + Water -> Bread
        Treasury,      // Hazine         - worker sweeps gold from every shop
        Blacksmith,    // Demirci        - Stone -> Sword
        Barracks,      // Kisla          - Sword -> Soldier (no customers)
        Inn,           // Han            - Grain + Water + Bread -> Beer
        VillageSquare, // Koy Meydani    - decoration
        Church         // Kilise         - decoration
    }

    /// <summary>What a hired villager actually does for a living.</summary>
    public enum WorkerRole
    {
        Harvester,     // world node -> home stockpile
        Carrier,       // home stockpile -> a shop shelf
        Producer,      // fetches recipe inputs from other buildings
        GoldCollector  // sweeps accumulated gold from every shop
    }

    /// <summary>
    /// Single source of truth for economy tuning. Everything the designer would want to
    /// rebalance lives here rather than being scattered across prefabs.
    /// </summary>
    public static class GameConfig
    {
        public const int StartingGold = 100;

        public const int PlayerCarryCapacity = 8;
        public const int WorkerCarryCapacity = 4;

        // --- Hiring -------------------------------------------------------------
        // Tuned for a playable-ad pace: a hire is a quick decision, not a long save-up.
        public const int BaseWorkerCost = 70;
        public const int WorkerCostIncrement = 60;

        public static int WorkerCostFor(int alreadyHired) =>
            BaseWorkerCost + alreadyHired * WorkerCostIncrement;

        // --- Buildings ----------------------------------------------------------
        /// <summary>Gold price of each build pad. Doubles as the gold threshold that reveals it.</summary>
        public static int CostOf(BuildingKind kind)
        {
            switch (kind)
            {
                // Whole ladder is deliberately shallow so a 60-second session can reach
                // the late buildings — a playable has to show its whole arc, fast.
                case BuildingKind.Market: return 50;
                case BuildingKind.LumberCamp: return 100;
                case BuildingKind.Quarry: return 150;
                case BuildingKind.Farm: return 250;
                case BuildingKind.CropField: return 350;
                case BuildingKind.Well: return 100;
                case BuildingKind.Mill: return 500;
                case BuildingKind.Treasury: return 600;
                case BuildingKind.Blacksmith: return 750;
                case BuildingKind.Barracks: return 900;
                case BuildingKind.Inn: return 1100;
                case BuildingKind.VillageSquare: return 1300;
                case BuildingKind.Church: return 1500;
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
                case BuildingKind.Treasury: return "Hazine";
                case BuildingKind.Blacksmith: return "Demirci";
                case BuildingKind.Barracks: return "Kisla";
                case BuildingKind.Inn: return "Han";
                case BuildingKind.VillageSquare: return "Koy Meydani";
                case BuildingKind.Church: return "Kilise";
                default: return kind.ToString();
            }
        }

        // --- Goods --------------------------------------------------------------
        /// <summary>Gold a customer pays per unit. Each processing tier is clearly richer.</summary>
        public static int SellPrice(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Wood: return 14;
                case ResourceType.Stone: return 18;
                case ResourceType.Grain: return 28;
                case ResourceType.Meat: return 38;
                case ResourceType.Bread: return 60;
                case ResourceType.Beer: return 85;
                case ResourceType.Sword: return 120;
                default: return 0;
            }
        }

        /// <summary>Gold earned for defeating one training dummy at the Barracks.</summary>
        public const int EnemyBounty = 45;

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
                case ResourceType.Water: return new Color(0.30f, 0.62f, 0.88f);
                case ResourceType.Sword: return new Color(0.72f, 0.75f, 0.82f);
                case ResourceType.Beer: return new Color(0.85f, 0.60f, 0.18f);
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
                case ResourceType.Water: return "Su";
                case ResourceType.Sword: return "Kilic";
                case ResourceType.Beer: return "Bira";
                default: return "Altin";
            }
        }
    }
}
