using System.Collections.Generic;
using MayorOfMedieval.Core;
using MayorOfMedieval.Economy;
using UnityEngine;

namespace MayorOfMedieval.NPC
{
    /// <summary>
    /// A fixed, repeatable target for the barracks soldiers to farm. Takes damage, pays a
    /// bounty when it drops, then stands back up after a short respawn so the loop never dries up.
    /// </summary>
    public class TrainingDummy : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float respawnSeconds = 4f;
        [SerializeField] private Transform visualRoot;

        public bool IsAlive { get; private set; } = true;
        public Vector3 Position => transform.position;

        private static readonly List<TrainingDummy> all = new List<TrainingDummy>();
        public static IReadOnlyList<TrainingDummy> All => all;

        private float health;
        private float respawnTimer;
        private Vector3 baseScale;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => all.Clear();

        private void Awake()
        {
            if (visualRoot == null) visualRoot = transform;
            baseScale = visualRoot.localScale;
            health = maxHealth;
        }

        private void OnEnable() => all.Add(this);
        private void OnDisable() => all.Remove(this);

        private void Update()
        {
            if (IsAlive)
            {
                // Ease back to rest after a hit wobble.
                visualRoot.localScale = Vector3.Lerp(visualRoot.localScale, baseScale, 8f * Time.deltaTime);
                return;
            }

            respawnTimer -= Time.deltaTime;
            if (respawnTimer > 0f) return;

            IsAlive = true;
            health = maxHealth;
            visualRoot.localScale = baseScale;
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive) return;

            health -= amount;
            visualRoot.localScale = baseScale * 1.12f;

            if (health > 0f) return;

            IsAlive = false;
            respawnTimer = respawnSeconds;
            visualRoot.localScale = Vector3.zero;

            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.AddResource(ResourceType.Gold, GameConfig.EnemyBounty);
            }
            UI.FloatingText.Spawn(transform.position + Vector3.up * 2f, "+" + GameConfig.EnemyBounty, new Color(1f, 0.85f, 0.2f));
        }

        /// <summary>Nearest dummy still standing.</summary>
        public static TrainingDummy FindNearestAlive(Vector3 from)
        {
            TrainingDummy best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < all.Count; i++)
            {
                TrainingDummy dummy = all[i];
                if (dummy == null || !dummy.IsAlive) continue;

                float distance = Vector3.SqrMagnitude(dummy.transform.position - from);
                if (distance >= bestDistance) continue;

                bestDistance = distance;
                best = dummy;
            }
            return best;
        }
    }
}
