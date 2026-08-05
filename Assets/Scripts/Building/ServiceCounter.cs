using System.Collections.Generic;
using MayorOfMedieval.Economy;
using UnityEngine;

namespace MayorOfMedieval.Building
{
    /// <summary>
    /// The spot in front of a building where customers line up. Each counter advertises
    /// which goods it takes orders for, so unlocking the Farm/Field/Mill widens the pool
    /// of orders the spawner can hand out.
    /// </summary>
    public class ServiceCounter : MonoBehaviour
    {
        [Header("Orders")]
        [Tooltip("Goods customers at this counter will ask for.")]
        [SerializeField] private List<ResourceType> acceptedGoods = new List<ResourceType> { ResourceType.Wood, ResourceType.Stone };

        [Header("Queue")]
        [SerializeField] private Transform queueAnchor;
        [SerializeField] private Vector3 queueDirection = new Vector3(0f, 0f, -1f);
        [SerializeField] private float queueSpacing = 1.4f;
        [SerializeField] private int maxQueueLength = 4;

        public IReadOnlyList<ResourceType> AcceptedGoods => acceptedGoods;
        public int QueueCount => queue.Count;
        public bool HasRoom => queue.Count < maxQueueLength;

        private static readonly List<ServiceCounter> all = new List<ServiceCounter>();
        public static IReadOnlyList<ServiceCounter> All => all;

        private readonly List<object> queue = new List<object>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => all.Clear();

        private void Awake()
        {
            if (queueAnchor == null) queueAnchor = transform;
            if (queueDirection.sqrMagnitude < 0.001f) queueDirection = Vector3.back;
            queueDirection.Normalize();
        }

        private void OnEnable() => all.Add(this);
        private void OnDisable() => all.Remove(this);

        /// <summary>Reserves the next slot in line. Returns -1 when the queue is full.</summary>
        public int Join(object customer)
        {
            if (!HasRoom) return -1;
            queue.Add(customer);
            return queue.Count - 1;
        }

        public void Leave(object customer)
        {
            queue.Remove(customer);
        }

        public int IndexOf(object customer) => queue.IndexOf(customer);

        public Vector3 SlotPosition(int index)
        {
            return queueAnchor.position + queueDirection * (queueSpacing * Mathf.Max(0, index));
        }

        /// <summary>Facing direction for a queued customer (they look at the counter).</summary>
        public Vector3 FacingFromQueue => -queueDirection;

        public bool Accepts(ResourceType type) => acceptedGoods.Contains(type);

        public void AddAcceptedGood(ResourceType type)
        {
            if (!acceptedGoods.Contains(type)) acceptedGoods.Add(type);
        }

        private void OnDrawGizmosSelected()
        {
            Transform anchor = queueAnchor != null ? queueAnchor : transform;
            Vector3 dir = queueDirection.sqrMagnitude < 0.001f ? Vector3.back : queueDirection.normalized;
            Gizmos.color = new Color(0.3f, 0.7f, 1f);
            for (int i = 0; i < maxQueueLength; i++)
            {
                Gizmos.DrawWireCube(anchor.position + dir * (queueSpacing * i), new Vector3(0.8f, 0.1f, 0.8f));
            }
        }
    }
}
