using MayorOfMedieval.Building;
using MayorOfMedieval.Character;
using MayorOfMedieval.Core;
using MayorOfMedieval.Economy;
using MayorOfMedieval.Environment;
using UnityEngine;

namespace MayorOfMedieval.NPC
{
    /// <summary>
    /// A hired villager. What it does depends on the role it was hired into:
    ///  - Harvester: world node -> home stockpile
    ///  - Carrier:   home stockpile -> the nearest shop shelf that still has room
    ///  - Producer:  fetches recipe inputs from depots -> the workshop's input piles
    ///  - GoldCollector: sweeps the till of every shop in turn
    /// </summary>
    [RequireComponent(typeof(CarrySystem))]
    public class Worker : MonoBehaviour
    {
        private enum State { Fetch, Gather, Deliver }

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3.1f;
        [SerializeField] private float turnSpeed = 12f;

        [Header("Work")]
        [SerializeField] private float nodeStandDistance = 1.5f;
        [SerializeField] private float depositDistance = 1.6f;
        [SerializeField] private float searchInterval = 0.6f;
        [SerializeField] private float transferInterval = 0.15f;

        public WorkerRole Role { get; private set; } = WorkerRole.Harvester;

        private ResourceType cargoType = ResourceType.Wood;
        private Stockpile homePile;
        private ProductionBuilding workshop;
        private CarrySystem carry;
        private CarrierBeacon beacon;

        private State state = State.Fetch;
        private HarvestNode targetNode;
        private Stockpile sourcePile;
        private Stockpile destinationPile;
        private SalesPoint targetShop;
        private SalesPoint tillTarget;

        private float searchTimer;
        private float transferTimer;
        private Vector3 idleAnchor;

        /// <summary>Harvester / Carrier setup.</summary>
        public void Configure(WorkerRole role, ResourceType type, Stockpile home)
        {
            Role = role;
            cargoType = type;
            homePile = home;
            idleAnchor = transform.position;
            ApplyHarvestFilter();
        }

        /// <summary>Producer setup — keeps a workshop's ingredient piles topped up.</summary>
        public void ConfigureProducer(ProductionBuilding target)
        {
            Role = WorkerRole.Producer;
            workshop = target;
            idleAnchor = transform.position;
            ApplyHarvestFilter();
        }

        /// <summary>Gold collector setup.</summary>
        public void ConfigureCollector()
        {
            Role = WorkerRole.GoldCollector;
            idleAnchor = transform.position;
            ApplyHarvestFilter();
        }

        /// <summary>
        /// Only a Harvester should ever be auto-loaded by a world node, and only with the
        /// good it was hired for. Without this a courier walking past a forest ends up with
        /// a backpack full of logs and can never pick up what it was actually sent for.
        /// </summary>
        private void ApplyHarvestFilter()
        {
            if (beacon == null) beacon = GetComponent<CarrierBeacon>();
            if (beacon == null) return;

            if (Role == WorkerRole.Harvester)
            {
                ResourceType wanted = cargoType;
                beacon.Accepts = type => type == wanted;
            }
            else
            {
                beacon.Accepts = type => false;
            }
        }

        private void Awake()
        {
            carry = GetComponent<CarrySystem>();
            beacon = GetComponent<CarrierBeacon>();
            if (beacon == null) beacon = gameObject.AddComponent<CarrierBeacon>();
            ApplyHarvestFilter();
        }

        private void Update()
        {
            switch (Role)
            {
                case WorkerRole.Harvester: TickHarvester(); break;
                case WorkerRole.Carrier: TickCarrier(); break;
                case WorkerRole.Producer: TickProducer(); break;
                case WorkerRole.GoldCollector: TickGoldCollector(); break;
            }
        }

        // -------------------------------------------------------------- harvester

        private void TickHarvester()
        {
            if (state == State.Deliver || carry.IsFull)
            {
                state = State.Deliver;
                DeliverTo(homePile, cargoType);
                return;
            }

            searchTimer -= Time.deltaTime;
            if (targetNode == null || !targetNode.IsAvailable)
            {
                if (searchTimer > 0f) { MoveToward(idleAnchor, 0.4f); return; }
                searchTimer = searchInterval;
                targetNode = FindNearestNode();
            }

            if (targetNode == null) return;

            // The node itself pushes units into our carry stack while we stand in range.
            MoveToward(targetNode.Position, nodeStandDistance);
        }

        // ---------------------------------------------------------------- carrier

        private void TickCarrier()
        {
            if (state == State.Deliver)
            {
                if (carry.CountOf(cargoType) <= 0)
                {
                    state = State.Fetch;
                    targetShop = null;
                    return;
                }

                if (targetShop == null || !targetShop.ShelfHasRoom(cargoType))
                {
                    targetShop = FindShopNeeding(cargoType);
                    if (targetShop == null) { MoveToward(idleAnchor, 0.5f); return; }
                }

                if (!MoveToward(targetShop.transform.position, depositDistance)) return;

                transferTimer -= Time.deltaTime;
                if (transferTimer > 0f) return;
                transferTimer = transferInterval;

                if (carry.TryRemove(cargoType))
                {
                    if (!targetShop.TryStock(cargoType)) carry.TryAdd(cargoType); // shelf filled up mid-drop
                }
                return;
            }

            // Fetching: pull from our home pile until we're loaded up.
            if (carry.IsFull) { state = State.Deliver; return; }

            if (homePile == null) return;

            if (!MoveToward(homePile.transform.position, depositDistance)) return;

            // Never dip into the pile's production reserve — the Barracks/Inn/Blacksmith
            // downstream of it would otherwise be starved by the shop run.
            if (homePile.AvailableToCarry <= 0)
            {
                if (carry.CountOf(cargoType) > 0) state = State.Deliver;
                return;
            }

            transferTimer -= Time.deltaTime;
            if (transferTimer > 0f) return;
            transferTimer = transferInterval;

            if (carry.TryAdd(cargoType)) homePile.Remove(1);
        }

        // --------------------------------------------------------------- producer

        private void TickProducer()
        {
            if (workshop == null) return;

            if (state == State.Deliver)
            {
                if (destinationPile == null || carry.CountOf(cargoType) <= 0)
                {
                    state = State.Fetch;
                    destinationPile = null;
                    sourcePile = null;
                    return;
                }

                if (!MoveToward(destinationPile.transform.position, depositDistance)) return;

                transferTimer -= Time.deltaTime;
                if (transferTimer > 0f) return;
                transferTimer = transferInterval;

                if (destinationPile.IsFull) { state = State.Fetch; return; }
                if (carry.TryRemove(cargoType)) destinationPile.Add(1);
                return;
            }

            // Pick whichever ingredient the workshop is shortest on.
            if (sourcePile == null)
            {
                searchTimer -= Time.deltaTime;
                if (searchTimer > 0f) { MoveToward(idleAnchor, 0.5f); return; }
                searchTimer = searchInterval;

                ProductionBuilding.Ingredient need = workshop.NeediestInput();
                if (need == null) { MoveToward(idleAnchor, 0.5f); return; }

                Stockpile source = Stockpile.FindSource(need.type, transform.position);
                if (source == null) { MoveToward(idleAnchor, 0.5f); return; }

                cargoType = need.type;
                sourcePile = source;
                destinationPile = need.pile;
                return;
            }

            if (carry.IsFull || sourcePile.IsEmpty)
            {
                if (carry.CountOf(cargoType) > 0) { state = State.Deliver; sourcePile = null; }
                else sourcePile = null;
                return;
            }

            if (!MoveToward(sourcePile.transform.position, depositDistance)) return;

            transferTimer -= Time.deltaTime;
            if (transferTimer > 0f) return;
            transferTimer = transferInterval;

            if (carry.TryAdd(cargoType)) sourcePile.Remove(1);
        }

        // ---------------------------------------------------------- gold collector

        private void TickGoldCollector()
        {
            if (tillTarget == null || tillTarget.PendingGold <= 0)
            {
                searchTimer -= Time.deltaTime;
                if (searchTimer > 0f) { MoveToward(idleAnchor, 0.5f); return; }
                searchTimer = searchInterval;
                tillTarget = FindRichestTill();
                if (tillTarget == null) { MoveToward(idleAnchor, 0.5f); return; }
            }

            if (!MoveToward(tillTarget.transform.position, depositDistance)) return;

            tillTarget.CollectInto(transform);
            tillTarget = null;
        }

        private static SalesPoint FindRichestTill()
        {
            SalesPoint best = null;
            int mostGold = 0;

            for (int i = 0; i < SalesPoint.All.Count; i++)
            {
                SalesPoint shop = SalesPoint.All[i];
                if (shop == null || shop.PendingGold <= mostGold) continue;
                mostGold = shop.PendingGold;
                best = shop;
            }
            return best;
        }

        // ----------------------------------------------------------------- shared

        private void DeliverTo(Stockpile pile, ResourceType type)
        {
            if (pile == null) { state = State.Fetch; return; }

            if (!MoveToward(pile.transform.position, depositDistance)) return;

            if (carry.CountOf(type) <= 0 || pile.IsFull)
            {
                state = State.Fetch;
                targetNode = null;
                return;
            }

            transferTimer -= Time.deltaTime;
            if (transferTimer > 0f) return;
            transferTimer = transferInterval;

            if (carry.TryRemove(type)) pile.Add(1);
        }

        private SalesPoint FindShopNeeding(ResourceType type)
        {
            SalesPoint best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < SalesPoint.All.Count; i++)
            {
                SalesPoint shop = SalesPoint.All[i];
                if (shop == null || !shop.ShelfHasRoom(type)) continue;

                float distance = Vector3.SqrMagnitude(shop.transform.position - transform.position);
                if (distance >= bestDistance) continue;

                bestDistance = distance;
                best = shop;
            }
            return best;
        }

        private HarvestNode FindNearestNode()
        {
            HarvestNode best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < HarvestNode.All.Count; i++)
            {
                HarvestNode node = HarvestNode.All[i];
                if (node == null || !node.IsAvailable) continue;
                if (node.ResourceType != cargoType) continue;

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
