using System;
using System.Collections.Generic;
using UnityEngine;

namespace MayorOfMedieval.Economy
{
    public enum ResourceType
    {
        Gold = 0,
        Wood = 1,
        Stone = 2,
        Seed = 3,
        Meat = 4,
        Grain = 5,
        Bread = 6,
        Water = 7,
        Sword = 8,
        Beer = 9
    }

    /// <summary>
    /// Global wallet. Only Gold is a true global counter — physical goods (wood, stone,
    /// meat, grain, bread) live in the player's carry stack or in a building Stockpile,
    /// which is why the HUD only ever shows gold.
    /// </summary>
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        [SerializeField] private int startingGold = Core.GameConfig.StartingGold;

        private readonly Dictionary<ResourceType, int> resources = new Dictionary<ResourceType, int>();

        /// <summary>(type, oldAmount, newAmount)</summary>
        public event Action<ResourceType, int, int> OnResourceChanged;

        /// <summary>Highest gold total reached this run. Drives build-pad unlocks so a
        /// pad never disappears again after the player spends back below its threshold.</summary>
        public int PeakGold { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            resources.Clear();
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                resources[type] = 0;
            }
            resources[ResourceType.Gold] = startingGold;
            PeakGold = startingGold;
        }

        public int Gold => GetResource(ResourceType.Gold);

        /// <summary>Restores a saved wallet. PeakGold is set directly so build pads that
        /// were already unlocked stay unlocked.</summary>
        public void RestoreGold(int gold, int peak)
        {
            int old = GetResource(ResourceType.Gold);
            resources[ResourceType.Gold] = Mathf.Max(0, gold);
            PeakGold = Mathf.Max(peak, resources[ResourceType.Gold]);
            OnResourceChanged?.Invoke(ResourceType.Gold, old, resources[ResourceType.Gold]);
        }

        public int GetResource(ResourceType type)
        {
            int value;
            return resources.TryGetValue(type, out value) ? value : 0;
        }

        public bool HasEnoughResource(ResourceType type, int amount) => GetResource(type) >= amount;

        public void AddResource(ResourceType type, int amount)
        {
            if (amount <= 0) return;
            int oldAmount = GetResource(type);
            resources[type] = oldAmount + amount;

            if (type == ResourceType.Gold && resources[type] > PeakGold) PeakGold = resources[type];

            OnResourceChanged?.Invoke(type, oldAmount, resources[type]);
        }

        public bool SpendResource(ResourceType type, int amount)
        {
            if (amount <= 0) return true;
            if (!HasEnoughResource(type, amount)) return false;

            int oldAmount = resources[type];
            resources[type] = oldAmount - amount;
            OnResourceChanged?.Invoke(type, oldAmount, resources[type]);
            return true;
        }
    }
}
