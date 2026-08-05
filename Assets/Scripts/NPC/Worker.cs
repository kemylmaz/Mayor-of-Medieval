using MayorOfMedieval.Building;
using MayorOfMedieval.Character;
using MayorOfMedieval.Economy;
using MayorOfMedieval.Environment;
using UnityEngine;

namespace MayorOfMedieval.NPC
{
    /// <summary>
    /// A hired villager. Endlessly walks to the nearest matching resource node, harvests
    /// until its little stack is full, hauls it back to the station stockpile, repeats.
    /// This is the automation the player buys with the 100-gold hire pads.
    /// </summary>
    [RequireComponent(typeof(CarrySystem))]
    public class Worker : MonoBehaviour
    {
        private enum State { SeekNode, Harvest, Deliver }

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3.1f;
        [SerializeField] private float turnSpeed = 12f;

        [Header("Work")]
        [SerializeField] private float nodeStandDistance = 1.5f;
        [SerializeField] private float depositDistance = 1.6f;
        [SerializeField] private float searchInterval = 0.75f;

        private ResourceType harvestType = ResourceType.Wood;
        private Stockpile dropOff;
        private CarrySystem carry;
        private State state = State.SeekNode;
        private HarvestNode targetNode;
        private float searchTimer;
        private Vector3 idleAnchor;

        public void Configure(ResourceType type, Stockpile stockpile)
        {
            harvestType = type;
            dropOff = stockpile;
            idleAnchor = transform.position;
        }

        private void Awake()
        {
            carry = GetComponent<CarrySystem>();
            if (GetComponent<CarrierBeacon>() == null) gameObject.AddComponent<CarrierBeacon>();
        }

        private void Update()
        {
            switch (state)
            {
                case State.SeekNode: TickSeek(); break;
                case State.Harvest: TickHarvest(); break;
                case State.Deliver: TickDeliver(); break;
            }
        }

        private void TickSeek()
        {
            if (carry.IsFull) { state = State.Deliver; return; }

            searchTimer -= Time.deltaTime;
            if (targetNode == null || !targetNode.IsAvailable)
            {
                if (searchTimer > 0f)
                {
                    // Idle in place between scans rather than spamming the search.
                    MoveToward(idleAnchor, 0.4f);
                    return;
                }
                searchTimer = searchInterval;
                targetNode = FindNearestNode();
            }

            if (targetNode == null) return;

            if (MoveToward(targetNode.Position, nodeStandDistance)) state = State.Harvest;
        }

        private void TickHarvest()
        {
            // HarvestNode itself feeds our CarrySystem while we stand in range.
            if (targetNode == null || !targetNode.IsAvailable)
            {
                state = carry.IsEmpty ? State.SeekNode : State.Deliver;
                targetNode = null;
                return;
            }

            if (carry.IsFull) { state = State.Deliver; return; }

            // Drift back in if the node's harvest radius is tighter than our stand distance.
            MoveToward(targetNode.Position, nodeStandDistance);
        }

        private void TickDeliver()
        {
            if (dropOff == null) { state = State.SeekNode; return; }

            if (!MoveToward(dropOff.transform.position, depositDistance)) return;

            if (carry.CountOf(harvestType) > 0 && !dropOff.IsFull)
            {
                if (carry.TryRemove(harvestType)) dropOff.Add(1);
                return;
            }

            // Nothing left worth hauling (or the pile is full) — go find more work.
            if (carry.CountOf(harvestType) == 0)
            {
                state = State.SeekNode;
                targetNode = null;
            }
        }

        private HarvestNode FindNearestNode()
        {
            HarvestNode best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < HarvestNode.All.Count; i++)
            {
                HarvestNode node = HarvestNode.All[i];
                if (node == null || !node.IsAvailable) continue;
                if (node.ResourceType != harvestType) continue;

                float distance = Vector3.SqrMagnitude(node.Position - transform.position);
                if (distance >= bestDistance) continue;

                bestDistance = distance;
                best = node;
            }
            return best;
        }

        private bool MoveToward(Vector3 target, float stopDistance)
        {
            Vector3 delta = target - transform.position;
            delta.y = 0f;

            if (delta.magnitude <= stopDistance) return true;

            Vector3 dir = delta.normalized;
            transform.position += dir * moveSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);
            return false;
        }
    }
}
