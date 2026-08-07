using System;
using System.Collections.Generic;
using MayorOfMedieval.Building;
using MayorOfMedieval.Economy;
using MayorOfMedieval.Environment;
using UnityEngine;

namespace MayorOfMedieval.Core
{
    /// <summary>
    /// Drives the unlock ladder and the tutorial. Build pads reveal once the player's gold
    /// has ever reached their price; the quest line on top of that teaches the actual loop
    /// (chop -> stock -> sell -> reinvest) instead of only naming the next building.
    /// </summary>
    public class GameProgression : MonoBehaviour
    {
        public static GameProgression Instance { get; private set; }

        [Header("Quest line")]
        [SerializeField] private List<QuestStep> steps = new List<QuestStep>();

        /// <summary>(questText, progress 0..1)</summary>
        public event Action<string, float> OnQuestChanged;
        public event Action<BuildingKind> OnBuildingConstructed;

        /// <summary>Raised whenever a customer order is fully delivered.</summary>
        public static event Action<ResourceType> OnOrderCompleted;

        private readonly HashSet<BuildingKind> built = new HashSet<BuildingKind>();
        private readonly Dictionary<BuildingKind, BuildPad> pads = new Dictionary<BuildingKind, BuildPad>();

        private int stepIndex;
        private string lastText = "";
        private float lastProgress = -1f;
        private float retargetTimer;

        public Transform CurrentTarget { get; private set; }
        public string CurrentQuestText { get; private set; } = "";
        public float Progress => steps.Count == 0 ? 0f : (float)stepIndex / steps.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            OnOrderCompleted = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            if (steps.Count == 0) BuildDefaultQuestLine();
        }

        private void Update()
        {
            RefreshPads();
            AdvanceQuest();
        }

        // ------------------------------------------------------------- quest line

        /// <summary>
        /// The teaching order matters more than the building order. The player is walked
        /// through one full earn cycle by hand before automation is ever offered, so they
        /// understand what a worker is doing for them.
        /// </summary>
        private void BuildDefaultQuestLine()
        {
            Add(QuestGoal.BuildBuilding, "Pazari kur!", BuildingKind.Market);

            // One complete manual loop: chop, stock the shelf, watch it sell.
            Add(QuestGoal.GatherResource, "Baltayi kap: 5 odun kes!", res: ResourceType.Wood, amount: 5);
            Add(QuestGoal.StockShop, "Odunlari Pazar'in rafina birak!", BuildingKind.Market,
                ResourceType.Wood, 3);
            Add(QuestGoal.EarnGold, "Musteriler alsin, 150 altin biriktir!", amount: 150);

            // Now automation makes sense.
            Add(QuestGoal.BuildBuilding, "Oduncu Kulubesi kur!", BuildingKind.LumberCamp);
            Add(QuestGoal.HireWorker, "Oduncuya bir isci al (senin yerine kessin)!",
                BuildingKind.LumberCamp, amount: 1);
            Add(QuestGoal.HireWorker, "Ikinci isci tasiyici olur: Pazar'a tasisin!",
                BuildingKind.LumberCamp, amount: 2);

            Add(QuestGoal.GatherResource, "Simdi tas kir: 5 tas topla!", res: ResourceType.Stone, amount: 5);
            Add(QuestGoal.BuildBuilding, "Tas Ocagi kur!", BuildingKind.Quarry);

            Add(QuestGoal.BuildBuilding, "Ciftlik kur (et daha cok kazandirir)!", BuildingKind.Farm);
            Add(QuestGoal.GatherResource, "Hayvan avla: 3 et topla!", res: ResourceType.Meat, amount: 3);

            Add(QuestGoal.BuildBuilding, "Tarla ek!", BuildingKind.CropField);
            Add(QuestGoal.BuildBuilding, "Kuyu kaz (tarla susuz calismaz)!", BuildingKind.Well);

            Add(QuestGoal.BuildBuilding, "Degirmen kur: tahil + su = ekmek!", BuildingKind.Mill);
            Add(QuestGoal.BuildBuilding, "Hazine kur (altini otomatik toplasin)!", BuildingKind.Treasury);
            Add(QuestGoal.BuildBuilding, "Demirci ac: tastan kilic!", BuildingKind.Blacksmith);
            Add(QuestGoal.BuildBuilding, "Kisla kur: kiliclar asker olsun!", BuildingKind.Barracks);
            Add(QuestGoal.BuildBuilding, "Han kur: tahil + su + ekmek = bira!", BuildingKind.Inn);
            Add(QuestGoal.BuildBuilding, "Koy meydanini yap!", BuildingKind.VillageSquare);
            Add(QuestGoal.BuildBuilding, "Kiliseyi yap!", BuildingKind.Church);
        }

        private void Add(QuestGoal goal, string text, BuildingKind building = BuildingKind.Market,
            ResourceType res = ResourceType.Wood, int amount = 1)
        {
            steps.Add(new QuestStep
            {
                goal = goal,
                text = text,
                building = building,
                usesBuilding = goal != QuestGoal.GatherResource && goal != QuestGoal.EarnGold,
                resource = res,
                amount = amount
            });
        }

        private void AdvanceQuest()
        {
            // Skip past anything the player already did (they can run ahead of the tutorial).
            while (stepIndex < steps.Count && steps[stepIndex].IsSatisfied(this)) stepIndex++;

            if (stepIndex >= steps.Count)
            {
                Publish("Koy tamam! Kasabaya hukmediyorsun.", 1f, null);
                return;
            }

            QuestStep step = steps[stepIndex];

            // Re-resolving the arrow target every frame is wasteful; a few times a second
            // is plenty and keeps the nearest-tree lookup cheap.
            retargetTimer -= Time.deltaTime;
            if (retargetTimer <= 0f || CurrentTarget == null)
            {
                retargetTimer = 0.35f;
                CurrentTarget = step.TargetFor(this);
            }

            Publish(step.text, Progress, CurrentTarget);
        }

        private void Publish(string text, float progress, Transform target)
        {
            CurrentTarget = target;
            if (lastText == text && Mathf.Approximately(lastProgress, progress)) return;

            lastText = text;
            lastProgress = progress;
            CurrentQuestText = text;
            OnQuestChanged?.Invoke(text, progress);
        }

        // ------------------------------------------------------------------- pads

        public void RegisterPad(BuildingKind kind, BuildPad pad) => pads[kind] = pad;

        public bool IsBuilt(BuildingKind kind) => built.Contains(kind);

        /// <summary>
        /// Whether a customer may ask for this good yet. Wood and stone can be gathered by
        /// hand from the first second, but everything else needs its production building
        /// standing — otherwise the Market would take bread orders the player has no way
        /// to fill, which reads as the game being broken.
        /// </summary>
        public bool IsGoodUnlocked(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Meat: return IsBuilt(BuildingKind.Farm);
                case ResourceType.Grain: return IsBuilt(BuildingKind.CropField);
                case ResourceType.Bread: return IsBuilt(BuildingKind.Mill);
                case ResourceType.Sword: return IsBuilt(BuildingKind.Blacksmith);
                case ResourceType.Beer: return IsBuilt(BuildingKind.Inn);
                default: return true;
            }
        }

        public void NotifyBuilt(BuildingKind kind)
        {
            if (!built.Add(kind)) return;
            OnBuildingConstructed?.Invoke(kind);
        }

        internal static void NotifyOrderCompleted(ResourceType type) => OnOrderCompleted?.Invoke(type);

        /// <summary>A pad is offered once the player has ever been able to afford it.</summary>
        private void RefreshPads()
        {
            int peak = ResourceManager.Instance != null ? ResourceManager.Instance.PeakGold : 0;

            foreach (KeyValuePair<BuildingKind, BuildPad> entry in pads)
            {
                BuildPad pad = entry.Value;
                if (pad == null || pad.IsBuilt) continue;

                bool affordableOnce = peak >= GameConfig.CostOf(entry.Key);
                bool prerequisiteMet = PrerequisiteMet(entry.Key);
                pad.SetRevealed(affordableOnce && prerequisiteMet);
            }
        }

        private bool PrerequisiteMet(BuildingKind kind)
        {
            switch (kind)
            {
                case BuildingKind.Well: return built.Contains(BuildingKind.CropField);
                case BuildingKind.Barracks: return built.Contains(BuildingKind.Blacksmith);
                case BuildingKind.Inn: return built.Contains(BuildingKind.Mill);
                default: return true;
            }
        }

        // -------------------------------------------------------------- lookups

        public BuildPad PadFor(BuildingKind kind)
        {
            BuildPad pad;
            return pads.TryGetValue(kind, out pad) ? pad : null;
        }

        public GameObject BuildingObjectFor(BuildingKind kind)
        {
            BuildPad pad = PadFor(kind);
            return pad != null ? pad.SpawnedBuilding : null;
        }

        public SalesPoint ShopFor(BuildingKind kind)
        {
            GameObject b = BuildingObjectFor(kind);
            return b != null ? b.GetComponent<SalesPoint>() : null;
        }

        public WorkerStation StationFor(BuildingKind kind)
        {
            GameObject b = BuildingObjectFor(kind);
            return b != null ? b.GetComponent<WorkerStation>() : null;
        }

        /// <summary>Closest available node of a given good, for pointing the tutorial arrow.</summary>
        public HarvestNode NearestNode(ResourceType type)
        {
            Transform player = PlayerRef.Root;
            if (player == null) return null;

            HarvestNode best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < HarvestNode.All.Count; i++)
            {
                HarvestNode node = HarvestNode.All[i];
                if (node == null || !node.IsAvailable || node.ResourceType != type) continue;

                float distance = Vector3.SqrMagnitude(node.Position - player.position);
                if (distance >= bestDistance) continue;

                bestDistance = distance;
                best = node;
            }
            return best;
        }
    }
}
