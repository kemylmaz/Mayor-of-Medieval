using System.Collections.Generic;
using MayorOfMedieval.Character;
using MayorOfMedieval.Core;
using MayorOfMedieval.Economy;
using UnityEngine;

namespace MayorOfMedieval.Building
{
    /// <summary>
    /// A pile of goods sitting next to a building. Workers drop harvested units in,
    /// the Lord walks over to scoop them out. Transfers happen one unit at a time on a
    /// short interval so it reads as a physical flow rather than an instant swap.
    /// </summary>
    public class Stockpile : MonoBehaviour
    {
        [Header("Contents")]
        [SerializeField] private ResourceType resourceType = ResourceType.Wood;
        [SerializeField] private int capacity = 20;
        [SerializeField] private int startingAmount;

        [Header("Player Interaction")]
        [SerializeField] private bool playerCanWithdraw = true;
        [SerializeField] private bool playerCanDeposit;
        [SerializeField] private float interactRadius = 2.2f;
        [SerializeField] private float transferInterval = 0.12f;

        [Header("Logistics")]
        [Tooltip("Workers are allowed to collect this good from here (a supply depot).")]
        [SerializeField] private bool isSupplySource;
        [Tooltip("Units a shop Carrier must leave behind, so downstream workshops always " +
                 "get fed before goods are sent off to be sold.")]
        [SerializeField] private int reserveForProduction;

        [Header("Visual")]
        [SerializeField] private Transform stackRoot;
        [SerializeField] private int itemsPerRow = 4;
        [SerializeField] private Vector3 itemSize = new Vector3(0.34f, 0.22f, 0.34f);
        [SerializeField] private float itemSpacing = 0.38f;

        public ResourceType ResourceType => resourceType;
        public int Amount { get; private set; }
        public int Capacity => capacity;
        public bool IsFull => Amount >= capacity;
        public bool IsEmpty => Amount <= 0;
        public bool IsSupplySource => isSupplySource;

        /// <summary>Units a Carrier may take right now (everything above the reserve).</summary>
        public int AvailableToCarry => Mathf.Max(0, Amount - reserveForProduction);

        private static readonly List<Stockpile> all = new List<Stockpile>();
        public static IReadOnlyList<Stockpile> All => all;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => all.Clear();

        /// <summary>Nearest depot that currently holds the requested good.</summary>
        public static Stockpile FindSource(ResourceType type, Vector3 from)
        {
            Stockpile best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < all.Count; i++)
            {
                Stockpile pile = all[i];
                if (pile == null || !pile.isSupplySource) continue;
                if (pile.resourceType != type || pile.IsEmpty) continue;

                float distance = Vector3.SqrMagnitude(pile.transform.position - from);
                if (distance >= bestDistance) continue;

                bestDistance = distance;
                best = pile;
            }
            return best;
        }

        private void OnEnable() => all.Add(this);
        private void OnDisable() => all.Remove(this);

        private readonly List<GameObject> visuals = new List<GameObject>();
        private static Material sharedMaterial;
        private float transferTimer;

        private void Awake()
        {
            if (stackRoot == null)
            {
                GameObject root = new GameObject("StackRoot");
                root.transform.SetParent(transform, false);
                stackRoot = root.transform;
            }
            Amount = Mathf.Clamp(startingAmount, 0, capacity);
            RebuildVisual();
        }

        private void Update()
        {
            if (!playerCanWithdraw && !playerCanDeposit) return;

            transferTimer -= Time.deltaTime;
            if (transferTimer > 0f) return;

            CarrySystem player = PlayerRef.Carry;
            if (player == null) return;
            if (Vector3.Distance(player.transform.position, transform.position) > interactRadius) return;

            transferTimer = transferInterval;

            // Depositing takes priority: if the Lord is hauling this good, drop it off.
            if (playerCanDeposit && !IsFull && player.CountOf(resourceType) > 0)
            {
                if (player.TryRemove(resourceType)) Add(1);
                return;
            }

            if (playerCanWithdraw && !IsEmpty && !player.IsFull)
            {
                if (player.TryAdd(resourceType)) Remove(1);
            }
        }

        public int Add(int units)
        {
            int accepted = Mathf.Clamp(units, 0, capacity - Amount);
            if (accepted <= 0) return 0;
            Amount += accepted;
            RebuildVisual();
            return accepted;
        }

        public int Remove(int units)
        {
            int taken = Mathf.Clamp(units, 0, Amount);
            if (taken <= 0) return 0;
            Amount -= taken;
            RebuildVisual();
            return taken;
        }

        private void RebuildVisual()
        {
            while (visuals.Count > Amount)
            {
                GameObject last = visuals[visuals.Count - 1];
                visuals.RemoveAt(visuals.Count - 1);
                if (last != null) Destroy(last);
            }

            while (visuals.Count < Amount)
            {
                visuals.Add(CreateItem(visuals.Count));
            }
        }

        private GameObject CreateItem(int index)
        {
            GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
            item.name = "Stock_" + resourceType;

            Collider col = item.GetComponent<Collider>();
            if (col != null) Destroy(col);

            item.transform.SetParent(stackRoot, false);
            item.transform.localScale = itemSize;

            int layer = index / (itemsPerRow * itemsPerRow);
            int withinLayer = index % (itemsPerRow * itemsPerRow);
            int row = withinLayer / itemsPerRow;
            int col2 = withinLayer % itemsPerRow;
            float offset = (itemsPerRow - 1) * itemSpacing * 0.5f;

            item.transform.localPosition = new Vector3(
                col2 * itemSpacing - offset,
                layer * itemSize.y * 1.05f + itemSize.y * 0.5f,
                row * itemSpacing - offset);

            Renderer renderer = item.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (sharedMaterial == null)
                {
                    Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                    sharedMaterial = new Material(shader);
                }
                renderer.sharedMaterial = sharedMaterial;
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                Color color = GameConfig.ColorOf(resourceType);
                block.SetColor("_BaseColor", color);
                block.SetColor("_Color", color);
                renderer.SetPropertyBlock(block);
            }

            return item;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 1f, 0.3f, 0.7f);
            Gizmos.DrawWireSphere(transform.position, interactRadius);
        }
    }

    /// <summary>Cached handle to the Lord so systems don't scan the scene every frame.</summary>
    public static class PlayerRef
    {
        private static CarrySystem carry;
        private static Transform root;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            carry = null;
            root = null;
        }

        public static CarrySystem Carry
        {
            get
            {
                if (carry == null) Resolve();
                return carry;
            }
        }

        public static Transform Root
        {
            get
            {
                if (root == null) Resolve();
                return root;
            }
        }

        private static void Resolve()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;
            root = player.transform;
            carry = player.GetComponent<CarrySystem>();
        }
    }
}
