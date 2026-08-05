using UnityEngine;

namespace MayorOfMedieval.Character
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float gravity = -20f;

        [Header("References")]
        [SerializeField] private Animator animator;

        private CharacterController characterController;
        private Vector3 velocity;
        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        // Isometric rotation matrix (45 degrees)
        private readonly float isoAngle = 45f;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            // Ensure CharacterController settings work on flat ground
            characterController.minMoveDistance = 0f;
            characterController.skinWidth = 0.08f;
            characterController.stepOffset = 0.3f;
        }

        private void Start()
        {
            // Snap to ground on start
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up * 5f, Vector3.down, out hit, 20f))
            {
                transform.position = hit.point + Vector3.up * (characterController.height * 0.5f + characterController.skinWidth);
            }
        }

        private void Update()
        {
            HandleMovement();
            ApplyGravity();
        }

        private void HandleMovement()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            Vector3 input = new Vector3(horizontal, 0f, vertical).normalized;

            if (input.magnitude >= 0.1f)
            {
                // Rotate input for isometric camera
                float rad = isoAngle * Mathf.Deg2Rad;
                float cos = Mathf.Cos(rad);
                float sin = Mathf.Sin(rad);
                Vector3 isoDirection = new Vector3(
                    input.x * cos - input.z * sin,
                    0f,
                    input.x * sin + input.z * cos
                );

                characterController.Move(isoDirection * moveSpeed * Time.deltaTime);

                // Smooth rotation to face movement direction
                Quaternion targetRotation = Quaternion.LookRotation(isoDirection);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }

            // Update animator
            if (animator != null)
            {
                animator.SetFloat(SpeedHash, input.magnitude);
            }
        }

        private void ApplyGravity()
        {
            if (characterController.isGrounded)
            {
                // Keep a small downward velocity to maintain ground contact
                velocity.y = -2f;
            }
            else
            {
                velocity.y += gravity * Time.deltaTime;
                // Clamp fall speed to prevent tunneling through ground
                velocity.y = Mathf.Max(velocity.y, -30f);
            }

            characterController.Move(velocity * Time.deltaTime);

            // Safety: if fallen below world, teleport back to surface
            if (transform.position.y < -5f)
            {
                characterController.enabled = false;
                transform.position = new Vector3(transform.position.x, 1f, transform.position.z);
                velocity.y = 0f;
                characterController.enabled = true;
            }
        }
    }
}