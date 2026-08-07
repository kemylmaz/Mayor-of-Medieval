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
        [Tooltip("Stop this far short of the building so tiles don't sink into its walls.")]
        [SerializeField] private float endPadding = 2.6f;
        [Tooltip("Leave the plaza itself clear — spokes start outside it.")]
        [SerializeField] private float centrePadding = 3.2f;
        [Tooltip("Village centre every road radiates from.")]
        [SerializeField] private Vector3 hubCentre = Vector3.zero;

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

        /// <summary>
        /// Called when a building finishes. Every road is a straight spoke from the village
        /// centre, so the network comes out as a clean wheel. Chaining each building to its
        /// nearest neighbour instead produced a crooked zig-zag that never looked planned.
        /// </summary>
        public void Connect(Vector3 position)
        {
            Vector3 flat = new Vector3(position.x, 0f, position.z);
            if (hubs.Contains(flat)) return;

            Pave(hubCentre, flat);
            hubs.Add(flat);
        }

        /// <summary>Straight run of tiles, each aligned to the direction of travel.</summary>
        private void Pave(Vector3 from, Vector3 to)
        {
            if (roadPiece == null) return;

            Vector3 delta = to - from;
            float length = delta.magnitude;
            if (length <= endPadding + centrePadding) return;

            Vector3 dir = delta / length;
            float yaw = Quaternion.LookRotation(dir).eulerAngles.y;

            // Snap tile spacing so the run divides evenly and ends flush with the building
            // instead of stopping at a ragged half-tile.
            float span = length - endPadding - centrePadding;
            int count = Mathf.Max(1, Mathf.RoundToInt(span / tileStep));
            float step = span / count;

            for (int i = 0; i <= count; i++)
            {
                Vector3 spot = from + dir * (centrePadding + step * i);
                GameObject tile = Instantiate(roadPiece, spot, Quaternion.Euler(0f, yaw, 0f), container);
                tile.name = "RoadTile";
                // Sit a hair above the ground so the paving never z-fights with the grass.
                tile.transform.position += Vector3.up * 0.012f;
            }
        }
    }
}
