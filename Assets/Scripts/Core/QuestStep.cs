using System;
using MayorOfMedieval.Building;
using MayorOfMedieval.Economy;
using UnityEngine;

namespace MayorOfMedieval.Core
{
    /// <summary>What the player has to actually *do* for a quest line entry.</summary>
    public enum QuestGoal
    {
        BuildBuilding,   // stand on a build pad until it is paid off
        GatherResource,  // carry N of a good on your head
        StockShop,       // put N of a good onto a shop shelf
        EarnGold,        // reach a gold total
        HireWorker       // staff a building
    }

    /// <summary>
    /// One rung of the tutorial ladder. Kept as plain data so the whole flow can be read
    /// (and reordered) in one place instead of being spread across manager code.
    /// </summary>
    [Serializable]
    public class QuestStep
    {
        public QuestGoal goal;
        public string text;

        [Tooltip("Building this step is about (build target, shop to stock, place to hire).")]
        public BuildingKind building;
        public bool usesBuilding = true;

        [Tooltip("Good to gather or stock.")]
        public ResourceType resource = ResourceType.Wood;
        public int amount = 1;

        /// <summary>True once the player has satisfied this step.</summary>
        public bool IsSatisfied(GameProgression progression)
        {
            switch (goal)
            {
                case QuestGoal.BuildBuilding:
                    return progression.IsBuilt(building);

                case QuestGoal.GatherResource:
                {
                    Character.CarrySystem carry = PlayerRef.Carry;
                    return carry != null && carry.CountOf(resource) >= amount;
                }

                case QuestGoal.StockShop:
                {
                    SalesPoint shop = progression.ShopFor(building);
                    return shop != null && shop.StockOf(resource) >= amount;
                }

                case QuestGoal.EarnGold:
                    return ResourceManager.Instance != null && ResourceManager.Instance.PeakGold >= amount;

                case QuestGoal.HireWorker:
                {
                    WorkerStation station = progression.StationFor(building);
                    return station != null && station.WorkerCount >= amount;
                }
            }
            return false;
        }

        /// <summary>Where the arrow should point for this step, or null when there is no obvious spot.</summary>
        public Transform TargetFor(GameProgression progression)
        {
            switch (goal)
            {
                case QuestGoal.BuildBuilding:
                {
                    BuildPad pad = progression.PadFor(building);
                    return pad != null && pad.IsRevealed ? pad.transform : null;
                }

                case QuestGoal.GatherResource:
                {
                    Environment.HarvestNode node = progression.NearestNode(resource);
                    return node != null ? node.transform : null;
                }

                case QuestGoal.StockShop:
                case QuestGoal.HireWorker:
                {
                    GameObject b = progression.BuildingObjectFor(building);
                    return b != null ? b.transform : null;
                }
            }
            return null;
        }
    }
}
