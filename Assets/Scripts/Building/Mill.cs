using MayorOfMedieval.Economy;
using UnityEngine;

namespace MayorOfMedieval.Building
{
    /// <summary>
    /// Grain in, bread out. The Lord dumps grain on the input pile, the mill grinds it
    /// on a timer, and the bread pile fills up for the (much richer) bread customers.
    /// </summary>
    public class Mill : MonoBehaviour
    {
        [Header("Conversion")]
        [SerializeField] private Stockpile grainInput;
        [SerializeField] private Stockpile breadOutput;
        [SerializeField] private int grainPerBread = 1;
        [SerializeField] private float secondsPerBread = 2.5f;

        [Header("Visual")]
        [Tooltip("Spun while the mill is actively grinding.")]
        [SerializeField] private Transform sails;
        [SerializeField] private float sailSpeed = 90f;

        private float timer;

        public bool IsGrinding { get; private set; }

        private void Update()
        {
            if (grainInput == null || breadOutput == null) return;

            IsGrinding = grainInput.Amount >= grainPerBread && !breadOutput.IsFull;

            if (sails != null && IsGrinding)
            {
                sails.Rotate(Vector3.forward, sailSpeed * Time.deltaTime, Space.Self);
            }

            if (!IsGrinding)
            {
                timer = 0f;
                return;
            }

            timer += Time.deltaTime;
            if (timer < secondsPerBread) return;
            timer = 0f;

            if (grainInput.Remove(grainPerBread) < grainPerBread) return;
            breadOutput.Add(1);
        }
    }
}
