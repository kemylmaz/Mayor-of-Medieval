using MayorOfMedieval.Building;
using MayorOfMedieval.Core;
using UnityEngine;

namespace MayorOfMedieval.UI
{
    /// <summary>
    /// The guidance arrow. It rides just in front of the Lord and points at whatever the
    /// current quest step wants — with the camera pulled in close, an arrow parked over a
    /// far-off building is usually off-screen, so it has to travel with the player.
    /// </summary>
    public class QuestArrow : MonoBehaviour
    {
        [SerializeField] private GameObject arrowVisual;

        [Header("Placement")]
        [Tooltip("How far in front of the Lord the arrow floats.")]
        [SerializeField] private float leadDistance = 1.9f;
        [SerializeField] private float hoverHeight = 0.35f;
        [SerializeField] private float bobHeight = 0.18f;
        [SerializeField] private float bobSpeed = 4f;
        [SerializeField] private float followSpeed = 12f;

        [Tooltip("Hide once the Lord is basically on top of the objective.")]
        [SerializeField] private float hideDistance = 3.5f;

        private Transform target;

        private void Update()
        {
            target = GameProgression.Instance != null ? GameProgression.Instance.CurrentTarget : null;
            Transform player = PlayerRef.Root;

            bool show = target != null && player != null;
            if (show && Vector3.Distance(player.position, target.position) < hideDistance) show = false;

            if (arrowVisual != null && arrowVisual.activeSelf != show) arrowVisual.SetActive(show);
            if (!show) return;

            Vector3 toTarget = target.position - player.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.01f) return;
            Vector3 dir = toTarget.normalized;

            // Sit ahead of the Lord along the route, not on top of the destination.
            float bob = Mathf.Abs(Mathf.Sin(Time.time * bobSpeed)) * bobHeight;
            Vector3 desired = player.position + dir * leadDistance + Vector3.up * (hoverHeight + bob);

            transform.position = Vector3.Lerp(transform.position, desired, followSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(dir), followSpeed * Time.deltaTime);
        }
    }
}
