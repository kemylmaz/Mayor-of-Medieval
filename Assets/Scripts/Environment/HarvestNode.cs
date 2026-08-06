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

        [Header("Wandering (animals)")]
        [Tooltip("Livestock drift around their patch instead of standing like statues.")]
        [SerializeField] private bool wanders;
        [SerializeField] private float wanderRadius = 5f;
        [SerializeField] private float wanderSpeed = 0.9f;
        [SerializeField] private Vector2 pauseRange = new Vector2(1.2f, 3.5f);

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

        private Vector3 homePosition;
        private Vector3 wanderTarget;
        private float wanderPause;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => all.Clear();

        private void Awake()
        {
            if (shakeRoot == null) shakeRoot = transform;
            baseScale = shakeRoot.localScale;
            unitsLeft = Mathf.Max(1, unitsPerNode);

            homePosition = transform.position;
            wanderTarget = homePosition;
            wanderPause = Random.Range(pauseRange.x, pauseRange.y);
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
                if (wanders) TickWander();
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

        /// <summary>Lazy grazing drift: pick a spot in the patch, amble over, pause, repeat.</summary>
        private void TickWander()
        {
            if (wanderPause > 0f)
            {
                wanderPause -= Time.deltaTime;
                return;
            }

            Vector3 delta = wanderTarget - transform.position;
            delta.y = 0f;

            if (delta.magnitude < 0.25f)
            {
                Vector2 offset = Random.insideUnitCircle * wanderRadius;
                wanderTarget = homePosition + new Vector3(offset.x, 0f, offset.y);
                wanderPause = Random.Range(pauseRange.x, pauseRange.y);
                return;
            }

            Vector3 dir = delta.normalized;
            transform.position += dir * wanderSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 5f * Time.deltaTime);
        }

        private CarrySystem FindHarvesterInRange()
        {
            CarrySystem best = null;
            float bestDistance = harvestRadius;

            for (int i = 0; i < CarrierRegistry.Beacons.Count; i++)
            {
                CarrierBeacon beacon = CarrierRegistry.Beacons[i];
                if (beacon == null || !beacon.WillAccept(resourceType)) continue;

                CarrySystem carrier = beacon.Carry;
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
        private static readonly List<CarrierBeacon> beacons = new List<CarrierBeacon>();
        public static IReadOnlyList<CarrierBeacon> Beacons => beacons;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => beacons.Clear();

        public static void Register(CarrierBeacon beacon)
        {
            if (beacon != null && !beacons.Contains(beacon)) beacons.Add(beacon);
        }

        public static void Unregister(CarrierBeacon beacon) => beacons.Remove(beacon);
    }

    /// <summary>
    /// Add next to a CarrySystem to make it eligible to harvest world nodes. Carriers,
    /// producers and tax collectors set <see cref="Accepts"/> so they don't get their
    /// backpack stuffed with logs just for walking past a tree.
    /// </summary>
    [RequireComponent(typeof(CarrySystem))]
    public class CarrierBeacon : MonoBehaviour
    {
        public CarrySystem Carry { get; private set; }

        /// <summary>Null means "take anything" — that's the Lord.</summary>
        public System.Func<ResourceType, bool> Accepts;

        public bool WillAccept(ResourceType type) => Accepts == null || Accepts(type);

        private void Awake() => Carry = GetComponent<CarrySystem>();
        private void OnEnable() => CarrierRegistry.Register(this);
        private void OnDisable() => CarrierRegistry.Unregister(this);
    }
}
