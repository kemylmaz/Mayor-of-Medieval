using System.Collections.Generic;
using MayorOfMedieval.Character;
using MayorOfMedieval.Core;
using MayorOfMedieval.Economy;
using UnityEngine;

namespace MayorOfMedieval.Environment
{
    /// <summary>
    /// A tree, rock or animal the player (or a worker) stands next to to harvest.
    /// Yields one unit at a time straight into the harvester's carry stack, depletes,
    /// then regrows after a delay.
    /// </summary>
    public class HarvestNode : MonoBehaviour
    {
        [Header("Yield")]
        [SerializeField] private ResourceType resourceType = ResourceType.Wood;
        [SerializeField] private int unitsPerNode = 3;
        [SerializeField] private float secondsPerUnit = 0.6f;
        [SerializeField] private float harvestRadius = 2.2f;
        [SerializeField] private float respawnSeconds = 10f;

        [Header("Visual")]
        [SerializeField] private Transform shakeRoot;
        [SerializeField] private float shakeAmount = 0.07f;

        public ResourceType ResourceType => resourceType;
        public bool IsAvailable => !depleted;
        public Vector3 Position => transform.position;

        private static readonly List<HarvestNode> all = new List<HarvestNode>();
        public static IReadOnlyList<HarvestNode> All => all;

        private int unitsLeft;
        private bool depleted;
        private float progress;
        private float respawnTimer;
        private Vector3 baseScale;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => all.Clear();

        private void Awake()
        {
            if (shakeRoot == null) shakeRoot = transform;
            baseScale = shakeRoot.localScale;
            unitsLeft = Mathf.Max(1, unitsPerNode);
        }

        private void OnEnable() => all.Add(this);
        private void OnDisable() => all.Remove(this);

        private void Update()
        {
            if (depleted)
            {
                respawnTimer -= Time.deltaTime;
                if (respawnTimer <= 0f) Respawn();
                return;
            }

            // Anyone with a CarrySystem standing close enough harvests; nearest wins the tick.
            CarrySystem harvester = FindHarvesterInRange();
            if (harvester == null)
            {
                progress = Mathf.Max(0f, progress - Time.deltaTime * 2f);
                shakeRoot.localScale = Vector3.Lerp(shakeRoot.localScale, baseScale, 10f * Time.deltaTime);
                return;
            }

            progress += Time.deltaTime;

            float shake = Mathf.Sin(Time.time * 28f) * shakeAmount;
            shakeRoot.localScale = baseScale + new Vector3(shake, -shake * 0.6f, shake);

            if (progress < secondsPerUnit) return;
            progress = 0f;

            if (!harvester.TryAdd(resourceType)) return; // stack full, keep the unit on the node

            unitsLeft--;
            if (unitsLeft <= 0) Deplete();
        }

        private CarrySystem FindHarvesterInRange()
        {
            CarrySystem best = null;
            float bestDistance = harvestRadius;

            for (int i = 0; i < CarrierRegistry.Carriers.Count; i++)
            {
                CarrySystem carrier = CarrierRegistry.Carriers[i];
                if (carrier == null || carrier.IsFull) continue;

                float distance = Vector3.Distance(carrier.transform.position, transform.position);
                if (distance > bestDistance) continue;

                bestDistance = distance;
                best = carrier;
            }
            return best;
        }

        private void Deplete()
        {
            depleted = true;
            progress = 0f;
            respawnTimer = respawnSeconds;
            shakeRoot.localScale = Vector3.zero;
        }

        private void Respawn()
        {
            depleted = false;
            unitsLeft = Mathf.Max(1, unitsPerNode);
            shakeRoot.localScale = baseScale;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = GameConfig.ColorOf(resourceType);
            Gizmos.DrawWireSphere(transform.position, harvestRadius);
        }
    }

    /// <summary>
    /// Lets HarvestNode find nearby carriers without a per-frame FindObjectsOfType.
    /// Every CarrySystem registers itself here through CarrierBeacon.
    /// </summary>
    public static class CarrierRegistry
    {
        private static readonly List<CarrySystem> carriers = new List<CarrySystem>();
        public static IReadOnlyList<CarrySystem> Carriers => carriers;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => carriers.Clear();

        public static void Register(CarrySystem carrier)
        {
            if (carrier != null && !carriers.Contains(carrier)) carriers.Add(carrier);
        }

        public static void Unregister(CarrySystem carrier) => carriers.Remove(carrier);
    }

    /// <summary>Add next to a CarrySystem to make it a valid harvester.</summary>
    [RequireComponent(typeof(CarrySystem))]
    public class CarrierBeacon : MonoBehaviour
    {
        private CarrySystem carrier;

        private void Awake() => carrier = GetComponent<CarrySystem>();
        private void OnEnable() => CarrierRegistry.Register(carrier);
        private void OnDisable() => CarrierRegistry.Unregister(carrier);
    }
}
