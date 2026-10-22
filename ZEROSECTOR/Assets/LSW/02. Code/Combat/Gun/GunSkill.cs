
using LSW._02._Code.Entity;
using LSW._02._Code.Entity.Enemy;
using LSW._02._Code.Entity.Players;
using LSW._02._Code.Sos;
using LSW._02._Code.System;
using LSW._02._Code.System.PoolSystems;
using UnityEngine;

namespace LSW._02._Code.Combat.Gun
{
    public class GunSkill : MonoBehaviour, IWeapon
    {
        [Header("Skill")]
        [field:SerializeField] public WeaponData GunData { get; private set; }
        [SerializeField] private Transform firePoint;
        [SerializeField] private LayerMask enemyLayers;

        [Header("Visual")]
        [SerializeField] private string bulletLinePoolName;

        private PlayerController _owner;
        private PoolSystem _poolSystem;

        public void Initialize(BaseEntity owner)
        {
            _owner = owner as PlayerController;
            SystemHandler.Instance.GetSystem(out _poolSystem);
            if (firePoint == null && _owner != null)
                firePoint = _owner.transform;
        }

        public bool Attack()
        {
            return TryAttack();
        }

        public void GunCast()
        {
            TryAttack();
        }

        public bool TryAttack()
        {
            if (_owner == null || _owner.inputData == null || firePoint == null)
                return false;

            Camera cam = _owner.PlayerCamera != null ? _owner.PlayerCamera
                : Camera.main;

            if (cam == null)
                return false;

            Vector2 mouseScreen = _owner.inputData.MousePosition;

            float depth = Mathf.Abs(
                firePoint.position.z - cam.transform.position.z
            );

            Vector3 mouseWorld = cam.ScreenToWorldPoint(
                new Vector3(mouseScreen.x, mouseScreen.y, depth)
            );
            
            Vector2 direction = (Vector2)mouseWorld - (Vector2)firePoint.position;
            
            return TryAttack(direction);
        }

        public bool TryAttack(Vector2 direction)
        {
            if (firePoint == null || _owner == null)
                return false;

            if (direction.sqrMagnitude <= 0.0001f)
                return false;
            
            direction.Normalize();
            _owner.Knockback(GunData != null ? GunData.selfKnockbackPower : 0f, -direction);
            
            RaycastHit2D hit = Physics2D.Raycast(
                firePoint.position,
                direction,
                Mathf.Infinity,
                enemyLayers
            );
            
            if (hit.collider == null)
                return false;
            
            if (_poolSystem.SpawnPool(bulletLinePoolName, out GunBulletLine bulletLine))
            {
                bulletLine.ShowLine(firePoint.position, hit.point);
            }

            EnemyProjectile projectile = hit.collider.GetComponentInParent<EnemyProjectile>();
            if (projectile != null)
            {
                projectile.DestroyByAttack();
                return true;
            }

            BaseEntity target =
                hit.collider.GetComponentInParent<BaseEntity>();

            if (target == null || target == _owner)
                return true;
            
            return Tools.AttackTarget(_owner, target);
        }
    }
}