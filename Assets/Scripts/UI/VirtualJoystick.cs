using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MayorOfMedieval.UI
{
    /// <summary>
    /// Floating touch stick for the left half of the screen. It appears wherever the thumb
    /// lands rather than sitting in a fixed corner, which is what mobile playables do —
    /// a fixed stick forces the player to look down and hunt for it.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public static VirtualJoystick Instance { get; private set; }

        [SerializeField] private RectTransform ring;
        [SerializeField] private RectTransform knob;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private float radius = 110f;

        /// <summary>-1..1 on each axis, zero when untouched.</summary>
        public Vector2 Direction { get; private set; }
        public bool IsHeld { get; private set; }

        private Canvas canvas;
        private int activePointer = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        private void Awake()
        {
            Instance = this;
            canvas = GetComponentInParent<Canvas>();
            if (group == null) group = GetComponent<CanvasGroup>();
            Hide();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (IsHeld) return;

            activePointer = eventData.pointerId;
            IsHeld = true;

            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)transform, eventData.position, EventCamera, out local);

            if (ring != null)
            {
                ring.anchoredPosition = local;
                ring.gameObject.SetActive(true);
            }
            if (knob != null) knob.anchoredPosition = local;
            if (group != null) group.alpha = 1f;

            Direction = Vector2.zero;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsHeld || eventData.pointerId != activePointer || ring == null) return;

            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)transform, eventData.position, EventCamera, out local);

            Vector2 offset = local - ring.anchoredPosition;
            Vector2 clamped = Vector2.ClampMagnitude(offset, radius);

            if (knob != null) knob.anchoredPosition = ring.anchoredPosition + clamped;
            Direction = clamped / radius;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointer) return;
            Hide();
        }

        private void Hide()
        {
            IsHeld = false;
            activePointer = -1;
            Direction = Vector2.zero;
            if (ring != null) ring.gameObject.SetActive(false);
            if (group != null) group.alpha = 0f;
        }

        private Camera EventCamera =>
            canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
    }
}
