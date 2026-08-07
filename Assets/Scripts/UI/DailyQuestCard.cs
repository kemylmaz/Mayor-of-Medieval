using System.Collections.Generic;
using MayorOfMedieval.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MayorOfMedieval.UI
{
    /// <summary>Draws the three daily tasks and their progress bars.</summary>
    public class DailyQuestCard : MonoBehaviour
    {
        [SerializeField] private List<TMP_Text> rows = new List<TMP_Text>();
        [SerializeField] private List<Slider> bars = new List<Slider>();

        private void Start()
        {
            if (DailyQuests.Instance != null) DailyQuests.Instance.OnTasksChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (DailyQuests.Instance != null) DailyQuests.Instance.OnTasksChanged -= Refresh;
        }

        private void Refresh()
        {
            DailyQuests quests = DailyQuests.Instance;

            for (int i = 0; i < rows.Count; i++)
            {
                bool hasTask = quests != null && i < quests.Tasks.Count;
                if (rows[i] != null) rows[i].gameObject.SetActive(hasTask);
                if (bars[i] != null) bars[i].gameObject.SetActive(hasTask);
                if (!hasTask) continue;

                DailyQuests.Task task = quests.Tasks[i];
                if (rows[i] != null)
                {
                    // Plain ASCII only — the TMP font has no tick glyph and would draw a box.
                    rows[i].SetText(task.IsDone
                        ? "<color=#8CF09A>" + task.text + "  TAMAM</color>"
                        : task.text + "  " + Mathf.Min(task.progress, task.target) + "/" + task.target);
                }
                if (bars[i] != null) bars[i].value = task.Ratio;
            }
        }
    }
}
