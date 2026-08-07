using MayorOfMedieval.Building;
using MayorOfMedieval.Economy;
using TMPro;
using UnityEngine;

namespace MayorOfMedieval.Core
{
    /// <summary>
    /// The green "stand here and pay" pad from the reference games. Gold drains while the
    /// Lord stands on it (accelerating the longer they wait), then the building pops in and
    /// the pad disappears. Hidden entirely until GameProgression reveals it.
    /// </summary>
    public class BuildPad : MonoBehaviour
    {
        [Header("What this pad builds")]
        [SerializeField] private BuildingKind kind = BuildingKind.Market;
        [SerializeField] private GameObject buildingPrefab;
        [Tooltip("Where the building spawns. Defaults to this pad's own transform.")]
        [SerializeField] private Transform spawnPoint;

        [Header("Payment")]
        [SerializeField] private float payInterval = 0.04f;
        [SerializeField] private int payPerTick = 2;
        [SerializeField] private float maxPayMultiplier = 6f;
        [SerializeField] private float accelerationRampTime = 1.5f;
        [SerializeField] private float padRadius = 1.7f;

        [Header("Visual")]
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private Transform arrow;
        [SerializeField] private TextMeshPro costLabel;
        [SerializeField] private float bounceHeight = 0.35f;
        [SerializeField] private float bounceSpeed = 4f;
        [SerializeField] private float popOutDuration = 0.25f;

        public BuildingKind Kind => kind;
        public bool IsBuilt { get; private set; }
        public bool IsRevealed { get; private set; }

        /// <summary>The building this pad spawned, so a save can find its piles again.</summary>
        public GameObject SpawnedBuilding { get; private set; }

        /// <summary>Rebuilds instantly with no payment — used when restoring a save.</summary>
        public void RestoreBuilt()
        {
            if (!IsBuilt) Construct();
        }

        private int remainingCost;
        private float payTimer;
        private float timeOnPad;
        private Vector3 arrowBase;
        private Vector3 padBaseScale;
        private int lastLabelValue = -1;

        private void Awake()
        {
            padBaseScale = transform.localScale;
            if (spawnPoint == null) spawnPoint = transform;
            if (arrow != null) arrowBase = arrow.localPosition;
            remainingCost = GameConfig.CostOf(kind);
        }

        private void Start()
        {
            if (GameProgression.Instance != null) GameProgression.Instance.RegisterPad(kind, this);
            SetRevealed(false);
            RefreshLabel();
        }

        public void SetRevealed(bool revealed)
        {
            if (IsBuilt) revealed = false;
            IsRevealed = revealed;
            if (visualRoot != null) visualRoot.SetActive(revealed);
            else gameObject.SetActive(revealed || IsBuilt);
        }

        private void Update()
        {
            if (IsBuilt || !IsRevealed) return;

            AnimateArrow();

            Transform player = PlayerRef.Root;
            bool onPad = player != null && Vector3.Distance(player.position, transform.position) <= padRadius;

            if (!onPad)
            {
                timeOnPad = 0f;
                payTimer = 0f;
                return;
            }

            timeOnPad += Time.deltaTime;
            payTimer -= Time.deltaTime;
            if (payTimer > 0f) return;
            payTimer = payInterval;

            ResourceManager wallet = ResourceManager.Instance;
            if (wallet == null) return;

            float multiplier = accelerationRampTime <= 0f
                ? maxPayMultiplier
                : Mathf.Lerp(1f, Mathf.Max(1f, maxPayMultiplier), Mathf.Clamp01(timeOnPad / accelerationRampTime));

            int step = Mathf.CeilToInt(payPerTick * multiplier);
            step = Mathf.Min(step, remainingCost);
            step = Mathf.Min(step, wallet.Gold);
            if (step <= 0) return;

            if (!wallet.SpendResource(ResourceType.Gold, step)) return;

            remainingCost -= step;
            RefreshLabel();

            if (remainingCost <= 0) Construct();
        }

        private void Construct()
        {
            IsBuilt = true;

            if (buildingPrefab != null)
            {
                GameObject building = Instantiate(buildingPrefab, spawnPoint.position, spawnPoint.rotation, transform.parent);
                building.name = GameConfig.DisplayName(kind);
                SpawnedBuilding = building;
                StartCoroutine(PopIn(building.transform));
            }

            if (GameProgression.Instance != null) GameProgression.Instance.NotifyBuilt(kind);
            if (RoadNetwork.Instance != null) RoadNetwork.Instance.Connect(spawnPoint.position);

            AudioManager.PlaySafe(Sfx.Build);
            AudioManager.PlaySafe(Sfx.Complete);
            if (DailyQuests.Instance != null) DailyQuests.Instance.Report(DailyQuests.Track.Build);
            UI.FloatingText.Spawn(transform.position + Vector3.up * 2f, GameConfig.DisplayName(kind) + "!", new Color(0.4f, 0.9f, 0.4f));

            if (visualRoot != null) visualRoot.SetActive(false);
            enabled = false;
        }

        private System.Collections.IEnumerator PopIn(Transform target)
        {
            Vector3 full = target.localScale;
            float elapsed = 0f;
            while (elapsed < popOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / popOutDuration);
                float eased = t < 0.7f ? Mathf.Lerp(0f, 1.1f, t / 0.7f) : Mathf.Lerp(1.1f, 1f, (t - 0.7f) / 0.3f);
                if (target != null) target.localScale = full * eased;
                yield return null;
            }
            if (target != null) target.localScale = full;
        }

        private void AnimateArrow()
        {
            if (arrow == null) return;
            float bounce = Mathf.Abs(Mathf.Sin(Time.time * bounceSpeed)) * bounceHeight;
            arrow.localPosition = arrowBase + Vector3.up * bounce;
        }

        private void RefreshLabel()
        {
            if (costLabel == null || remainingCost == lastLabelValue) return;
            lastLabelValue = remainingCost;
            costLabel.SetText(remainingCost.ToString());
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.35f, 0.85f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, padRadius);
        }
    }
}
