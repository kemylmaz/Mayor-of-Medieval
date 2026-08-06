using MayorOfMedieval.Core;
using MayorOfMedieval.Economy;
using MayorOfMedieval.NPC;
using TMPro;
using UnityEngine;

namespace MayorOfMedieval.Building
{
    /// <summary>
    /// The hire pad attached to a building. Each hire costs more than the last, and the
    /// role each new villager takes is fixed by <see cref="hireOrder"/> — so at a lumber
    /// camp the first hire chops and the second hauls the pile to a shop.
    /// </summary>
    public class WorkerStation : MonoBehaviour
    {
        [Header("Workers")]
        [SerializeField] private GameObject workerPrefab;
        [Tooltip("Role given to hire #1, #2, ... . The list length is the staffing cap.")]
        [SerializeField] private WorkerRole[] hireOrder = { WorkerRole.Harvester, WorkerRole.Carrier };
        [SerializeField] private ResourceType cargoType = ResourceType.Wood;
        [SerializeField] private Stockpile stockpile;
        [Tooltip("Workshop a Producer hire should keep supplied.")]
        [SerializeField] private ProductionBuilding workshop;

        [Header("Hire Pad")]
        [SerializeField] private Transform padAnchor;
        [SerializeField] private Vector3 padOffset = new Vector3(1.8f, 0f, 0f);
        [SerializeField] private float padRadius = 1.5f;
        [SerializeField] private float payInterval = 0.04f;
        [SerializeField] private int payPerTick = 3;

        public int WorkerCount { get; private set; }
        public int MaxWorkers => hireOrder != null ? hireOrder.Length : 0;
        public bool IsFullyStaffed => WorkerCount >= MaxWorkers;

        private int remainingCost;
        private float payTimer;
        private GameObject padVisual;
        private TextMeshPro padLabel;
        private Transform padArrow;
        private Vector3 arrowBase;
        private static Material sharedPadMaterial;

        private void Awake()
        {
            if (padAnchor == null) padAnchor = transform;
            remainingCost = GameConfig.WorkerCostFor(0);
        }

        private void Start()
        {
            BuildPad();
            RefreshPad();
        }

        private void Update()
        {
            if (IsFullyStaffed)
            {
                if (padVisual != null && padVisual.activeSelf) padVisual.SetActive(false);
                return;
            }

            AnimateArrow();

            payTimer -= Time.deltaTime;
            if (payTimer > 0f) return;
            payTimer = payInterval;

            Transform player = PlayerRef.Root;
            if (player == null) return;
            if (Vector3.Distance(player.position, PadPosition) > padRadius) return;

            ResourceManager wallet = ResourceManager.Instance;
            if (wallet == null) return;

            int step = Mathf.Min(payPerTick, remainingCost);
            step = Mathf.Min(step, wallet.Gold);
            if (step <= 0) return;

            if (!wallet.SpendResource(ResourceType.Gold, step)) return;

            remainingCost -= step;
            RefreshPad();

            if (remainingCost <= 0) HireWorker();
        }

        private Vector3 PadPosition => padAnchor.position + padAnchor.TransformVector(padOffset);

        /// <summary>Re-hires a saved crew with no gold cost.</summary>
        public void RestoreWorkers(int count)
        {
            for (int i = WorkerCount; i < Mathf.Min(count, MaxWorkers); i++) HireWorker();
        }

        private void HireWorker()
        {
            WorkerRole role = hireOrder[Mathf.Clamp(WorkerCount, 0, hireOrder.Length - 1)];
            WorkerCount++;
            remainingCost = GameConfig.WorkerCostFor(WorkerCount);

            if (workerPrefab != null)
            {
                Vector3 spawn = PadPosition + Vector3.forward * 0.6f;
                GameObject go = Instantiate(workerPrefab, spawn, Quaternion.identity, transform);
                go.name = "Worker_" + role + "_" + WorkerCount;

                Worker worker = go.GetComponent<Worker>();
                if (worker == null) worker = go.AddComponent<Worker>();

                switch (role)
                {
                    case WorkerRole.Producer: worker.ConfigureProducer(workshop); break;
                    case WorkerRole.GoldCollector: worker.ConfigureCollector(); break;
                    default: worker.Configure(role, cargoType, stockpile); break;
                }
            }

            string label = role == WorkerRole.Carrier ? "Tasiyici +1"
                         : role == WorkerRole.Producer ? "Ustaci +1"
                         : role == WorkerRole.GoldCollector ? "Tahsildar +1"
                         : "Isci +1";
            UI.FloatingText.Spawn(PadPosition + Vector3.up * 2f, label, new Color(0.4f, 0.9f, 0.4f));
            RefreshPad();
        }

        private void BuildPad()
        {
            padVisual = new GameObject("HirePad");
            padVisual.transform.SetParent(transform, false);
            padVisual.transform.position = PadPosition;

            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "Pad";
            Collider discCol = disc.GetComponent<Collider>();
            if (discCol != null) Destroy(discCol);
            disc.transform.SetParent(padVisual.transform, false);
            disc.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            disc.transform.localScale = new Vector3(1.5f, 0.02f, 1.5f);

            Renderer discRenderer = disc.GetComponent<Renderer>();
            if (discRenderer != null)
            {
                if (sharedPadMaterial == null)
                {
                    Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                    sharedPadMaterial = new Material(shader);
                    Color green = new Color(0.35f, 0.8f, 0.4f);
                    sharedPadMaterial.SetColor("_BaseColor", green);
                    sharedPadMaterial.SetColor("_Color", green);
                }
                discRenderer.sharedMaterial = sharedPadMaterial;
            }

            GameObject arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arrow.name = "Arrow";
            Collider arrowCol = arrow.GetComponent<Collider>();
            if (arrowCol != null) Destroy(arrowCol);
            arrow.transform.SetParent(padVisual.transform, false);
            arrow.transform.localPosition = new Vector3(0f, 1.3f, 0f);
            arrow.transform.localRotation = Quaternion.Euler(0f, 45f, 45f);
            arrow.transform.localScale = Vector3.one * 0.3f;
            Renderer arrowRenderer = arrow.GetComponent<Renderer>();
            if (arrowRenderer != null && sharedPadMaterial != null) arrowRenderer.sharedMaterial = sharedPadMaterial;
            padArrow = arrow.transform;
            arrowBase = padArrow.localPosition;

            GameObject labelGo = new GameObject("CostLabel");
            labelGo.transform.SetParent(padVisual.transform, false);
            padLabel = labelGo.AddComponent<TextMeshPro>();
            padLabel.font = TMP_Settings.defaultFontAsset;
            padLabel.fontSize = 4.5f;
            padLabel.fontStyle = FontStyles.Bold;
            padLabel.alignment = TextAlignmentOptions.Center;
            padLabel.color = Color.white;
            padLabel.rectTransform.sizeDelta = new Vector2(3f, 0.9f);
            padLabel.rectTransform.localPosition = new Vector3(0f, 0.6f, 0f);
            padLabel.rectTransform.localRotation = Quaternion.LookRotation(new Vector3(-15f, -20f, 15f).normalized, Vector3.up);
        }

        private void AnimateArrow()
        {
            if (padArrow == null) return;
            float bounce = Mathf.Abs(Mathf.Sin(Time.time * 4f)) * 0.28f;
            padArrow.localPosition = arrowBase + Vector3.up * bounce;
        }

        private void RefreshPad()
        {
            if (padLabel == null) return;
            padLabel.SetText(IsFullyStaffed ? "DOLU" : remainingCost.ToString());
        }

        private void OnDrawGizmosSelected()
        {
            Transform anchor = padAnchor != null ? padAnchor : transform;
            Gizmos.color = new Color(0.35f, 0.85f, 0.4f);
            Gizmos.DrawWireSphere(anchor.position + anchor.TransformVector(padOffset), padRadius);
        }
    }
}
