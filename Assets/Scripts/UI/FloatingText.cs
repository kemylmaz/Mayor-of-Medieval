using TMPro;
using UnityEngine;

namespace MayorOfMedieval.UI
{
    /// <summary>Short-lived world-space "+25" pop that drifts up and fades out.</summary>
    public class FloatingText : MonoBehaviour
    {
        [SerializeField] private float lifetime = 0.9f;
        [SerializeField] private float riseSpeed = 1.6f;

        private TextMeshPro label;
        private float elapsed;
        private Color baseColor;

        public static FloatingText Spawn(Vector3 worldPosition, string text, Color color)
        {
            GameObject go = new GameObject("FloatingText");
            go.transform.position = worldPosition;

            FloatingText floating = go.AddComponent<FloatingText>();
            floating.Setup(text, color);
            return floating;
        }

        private void Setup(string text, Color color)
        {
            label = gameObject.AddComponent<TextMeshPro>();
            label.font = TMP_Settings.defaultFontAsset;
            label.text = text;
            label.fontSize = 5.5f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = color;
            label.rectTransform.sizeDelta = new Vector2(4f, 1f);

            baseColor = color;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;

            transform.position += Vector3.up * riseSpeed * Time.deltaTime;
            if (Camera.main != null) transform.rotation = Camera.main.transform.rotation;

            if (label != null)
            {
                float t = Mathf.Clamp01(elapsed / lifetime);
                label.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f - t);
            }

            if (elapsed >= lifetime) Destroy(gameObject);
        }
    }
}
