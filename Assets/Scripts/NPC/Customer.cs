using MayorOfMedieval.Building;
using MayorOfMedieval.Character;
using MayorOfMedieval.Core;
using MayorOfMedieval.Economy;
using TMPro;
using UnityEngine;

namespace MayorOfMedieval.NPC
{
    /// <summary>
    /// Walks in, queues at a counter, holds up a request bubble, and pays gold as the
    /// Lord hands goods over one at a time. Leaves once satisfied (or once patience runs out).
    /// </summary>
    public class Customer : MonoBehaviour
    {
        private enum State { WalkingToQueue, Waiting, Leaving }

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 2.6f;
        [SerializeField] private float turnSpeed = 12f;
        [SerializeField] private float arriveDistance = 0.25f;

        [Header("Service")]
        [SerializeField] private float deliverRadius = 2.4f;
        [SerializeField] private float deliverInterval = 0.18f;

        [Header("Bubble")]
        [SerializeField] private Vector3 bubbleOffset = new Vector3(0f, 2.05f, 0f);

        public ResourceType Wanted { get; private set; }
        public int Remaining { get; private set; }

        private State state = State.WalkingToQueue;
        private ServiceCounter counter;
        private SalesPoint shop;
        private Vector3 exitPoint;
        private float deliverTimer;

        private Transform bubbleRoot;
        private TextMeshPro bubbleLabel;
        private Renderer bubbleIcon;
        private static Material sharedBubbleMaterial;

        public void Initialise(ServiceCounter targetCounter, ResourceType wanted, int amount, Vector3 exit)
        {
            counter = targetCounter;
            Wanted = wanted;
            Remaining = Mathf.Max(1, amount);
            exitPoint = exit;

            // The shop front owning this counter is what holds the shelves and the till.
            if (counter != null) shop = counter.GetComponentInParent<SalesPoint>();

            BuildBubble();
            RefreshBubble();

            if (counter == null || counter.Join(this) < 0)
            {
                // No room after all — turn around immediately rather than idling forever.
                state = State.Leaving;
                counter = null;
            }
        }

        private void Update()
        {
            switch (state)
            {
                case State.WalkingToQueue: TickWalking(); break;
                case State.Waiting: TickWaiting(); break;
                case State.Leaving: TickLeaving(); break;
            }

            if (bubbleRoot != null && Camera.main != null)
            {
                bubbleRoot.position = transform.position + bubbleOffset;
                bubbleRoot.rotation = Camera.main.transform.rotation;
            }
        }

        private void TickWalking()
        {
            if (counter == null) { state = State.Leaving; return; }

            int index = counter.IndexOf(this);
            if (index < 0) { state = State.Leaving; return; }

            Vector3 target = counter.SlotPosition(index);
            if (MoveToward(target))
            {
                state = State.Waiting;
            }
        }

        private void TickWaiting()
        {
            if (counter == null) { state = State.Leaving; return; }

            // Keep shuffling forward as the line advances.
            int index = counter.IndexOf(this);
            if (index >= 0)
            {
                Vector3 slot = counter.SlotPosition(index);
                if (Vector3.Distance(transform.position, slot) > arriveDistance) MoveToward(slot);
                else FaceDirection(counter.FacingFromQueue);
            }

            deliverTimer -= Time.deltaTime;
            if (deliverTimer > 0f) return;

            // Preferred path: serve themselves off the shop shelf, dropping payment in the
            // till. This is what makes stocking a shop worthwhile while the Lord is away.
            if (shop != null && shop.StockOf(Wanted) > 0)
            {
                deliverTimer = deliverInterval;

                int paid;
                if (!shop.TryBuyOne(Wanted, out paid)) return;

                Remaining--;
                RefreshBubble();
                if (Remaining <= 0) Complete();
                return;
            }

            // Fallback: the Lord hands goods over in person. Payment still lands in the till.
            CarrySystem player = PlayerRef.Carry;
            if (player == null) return;
            if (Vector3.Distance(player.transform.position, transform.position) > deliverRadius) return;
            if (player.CountOf(Wanted) <= 0) return;

            deliverTimer = deliverInterval;

            if (!player.TryRemove(Wanted)) return;

            Remaining--;
            int payout = GameConfig.SellPrice(Wanted);
            if (shop != null) shop.AddGold(payout);
            else if (ResourceManager.Instance != null) ResourceManager.Instance.AddResource(ResourceType.Gold, payout);

            RefreshBubble();

            if (Remaining <= 0) Complete();
        }

        private void Complete()
        {
            if (counter != null)
            {
                counter.Leave(this);
                counter = null;
            }
            if (bubbleRoot != null) Destroy(bubbleRoot.gameObject);
            GameProgression.NotifyOrderCompleted(Wanted);
            state = State.Leaving;
        }

        private void TickLeaving()
        {
            if (MoveToward(exitPoint)) Destroy(gameObject);
        }

        private bool MoveToward(Vector3 target)
        {
            Vector3 delta = target - transform.position;
            delta.y = 0f;

            if (delta.magnitude <= arriveDistance) return true;

            Vector3 dir = delta.normalized;
            transform.position += dir * moveSpeed * Time.deltaTime;
            FaceDirection(dir);
            return false;
        }

        private void FaceDirection(Vector3 dir)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (counter != null) counter.Leave(this);
            if (bubbleRoot != null) Destroy(bubbleRoot.gameObject);
        }

        private void BuildBubble()
        {
            GameObject root = new GameObject("OrderBubble");
            bubbleRoot = root.transform;

            GameObject icon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            icon.name = "Icon";
            Collider iconCol = icon.GetComponent<Collider>();
            if (iconCol != null) Destroy(iconCol);
            icon.transform.SetParent(bubbleRoot, false);
            icon.transform.localPosition = new Vector3(-0.28f, 0f, 0f);
            icon.transform.localScale = new Vector3(0.34f, 0.34f, 0.06f);
            bubbleIcon = icon.GetComponent<Renderer>();

            if (bubbleIcon != null)
            {
                if (sharedBubbleMaterial == null)
                {
                    Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                    sharedBubbleMaterial = new Material(shader);
                }
                bubbleIcon.sharedMaterial = sharedBubbleMaterial;
            }

            GameObject labelGo = new GameObject("Label");
            labelGo.transform.SetParent(bubbleRoot, false);
            bubbleLabel = labelGo.AddComponent<TextMeshPro>();
            bubbleLabel.font = TMP_Settings.defaultFontAsset;
            bubbleLabel.fontSize = 4.5f;
            bubbleLabel.fontStyle = FontStyles.Bold;
            bubbleLabel.alignment = TextAlignmentOptions.Left;
            bubbleLabel.color = Color.white;
            bubbleLabel.rectTransform.sizeDelta = new Vector2(1.6f, 0.6f);
            bubbleLabel.rectTransform.localPosition = new Vector3(0.36f, 0f, 0f);
        }

        private void RefreshBubble()
        {
            if (bubbleLabel != null) bubbleLabel.SetText("x{0}", Remaining);

            if (bubbleIcon != null)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                bubbleIcon.GetPropertyBlock(block);
                Color color = GameConfig.ColorOf(Wanted);
                block.SetColor("_BaseColor", color);
                block.SetColor("_Color", color);
                bubbleIcon.SetPropertyBlock(block);
            }
        }
    }
}
