using UnityEngine;

namespace MayorOfMedieval.Building
{
    /// <summary>
    /// Refills its bucket pile with water on a timer. Acts both as the irrigation flag the
    /// CropField checks and as the water depot the Mill/Inn producers draw from.
    /// </summary>
    public class WaterWell : MonoBehaviour
    {
        [SerializeField] private Stockpile waterPile;
        [SerializeField] private float secondsPerBucket = 1.6f;

        private float timer;

        private void Update()
        {
            if (waterPile == null || waterPile.IsFull) return;

            timer += Time.deltaTime;
            if (timer < secondsPerBucket) return;
            timer = 0f;

            waterPile.Add(1);
        }
    }
}
