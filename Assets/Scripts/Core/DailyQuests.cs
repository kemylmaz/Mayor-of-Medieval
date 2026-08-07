using System;
using System.Collections.Generic;
using MayorOfMedieval.Economy;
using UnityEngine;

namespace MayorOfMedieval.Core
{
    /// <summary>
    /// A rolling set of three short objectives drawn from things the player actually does:
    /// gather, sell, build, hire, collect takings. They refresh once a day (and whenever
    /// all three are cleared) so there is always a reason to come back.
    /// </summary>
    public class DailyQuests : MonoBehaviour
    {
        private const string SeedKey = "MayorOfMedieval.Daily.Seed";
        private const string DayKey = "MayorOfMedieval.Daily.Day";
        private const string ProgressKey = "MayorOfMedieval.Daily.Progress";

        public static DailyQuests Instance { get; private set; }

        public enum Track { Gather, Sell, Build, Hire, CollectGold }

        [Serializable]
        public class Task
        {
            public Track track;
            public ResourceType resource;
            public int target;
            public int progress;
            public string text;
            public int reward;

            public bool IsDone => progress >= target;
            public float Ratio => target <= 0 ? 1f : Mathf.Clamp01(progress / (float)target);
        }

        [SerializeField] private int taskCount = 3;

        private readonly List<Task> tasks = new List<Task>();
        public IReadOnlyList<Task> Tasks => tasks;

        public event Action OnTasksChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void Start()
        {
            int today = DateTime.UtcNow.DayOfYear;
            int storedDay = PlayerPrefs.GetInt(DayKey, -1);

            if (storedDay != today)
            {
                // New day: brand new set, seeded by the date so it is stable across a session.
                Roll(today * 7919);
                PlayerPrefs.SetInt(DayKey, today);
                PlayerPrefs.SetInt(SeedKey, today * 7919);
            }
            else
            {
                Roll(PlayerPrefs.GetInt(SeedKey, today * 7919));
                RestoreProgress();
            }

            HookEvents();
        }

        private void OnDestroy()
        {
            GameProgression.OnOrderCompleted -= HandleOrderCompleted;
            SaveProgress();
        }

        private void HookEvents()
        {
            GameProgression.OnOrderCompleted += HandleOrderCompleted;
        }

        // ------------------------------------------------------------------ rolling

        private void Roll(int seed)
        {
            System.Random rng = new System.Random(seed);
            tasks.Clear();

            // Pick distinct tracks so the three tasks never read as duplicates.
            List<Track> pool = new List<Track>
            {
                Track.Gather, Track.Sell, Track.Build, Track.Hire, Track.CollectGold
            };

            for (int i = 0; i < taskCount && pool.Count > 0; i++)
            {
                int pick = rng.Next(pool.Count);
                Track track = pool[pick];
                pool.RemoveAt(pick);
                tasks.Add(Create(track, rng));
            }

            OnTasksChanged?.Invoke();
        }

        private Task Create(Track track, System.Random rng)
        {
            switch (track)
            {
                case Track.Gather:
                {
                    ResourceType[] basics = { ResourceType.Wood, ResourceType.Stone };
                    ResourceType res = basics[rng.Next(basics.Length)];
                    int amount = 8 + rng.Next(3) * 4;
                    return new Task
                    {
                        track = track, resource = res, target = amount,
                        text = amount + " " + GameConfig.DisplayName(res).ToLowerInvariant() + " topla",
                        reward = amount * 4
                    };
                }
                case Track.Sell:
                {
                    int amount = 6 + rng.Next(4) * 3;
                    return new Task
                    {
                        track = track, target = amount,
                        text = amount + " mal sat",
                        reward = amount * 8
                    };
                }
                case Track.Build:
                {
                    int amount = 1 + rng.Next(2);
                    return new Task
                    {
                        track = track, target = amount,
                        text = amount + " bina kur",
                        reward = amount * 120
                    };
                }
                case Track.Hire:
                {
                    int amount = 1 + rng.Next(2);
                    return new Task
                    {
                        track = track, target = amount,
                        text = amount + " isci al",
                        reward = amount * 100
                    };
                }
                default:
                {
                    int amount = 150 + rng.Next(4) * 50;
                    return new Task
                    {
                        track = Track.CollectGold, target = amount,
                        text = amount + " altin kazan",
                        reward = 100
                    };
                }
            }
        }

        // ------------------------------------------------------------------ reporting

        public void Report(Track track, int amount = 1, ResourceType resource = ResourceType.Gold)
        {
            bool changed = false;

            for (int i = 0; i < tasks.Count; i++)
            {
                Task task = tasks[i];
                if (task.track != track || task.IsDone) continue;
                if (track == Track.Gather && task.resource != resource) continue;

                task.progress += amount;
                changed = true;

                if (task.IsDone)
                {
                    if (ResourceManager.Instance != null)
                        ResourceManager.Instance.AddResource(ResourceType.Gold, task.reward);
                    AudioManager.PlaySafe(Sfx.Complete);
                }
            }

            if (!changed) return;

            SaveProgress();
            OnTasksChanged?.Invoke();

            // Fully qualified: MayorOfMedieval.Environment would otherwise shadow System.Environment.
            if (AllDone()) Roll(System.Environment.TickCount);
        }

        private bool AllDone()
        {
            for (int i = 0; i < tasks.Count; i++) if (!tasks[i].IsDone) return false;
            return tasks.Count > 0;
        }

        private void HandleOrderCompleted(ResourceType type) => Report(Track.Sell);

        // ------------------------------------------------------------------ persistence

        private void SaveProgress()
        {
            string[] parts = new string[tasks.Count];
            for (int i = 0; i < tasks.Count; i++) parts[i] = tasks[i].progress.ToString();
            PlayerPrefs.SetString(ProgressKey, string.Join(",", parts));
        }

        private void RestoreProgress()
        {
            string raw = PlayerPrefs.GetString(ProgressKey, "");
            if (string.IsNullOrEmpty(raw)) return;

            string[] parts = raw.Split(',');
            for (int i = 0; i < tasks.Count && i < parts.Length; i++)
            {
                int value;
                if (int.TryParse(parts[i], out value)) tasks[i].progress = value;
            }
        }
    }
}
