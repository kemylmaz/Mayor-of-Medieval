using MayorOfMedieval.Building;
using MayorOfMedieval.Core;
using UnityEngine;

namespace MayorOfMedieval.UI
{
    /// <summary>
    /// The big green arrow that hovers over whatever the player should walk to next.
    /// Follows GameProgression's current target and hides itself when there isn't one.
    /// </summary>
    public class QuestArrow : MonoBehaviour
    {
        [SerializeField] private GameObject arrowVisual;
        [SerializeField] private float hoverHeight = 2.6f;
        [SerializeField] private float bounceHeight = 0.4f;
        [SerializeField] private float bounceSpeed = 3.2f;
        [SerializeField] private float followSpeed = 8f;
        [Tooltip("Hide the arrow once the Lord is this close to the target.")]
        [SerializeField] private float hideDistance = 3f;

        private Transform target;

        private void Update()
        {
            target = GameProgression.Instance != null ? GameProgression.Instance.CurrentTarget : null;

            bool show = target != null;
            if (show)
            {
                Transform player = PlayerRef.Root;
                if (player != null && Vector3.Distance(player.position, target.position) < hideDistance) show = false;
            }

            if (arrowVisual != null && arrowVisual.activeSelf != show) arrowVisual.SetActive(show);
            if (!show) return;

            float bounce = Mathf.Abs(Mathf.Sin(Time.time * bounceSpeed)) * bounceHeight;
            Vector3 desired = target.position + Vector3.up * (hoverHeight + bounce);
            transform.position = Vector3.Lerp(transform.position, desired, followSpeed * Time.deltaTime);
        }
    }
}
