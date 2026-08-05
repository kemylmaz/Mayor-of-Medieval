using MayorOfMedieval.Core;
using MayorOfMedieval.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MayorOfMedieval.UI
{
    /// <summary>
    /// Deliberately minimal HUD: gold only, plus the quest banner and progress bar.
    /// Wood/stone/grain/meat/bread are never shown as counters — the player reads those
    /// off the stack on their head, which is what keeps the screen playable-ad clean.
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        public static HUDManager Instance { get; private set; }

        [Header("Gold")]
        [SerializeField] private TMP_Text goldText;

        [Header("Quest")]
        [SerializeField] private TMP_Text questText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TMP_Text progressText;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void Start()
        {
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnResourceChanged += HandleResourceChanged;
                SetGold(ResourceManager.Instance.Gold);
            }

            if (GameProgression.Instance != null)
            {
                GameProgression.Instance.OnQuestChanged += HandleQuestChanged;
                HandleQuestChanged(GameProgression.Instance.CurrentQuestText, GameProgression.Instance.Progress);
            }
        }

        private void OnDestroy()
        {
            if (ResourceManager.Instance != null) ResourceManager.Instance.OnResourceChanged -= HandleResourceChanged;
            if (GameProgression.Instance != null) GameProgression.Instance.OnQuestChanged -= HandleQuestChanged;
        }

        public void Bind(TMP_Text gold, TMP_Text quest, Slider progress, TMP_Text progressLabel)
        {
            goldText = gold;
            questText = quest;
            progressBar = progress;
            progressText = progressLabel;
        }

        private void HandleResourceChanged(ResourceType type, int oldValue, int newValue)
        {
            if (type == ResourceType.Gold) SetGold(newValue);
        }

        private void SetGold(int value)
        {
            if (goldText != null) goldText.SetText("{0}", value);
        }

        private void HandleQuestChanged(string text, float progress)
        {
            if (questText != null) questText.SetText(text);
            if (progressBar != null) progressBar.value = progress;
            if (progressText != null) progressText.SetText("{0}%", Mathf.RoundToInt(progress * 100f));
        }
    }
}
