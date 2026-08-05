using MayorOfMedieval.Core;
using MayorOfMedieval.Economy;
using UnityEngine;

namespace MayorOfMedieval.Building
{
    /// <summary>
    /// Fully automatic grain production — but the crops only grow once the Well is dug,
    /// which is why the Well pad appears alongside the field.
    /// </summary>
    public class CropField : MonoBehaviour
    {
        [Header("Production")]
        [SerializeField] private Stockpile output;
        [SerializeField] private float secondsPerGrain = 3.5f;

        [Header("Dry State")]
        [Tooltip("Shown while the Well has not been built yet.")]
        [SerializeField] private GameObject dryOverlay;
        [SerializeField] private Transform[] cropVisuals;

        private float timer;
        private bool wasIrrigated;

        private bool IsIrrigated => GameProgression.Instance != null && GameProgression.Instance.IsBuilt(BuildingKind.Well);

        private void Start()
        {
            wasIrrigated = !IsIrrigated; // force a refresh on the first frame
            ApplyIrrigationVisual();
        }

        private void Update()
        {
            if (wasIrrigated != IsIrrigated) ApplyIrrigationVisual();

            if (!IsIrrigated || output == null) return;
            if (output.IsFull) return;

            timer += Time.deltaTime;
            if (timer < secondsPerGrain) return;
            timer = 0f;

            output.Add(1);
        }

        private void ApplyIrrigationVisual()
        {
            wasIrrigated = IsIrrigated;

            if (dryOverlay != null) dryOverlay.SetActive(!wasIrrigated);

            for (int i = 0; i < cropVisuals.Length; i++)
            {
                if (cropVisuals[i] != null) cropVisuals[i].gameObject.SetActive(wasIrrigated);
            }
        }
    }
}
