using UnityEngine;

namespace MayorOfMedieval.NPC
{
    /// <summary>
    /// Mustered at the Barracks. Walks to the nearest standing training dummy, hits it on a
    /// cadence, and moves on to the next one. The gold is paid out by the dummy itself.
    /// </summary>
    public class Soldier : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3.4f;
        [SerializeField] private float turnSpeed = 12f;
        [SerializeField] private float attackRange = 1.5f;

        [Header("Combat")]
        [SerializeField] private float damagePerHit = 20f;
        [SerializeField] private float secondsPerHit = 0.7f;

        private TrainingDummy target;
        private float attackTimer;
        private float searchTimer;
        private Vector3 lungeBase;
        private bool lungeCaptured;

        private void Update()
        {
            if (target == null || !target.IsAlive)
            {
                searchTimer -= Time.deltaTime;
                if (searchTimer <= 0f)
                {
                    searchTimer = 0.5f;
                    target = TrainingDummy.FindNearestAlive(transform.position);
                }
                if (target == null) return;
            }

            Vector3 delta = target.Position - transform.position;
            delta.y = 0f;

            if (delta.magnitude > attackRange)
            {
                Vector3 dir = delta.normalized;
                transform.position += dir * moveSpeed * Time.deltaTime;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);
                return;
            }

            if (delta.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(delta.normalized), turnSpeed * Time.deltaTime);
            }

            if (!lungeCaptured)
            {
                lungeBase = transform.localScale;
                lungeCaptured = true;
            }
            transform.localScale = Vector3.Lerp(transform.localScale, lungeBase, 10f * Time.deltaTime);

            attackTimer -= Time.deltaTime;
            if (attackTimer > 0f) return;
            attackTimer = secondsPerHit;

            target.TakeDamage(damagePerHit);
            transform.localScale = lungeBase * 1.15f;
        }
    }
}
