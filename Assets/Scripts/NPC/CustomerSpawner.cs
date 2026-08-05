using System.Collections.Generic;
using MayorOfMedieval.Building;
using MayorOfMedieval.Economy;
using UnityEngine;

namespace MayorOfMedieval.NPC
{
    /// <summary>
    /// Feeds customers into whichever counters are currently open. Orders are drawn only
    /// from goods the player can actually supply, so unlocking the Farm/Field/Mill is what
    /// makes the richer requests start appearing.
    /// </summary>
    public class CustomerSpawner : MonoBehaviour
    {
        public static CustomerSpawner Instance { get; private set; }

        [Header("Spawning")]
        [SerializeField] private GameObject customerPrefab;
        [SerializeField] private float spawnInterval = 4.5f;
        [SerializeField] private int maxActiveCustomers = 6;
        [SerializeField] private Vector2Int orderSizeRange = new Vector2Int(2, 4);

        [Header("Path")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform exitPoint;

        private float timer;
        private readonly List<ResourceType> pool = new List<ResourceType>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            timer = 1.5f;
        }

        private void Update()
        {
            if (customerPrefab == null) return;
            if (ServiceCounter.All.Count == 0) return;

            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = spawnInterval;

            if (CountActive() >= maxActiveCustomers) return;

            ServiceCounter counter = PickCounter();
            if (counter == null) return;

            BuildPool(counter);
            if (pool.Count == 0) return;

            ResourceType wanted = pool[Random.Range(0, pool.Count)];
            int amount = Random.Range(orderSizeRange.x, orderSizeRange.y + 1);

            Vector3 origin = spawnPoint != null ? spawnPoint.position : transform.position;
            Vector3 exit = exitPoint != null ? exitPoint.position : origin;

            GameObject go = Instantiate(customerPrefab, origin, Quaternion.identity, transform);
            go.name = "Customer_" + wanted;

            Customer customer = go.GetComponent<Customer>();
            if (customer == null) customer = go.AddComponent<Customer>();
            customer.Initialise(counter, wanted, amount, exit);
        }

        private int CountActive() => GetComponentsInChildren<Customer>().Length;

        private ServiceCounter PickCounter()
        {
            ServiceCounter best = null;
            int shortest = int.MaxValue;

            for (int i = 0; i < ServiceCounter.All.Count; i++)
            {
                ServiceCounter candidate = ServiceCounter.All[i];
                if (candidate == null || !candidate.HasRoom) continue;
                if (candidate.AcceptedGoods.Count == 0) continue;

                if (candidate.QueueCount >= shortest) continue;
                shortest = candidate.QueueCount;
                best = candidate;
            }
            return best;
        }

        private void BuildPool(ServiceCounter counter)
        {
            pool.Clear();
            for (int i = 0; i < counter.AcceptedGoods.Count; i++)
            {
                pool.Add(counter.AcceptedGoods[i]);
            }
        }
    }
}
