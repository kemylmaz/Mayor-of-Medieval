using MayorOfMedieval.Core;
using MayorOfMedieval.Economy;
using MayorOfMedieval.NPC;
using UnityEngine;

namespace MayorOfMedieval.Building
{
    /// <summary>
    /// The only building with no customers. Swords go in, soldiers come out, and those
    /// soldiers farm the training dummies for gold on their own. Each muster also costs
    /// gold, so the barracks is a pure reinvestment engine.
    /// </summary>
    public class Barracks : MonoBehaviour
    {
        [Header("Muster")]
        [SerializeField] private Stockpile swordInput;
        [SerializeField] private GameObject soldierPrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private int swordsPerSoldier = 1;
        [SerializeField] private int goldPerSoldier = 120;
        [SerializeField] private float secondsPerMuster = 4f;
        [SerializeField] private int maxSoldiers = 6;

        public int SoldierCount { get; private set; }

        private float timer;

        /// <summary>Re-musters a saved garrison with no sword or gold cost.</summary>
        public void RestoreSoldiers(int count)
        {
            if (soldierPrefab == null) return;

            for (int i = SoldierCount; i < Mathf.Min(count, maxSoldiers); i++)
            {
                SoldierCount++;
                Vector3 spawn = spawnPoint != null ? spawnPoint.position : transform.position + Vector3.forward * 2f;
                GameObject go = Instantiate(soldierPrefab, spawn, Quaternion.identity, transform);
                go.name = "Soldier_" + SoldierCount;
                if (go.GetComponent<Soldier>() == null) go.AddComponent<Soldier>();
            }
        }

        private void Update()
        {
            if (swordInput == null || soldierPrefab == null) return;
            if (SoldierCount >= maxSoldiers) return;
            if (swordInput.Amount < swordsPerSoldier) return;

            ResourceManager wallet = ResourceManager.Instance;
            if (wallet == null || wallet.Gold < goldPerSoldier) return;

            timer += Time.deltaTime;
            if (timer < secondsPerMuster) return;
            timer = 0f;

            if (swordInput.Remove(swordsPerSoldier) < swordsPerSoldier) return;
            if (!wallet.SpendResource(ResourceType.Gold, goldPerSoldier)) return;

            SoldierCount++;

            Vector3 spawn = spawnPoint != null ? spawnPoint.position : transform.position + Vector3.forward * 2f;
            GameObject go = Instantiate(soldierPrefab, spawn, Quaternion.identity, transform);
            go.name = "Soldier_" + SoldierCount;
            if (go.GetComponent<Soldier>() == null) go.AddComponent<Soldier>();

            UI.FloatingText.Spawn(spawn + Vector3.up * 2.2f, "Asker +1", new Color(0.6f, 0.75f, 1f));
        }
    }
}
