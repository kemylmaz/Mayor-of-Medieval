using MayorOfMedieval.Building;
using MayorOfMedieval.Character;
using UnityEngine;
using UnityEngine.UI;

namespace MayorOfMedieval.UI
{
    /// <summary>
    /// Bottom-right dump button. Without it the Lord can deadlock: a stack full of stone
    /// with a bread order waiting and nowhere to put the stone down. Works with a tap on
    /// mobile and a click on desktop, since it is a plain uGUI Button.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class TrashButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private CanvasGroup group;
        [Tooltip("Dimmed instead of hidden so its position stays learnable.")]
        [SerializeField] private float emptyAlpha = 0.35f;

        private void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            if (group == null) group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();

            button.onClick.AddListener(DumpEverything);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(DumpEverything);
        }

        private void Update()
        {
            CarrySystem carry = PlayerRef.Carry;
            bool carrying = carry != null && !carry.IsEmpty;

            group.alpha = Mathf.Lerp(group.alpha, carrying ? 1f : emptyAlpha, 10f * Time.deltaTime);
            group.interactable = carrying;
            group.blocksRaycasts = carrying;
        }

        public void DumpEverything()
        {
            CarrySystem carry = PlayerRef.Carry;
            if (carry == null || carry.IsEmpty) return;

            int dropped = carry.Count;
            carry.Clear();
            FloatingText.Spawn(carry.transform.position + Vector3.up * 2.4f, "-" + dropped,
                new Color(0.9f, 0.4f, 0.35f));
        }
    }
}
