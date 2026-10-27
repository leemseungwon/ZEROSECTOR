using LSW._02._Code.Entity.Players;
using UnityEngine;

namespace LSW._02._Code.Entity.Enemy
{
    public class MeleeEnemy : BaseEnemy
    {
        [Header("Melee Attack")]
        [SerializeField, Min(0f)] private float attackHitRadius = 1.2f;
        [SerializeField] private Vector2 attackHitOffset = new(0.7f, 0.25f);
        [SerializeField] private LayerMask attackHitLayers = 1 << 7;

        public override void AttackTarget()
        {
            if (!HasLivingTarget() || IsTargetBlockedByWall())
                return;

            float facingDirection = Animation != null
                ? Mathf.Sign(Animation.transform.localScale.x)
                : InitialFacingDirection;
            Vector2 attackCenter = (Vector2)transform.position +
                                   new Vector2(attackHitOffset.x * facingDirection, attackHitOffset.y);
            Collider2D[] hits = Physics2D.OverlapCircleAll(
                attackCenter, attackHitRadius, attackHitLayers);

            foreach (Collider2D hit in hits)
            {
                if (hit.GetComponentInParent<PlayerController>() != TargetPlayer)
                    continue;

                TargetPlayer.ReceiveHit(this);
                return;
            }
        }

        private void OnDrawGizmosSelected()
        {
            float facingDirection = Animation != null
                ? Mathf.Sign(Animation.transform.localScale.x)
                : 1f;
            Vector2 attackCenter = (Vector2)transform.position +
                                   new Vector2(attackHitOffset.x * facingDirection, attackHitOffset.y);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackCenter, attackHitRadius);
        }
    }
}
