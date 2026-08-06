using UnityEngine;

namespace MayorOfMedieval.Core
{
    /// <summary>
    /// Keeps the fixed isometric angle but rides along with the Lord, close in like the
    /// reference playables. A static wide shot makes the character a dot; this keeps them
    /// large and centred while still showing the shop they're walking toward.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private string targetTag = "Player";

        [Header("Framing")]
        [Tooltip("Offset from the target, in the isometric direction.")]
        [SerializeField] private Vector3 offset = new Vector3(11f, 15f, -11f);
        [SerializeField] private float orthographicSize = 7f;
        [Tooltip("Nudges the view ahead of the Lord so there is more room to see where they are going.")]
        [SerializeField] private float lookAhead = 1.2f;

        [Header("Smoothing")]
        [SerializeField] private float followSmoothing = 8f;

        private Camera cam;
        private Vector3 velocity;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = orthographicSize;
        }

        private void Start()
        {
            Resolve();
            if (target != null) transform.position = DesiredPosition();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                Resolve();
                if (target == null) return;
            }

            transform.position = Vector3.SmoothDamp(transform.position, DesiredPosition(),
                ref velocity, 1f / Mathf.Max(0.01f, followSmoothing));

            if (!Mathf.Approximately(cam.orthographicSize, orthographicSize))
                cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, orthographicSize, 4f * Time.deltaTime);
        }

        private Vector3 DesiredPosition()
        {
            // Shift the framing slightly along the Lord's facing so they sit a touch
            // behind centre rather than dead centre.
            Vector3 ahead = target.forward * lookAhead;
            ahead.y = 0f;
            return target.position + ahead + offset;
        }

        private void Resolve()
        {
            if (target != null) return;
            GameObject player = GameObject.FindGameObjectWithTag(targetTag);
            if (player != null) target = player.transform;
        }
    }
}
