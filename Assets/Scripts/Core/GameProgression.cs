using System;
using System.Collections.Generic;
using MayorOfMedieval.Economy;
using UnityEngine;

namespace MayorOfMedieval.Core
{
    /// <summary>
    /// Drives the whole unlock ladder: reveals a build pad once the player's gold has
    /// ever reached its price, tracks which buildings exist, and publishes the current
    /// quest line for the HUD and the world arrow.
    /// </summary>
    public class GameProgression : MonoBehaviour
    {
        public static GameProgression Instance { get; private set; }

        [Serializable]
        public class Step
        {
            public BuildingKind kind;
            [Tooltip("Pad revealed once PeakGold reaches this. Defaults to the building's price.")]
            public int revealAtGold = -1;
            [Tooltip("Optional building that must exist before this pad can appear.")]
            public BuildingKind requires;
            public bool hasRequirement;
            [TextArea] public string questText;
        }

        [Header("Ladder")]
        [SerializeField]
        private List<Step> steps = new List<Step>();

        /// <summary>(questText, progress 0..1)</summary>
        public event Action<string, float> OnQuestChanged;
        public event Action<BuildingKind> OnBuildingConstructed;

        /// <summary>Raised whenever a customer order is fully delivered.</summary>
        public static event Action<ResourceType> OnOrderCompleted;

        private readonly HashSet<BuildingKind> built = new HashSet<BuildingKind>();
        private readonly Dictionary<BuildingKind, BuildPad> pads = new Dictionary<BuildingKind, BuildPad>();
        private float lastBroadcastProgress = -1f;

        public Transform CurrentTarget { get; private set; }
        public string CurrentQuestText { get; private set; } = "";
        public float Progress => steps.Count == 0 ? 0f : (float)built.Count / steps.Count;

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
            if (steps.Count == 0) BuildDefaultLadder();
        }

        private void Start() => Refresh();

        private void Update() => Refresh();

        private void BuildDefaultLadder()
        {
            steps.Add(new Step { kind = BuildingKind.Market, questText = "Pazari kur!" });
            steps.Add(new Step { kind = BuildingKind.LumberCamp, questText = "Oduncu kulubesi ac!" });
            steps.Add(new Step { kind = BuildingKind.Quarry, questText = "Tas ocagi ac!" });
            steps.Add(new Step { kind = BuildingKind.Farm, questText = "Ciftlik kur!" });
            steps.Add(new Step { kind = BuildingKind.CropField, questText = "Tarla ek!" });
            steps.Add(new Step { kind = BuildingKind.Well, hasRequirement = true, requires = BuildingKind.CropField, questText = "Kuyu kaz!" });
            steps.Add(new Step { kind = BuildingKind.Mill, questText = "Degirmen kur!" });
            steps.Add(new Step { kind = BuildingKind.Treasury, questText = "Hazine kur!" });
            steps.Add(new Step { kind = BuildingKind.Blacksmith, questText = "Demirci ac!" });
            steps.Add(new Step { kind = BuildingKind.Barracks, hasRequirement = true, requires = BuildingKind.Blacksmith, questText = "Kisla kur!" });
            steps.Add(new Step { kind = BuildingKind.Inn, hasRequirement = true, requires = BuildingKind.Mill, questText = "Hani kur!" });
            steps.Add(new Step { kind = BuildingKind.VillageSquare, questText = "Koy meydani yap!" });
            steps.Add(new Step { kind = BuildingKind.Church, questText = "Kilise yap!" });
        }

        public void RegisterPad(BuildingKind kind, BuildPad pad)
        {
            pads[kind] = pad;
            Refresh();
        }

        public bool IsBuilt(BuildingKind kind) => built.Contains(kind);

        public void NotifyBuilt(BuildingKind kind)
        {
            if (!built.Add(kind)) return;
            OnBuildingConstructed?.Invoke(kind);
            Refresh();
        }

        internal static void NotifyOrderCompleted(ResourceType type) => OnOrderCompleted?.Invoke(type);

        private void Refresh()
        {
            int peak = ResourceManager.Instance != null ? ResourceManager.Instance.PeakGold : 0;

            Transform target = null;
            string questText = "Tum binalar tamam!";
            bool questChosen = false;

            for (int i = 0; i < steps.Count; i++)
            {
                Step step = steps[i];

                BuildPad pad;
                bool hasPad = pads.TryGetValue(step.kind, out pad) && pad != null;

                int reveal = step.revealAtGold >= 0 ? step.revealAtGold : GameConfig.CostOf(step.kind);
                bool requirementMet = !step.hasRequirement || built.Contains(step.requires);
                bool revealed = peak >= reveal && requirementMet;

                // Every pad's visibility is refreshed, regardless of which step owns the quest.
                if (hasPad) pad.SetRevealed(revealed && !built.Contains(step.kind));

                if (built.Contains(step.kind)) continue;
                if (questChosen) continue;
                // Gated behind a building that isn't up yet — let a later step claim the quest.
                if (!requirementMet) continue;

                questChosen = true;

                if (revealed && hasPad)
                {
                    target = pad.transform;
                    questText = string.IsNullOrEmpty(step.questText)
                        ? GameConfig.DisplayName(step.kind) + " kur!"
                        : step.questText;
                }
                else
                {
                    questText = reveal + " altin biriktir (" + GameConfig.DisplayName(step.kind) + ")";
                    target = null;
                }
            }

            CurrentTarget = target;

            // Progress moves independently of the text (e.g. the Well is built while the
            // quest still points at the Market), so both have to be watched.
            float progress = Progress;
            if (CurrentQuestText != questText || !Mathf.Approximately(lastBroadcastProgress, progress))
            {
                CurrentQuestText = questText;
                lastBroadcastProgress = progress;
                OnQuestChanged?.Invoke(questText, progress);
            }
        }
    }
}
