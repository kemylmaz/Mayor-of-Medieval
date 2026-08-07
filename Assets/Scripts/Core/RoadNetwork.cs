using System.Collections.Generic;
using UnityEngine;

namespace MayorOfMedieval.Core
{
    /// <summary>
    /// Lays a paved path from each newly finished building to the nearest one already
    /// standing. By the end of a run every building is joined up, so the village grows
    /// into a connected town instead of a field of unrelated huts.
    /// </summary>
    public class RoadNetwork : MonoBehaviour
    {
        public static RoadNetwork Instance { get; private set; }

        [SerializeField] private GameObject roadPiece;
        [Tooltip("Spacing between paving tiles along a path.")]
        [SerializeField] private float tileStep = 1f;
        [Tooltip("Stop this far short of each end so tiles don't sink into the buildings.")]
        [SerializeField] private float endPadding = 2.2f;

        private readonly List<Vector3> hubs = new List<Vector3>();
        private Transform container;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;

            GameObject holder = new GameObject("Roads");
            holder.transform.SetParent(transform, false);
            container = holder.transform;
        }

        /// <summary>Called when a building finishes. Paves a road to its nearest neighbour.</summary>
        public void Connect(Vector3 position)
        {
            Vector3 flat = new Vector3(position.x, 0f, position.z);

            Vector3 nearest = Vector3.zero;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < hubs.Count; i++)
            {
                float d = Vector3.SqrMagnitude(hubs[i] - flat);
                if (d >= bestDistance) continue;
                bestDistance = d;
                nearest = hubs[i];
            }

            // First building has nothing to link to yet; it just seeds the network.
            if (hubs.Count > 0) Pave(nearest, flat);
            hubs.Add(flat);
        }

        private void Pave(Vector3 from, Vector3 to)
        {
            if (roadPiece == null) return;

            Vector3 delta = to - from;
            float length = delta.magnitude;
            if (length <= endPadding * 2f) return;

            Vector3 dir = delta / length;
            float yaw = Quaternion.LookRotation(dir).eulerAngles.y;

            for (float travelled = endPadding; travelled <= length - endPadding; travelled += tileStep)
            {
                GameObject tile = Instantiate(roadPiece, from + dir * travelled, Quaternion.Euler(0f, yaw, 0f), container);
                tile.name = "RoadTile";
                // Sit a hair above the ground so the paving never z-fights with the grass.
                tile.transform.position += Vector3.up * 0.012f;
            }
        }
    }
}
