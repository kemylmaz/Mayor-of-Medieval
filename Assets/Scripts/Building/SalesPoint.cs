using System.Collections.Generic;
using MayorOfMedieval.Core;
using MayorOfMedieval.Economy;
using UnityEngine;

namespace MayorOfMedieval.Building
{
    /// <summary>
    /// A shop front. The Lord (or a Carrier worker) piles goods onto its shelves, customers
    /// then serve themselves and drop the payment into the till. Gold sits in the till until
    /// somebody picks it up — either the player walking past, or a Treasury gold collector.
    /// </summary>
    public class SalesPoint : MonoBehaviour
    {
        [Header("Shelves")]
        [Tooltip("One stockpile per good this shop sells.")]
        [SerializeField] private List<Stockpile> shelves = new List<Stockpile>();

        [Header("Till")]
        [SerializeField] private float collectRadius = 2.4f;
        [SerializeField] private Transform coinAnchor;

        public int PendingGold { get; private set; }

        private static readonly List<SalesPoint> all = new List<SalesPoint>();
        public static IReadOnlyList<SalesPoint> All => all;

        private readonly List<GameObject> coinVisuals = new List<GameObject>();
        private static Material sharedCoinMaterial;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => all.Clear();

        private void Awake()
        {
            if (coinAnchor == null)
            {
                GameObject anchor = new GameObject("TillAnchor");
                anchor.transform.SetParent(transform, false);
                anchor.transform.localPosition = new Vector3(0f, 0f, 1.4f);
                coinAnchor = anchor.transform;
            }
        }

        private void OnEnable() => all.Add(this);
        private void OnDisable() => all.Remove(this);

        private void Update()
        {
            if (PendingGold <= 0) return;

            Transform player = PlayerRef.Root;
            if (player == null) return;
            if (Vector3.Distance(player.position, coinAnchor.position) > collectRadius) return;

            CollectInto(null);
        }

        // ------------------------------------------------------------------ stock

        public bool Sells(ResourceType type) => FindShelf(type) != null;

        public int StockOf(ResourceType type)
        {
            Stockpile shelf = FindShelf(type);
            return shelf != null ? shelf.Amount : 0;
        }

        /// <summary>Customer takes one unit off the shelf and pays for it.</summary>
        public bool TryBuyOne(ResourceType type, out int paid)
        {
            paid = 0;
            Stockpile shelf = FindShelf(type);
            if (shelf == null || shelf.IsEmpty) return false;

            shelf.Remove(1);
            paid = GameConfig.SellPrice(type);
            AddGold(paid);
            Core.AudioManager.PlaySafe(Core.Sfx.Sale);
            return true;
        }

        /// <summary>Used by Carrier workers to restock a shelf directly.</summary>
        public bool TryStock(ResourceType type, int units = 1)
        {
            Stockpile shelf = FindShelf(type);
            if (shelf == null || shelf.IsFull) return false;
            return shelf.Add(units) > 0;
        }

        public bool ShelfHasRoom(ResourceType type)
        {
            Stockpile shelf = FindShelf(type);
            return shelf != null && !shelf.IsFull;
        }

        /// <summary>Goods this shop can still take more of, used by Carrier target picking.</summary>
        public IReadOnlyList<Stockpile> Shelves => shelves;

        private Stockpile FindShelf(ResourceType type)
        {
            for (int i = 0; i < shelves.Count; i++)
            {
                if (shelves[i] != null && shelves[i].ResourceType == type) return shelves[i];
            }
            return null;
        }

        // ------------------------------------------------------------------- till

        /// <summary>Restores a saved till balance without paying it out.</summary>
        public void SetPendingGold(int amount)
        {
            PendingGold = Mathf.Max(0, amount);
            RefreshCoins();
        }

        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            PendingGold += amount;
            RefreshCoins();
        }

        /// <summary>
        /// Empties the till. Pass a world position to show the pickup popup there
        /// (a gold-collector worker), or null to use this shop's own anchor (the player).
        /// </summary>
        public int CollectInto(Transform collector)
        {
            if (PendingGold <= 0) return 0;

            int amount = PendingGold;
            PendingGold = 0;
            RefreshCoins();

            if (ResourceManager.Instance != null) ResourceManager.Instance.AddResource(ResourceType.Gold, amount);
            Core.AudioManager.PlaySafe(Core.Sfx.Coins);
            if (Core.DailyQuests.Instance != null)
                Core.DailyQuests.Instance.Report(Core.DailyQuests.Track.CollectGold, amount);

            Vector3 popupAt = collector != null ? collector.position : coinAnchor.position;
            UI.FloatingText.Spawn(popupAt + Vector3.up * 2.2f, "+" + amount, new Color(1f, 0.85f, 0.2f));
            return amount;
        }

        private void RefreshCoins()
        {
            // One coin per 25 gold, capped so a rich till doesn't turn into a tower.
            int wanted = Mathf.Clamp(PendingGold / 25, 0, 12);

            while (coinVisuals.Count > wanted)
            {
                GameObject last = coinVisuals[coinVisuals.Count - 1];
                coinVisuals.RemoveAt(coinVisuals.Count - 1);
                if (last != null) Destroy(last);
            }

            while (coinVisuals.Count < wanted)
            {
                coinVisuals.Add(CreateCoin(coinVisuals.Count));
            }
        }

        private GameObject CreateCoin(int index)
        {
            GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coin.name = "Coin";
            Collider col = coin.GetComponent<Collider>();
            if (col != null) Destroy(col);

            coin.transform.SetParent(coinAnchor, false);
            coin.transform.localScale = new Vector3(0.28f, 0.04f, 0.28f);

            int row = index / 4;
            int column = index % 4;
            coin.transform.localPosition = new Vector3(column * 0.32f - 0.48f, 0.05f + row * 0.09f, 0f);

            Renderer renderer = coin.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (sharedCoinMaterial == null)
                {
                    Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                    sharedCoinMaterial = new Material(shader);
                    Color gold = GameConfig.ColorOf(ResourceType.Gold);
                    sharedCoinMaterial.SetColor("_BaseColor", gold);
                    sharedCoinMaterial.SetColor("_Color", gold);
                }
                renderer.sharedMaterial = sharedCoinMaterial;
            }
            return coin;
        }

        private void OnDrawGizmosSelected()
        {
            Transform anchor = coinAnchor != null ? coinAnchor : transform;
            Gizmos.color = new Color(1f, 0.85f, 0.2f);
            Gizmos.DrawWireSphere(anchor.position, collectRadius);
        }
    }
}
