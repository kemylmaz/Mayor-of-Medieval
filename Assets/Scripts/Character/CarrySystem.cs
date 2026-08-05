using System.Collections.Generic;
using MayorOfMedieval.Core;
using MayorOfMedieval.Economy;
using UnityEngine;

namespace MayorOfMedieval.Character
{
    /// <summary>
    /// The visible stack of goods carried above a character's head. Used by both the
    /// Lord and the hired workers. Items are strictly ordered so the visual stack and
    /// the logical contents can never disagree.
    /// </summary>
    public class CarrySystem : MonoBehaviour
    {
        [SerializeField] private Transform carryPoint;
        [SerializeField] private int capacity = GameConfig.PlayerCarryCapacity;
        [SerializeField] private float stackStep = 0.28f;
        [SerializeField] private Vector3 itemScale = new Vector3(0.42f, 0.24f, 0.42f);
        [SerializeField] private float settleSpeed = 14f;

        private readonly List<ResourceType> contents = new List<ResourceType>();
        private readonly List<Transform> visuals = new List<Transform>();
        private static Material sharedItemMaterial;

        public int Count => contents.Count;
        public int Capacity => capacity;
        public bool IsFull => contents.Count >= capacity;
        public bool IsEmpty => contents.Count == 0;

        /// <summary>Type currently on top, or null when empty.</summary>
        public ResourceType? TopType => contents.Count > 0 ? contents[contents.Count - 1] : (ResourceType?)null;

        private void Awake()
        {
            if (carryPoint == null)
            {
                GameObject point = new GameObject("CarryPoint");
                point.transform.SetParent(transform, false);
                point.transform.localPosition = new Vector3(0f, 1.15f, 0f);
                carryPoint = point.transform;
            }
        }

        private void Update()
        {
            // Ease each item toward its slot height so pickups visibly "stack up".
            for (int i = 0; i < visuals.Count; i++)
            {
                if (visuals[i] == null) continue;
                Vector3 target = new Vector3(0f, i * stackStep, 0f);
                visuals[i].localPosition = Vector3.Lerp(visuals[i].localPosition, target, settleSpeed * Time.deltaTime);
            }
        }

        public int CountOf(ResourceType type)
        {
            int n = 0;
            for (int i = 0; i < contents.Count; i++) if (contents[i] == type) n++;
            return n;
        }

        public bool TryAdd(ResourceType type)
        {
            if (IsFull) return false;

            contents.Add(type);
            visuals.Add(CreateVisual(type, contents.Count - 1));
            return true;
        }

        /// <summary>Removes one item of the given type (topmost first). False if none held.</summary>
        public bool TryRemove(ResourceType type)
        {
            for (int i = contents.Count - 1; i >= 0; i--)
            {
                if (contents[i] != type) continue;

                contents.RemoveAt(i);
                if (visuals[i] != null) Destroy(visuals[i].gameObject);
                visuals.RemoveAt(i);
                return true;
            }
            return false;
        }

        public void Clear()
        {
            for (int i = 0; i < visuals.Count; i++)
            {
                if (visuals[i] != null) Destroy(visuals[i].gameObject);
            }
            visuals.Clear();
            contents.Clear();
        }

        private Transform CreateVisual(ResourceType type, int index)
        {
            GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
            item.name = "Carried_" + type;

            Collider col = item.GetComponent<Collider>();
            if (col != null) Destroy(col);

            item.transform.SetParent(carryPoint, false);
            item.transform.localScale = itemScale;
            // Spawn slightly below its slot so it eases upward into place.
            item.transform.localPosition = new Vector3(0f, Mathf.Max(0f, index - 1) * stackStep, 0f);
            item.transform.localRotation = Quaternion.Euler(0f, Random.Range(-8f, 8f), 0f);

            Renderer renderer = item.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (sharedItemMaterial == null)
                {
                    Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                    sharedItemMaterial = new Material(shader);
                }
                // A per-renderer property block keeps this to one material, many colors.
                renderer.sharedMaterial = sharedItemMaterial;
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                Color color = GameConfig.ColorOf(type);
                block.SetColor("_BaseColor", color);
                block.SetColor("_Color", color);
                renderer.SetPropertyBlock(block);
            }

            return item.transform;
        }
    }
}
