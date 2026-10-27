using LSW._02._Code.System;
using LSW._02._Code.System.PoolSystems;
using UnityEngine;

namespace LSW._02._Code.Entity.Enemy
{
    public class RangedEnemy : BaseEnemy
    {
        [Header("Ranged Attack")]
        [SerializeField] private string projectilePoolName;
        [SerializeField, Min(0f)] private float projectileSpeed = 8f;
        [SerializeField, Min(0.01f)] private float projectileLifetime = 4f;
        [SerializeField] private Transform firePoint;
        [SerializeField] private Vector2 fallbackFireOffset = new(0.7f, 0.25f);

        private EnemyProjectile _launchProjectile;

        public override void AttackTarget()
        {
            if (!HasLivingTarget() || IsTargetBlockedByWall() ||
                string.IsNullOrEmpty(projectilePoolName))
                return;

            float facingDirection = Animation != null
                ? Mathf.Sign(Animation.transform.localScale.x)
                : InitialFacingDirection;
            Vector2 spawnPosition = firePoint != null
                ? firePoint.position
                : (Vector2)transform.position + new Vector2(
                    fallbackFireOffset.x * facingDirection, fallbackFireOffset.y);
            Vector2 direction = (Vector2)TargetPlayer.transform.position - spawnPosition;
            direction.y += 0.5f;

            if (direction.sqrMagnitude <= 0.0001f)
                return;

            CachePoolSystem();
            PoolSystem poolSystem = PoolSystem;
            if (poolSystem != null &&
                poolSystem.SpawnPool(projectilePoolName, out EnemyProjectile projectile))
            {
                _launchProjectile = projectile;
                projectile.Launch(this, spawnPosition, direction.normalized,
                    projectileSpeed, projectileLifetime);
            }
        }

        public override void PlayDeadAnimation()
        {
            if (_launchProjectile != null)
            {
                CachePoolSystem();
                PoolSystem?.DespawnPool(projectilePoolName, _launchProjectile);
                _launchProjectile = null;
            }

            base.PlayDeadAnimation();
        }
    }
}
