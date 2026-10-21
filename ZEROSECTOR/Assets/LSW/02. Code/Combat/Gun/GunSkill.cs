
using LSW._02._Code.Entity;
using LSW._02._Code.Entity.Enemy;
using LSW._02._Code.Sos;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LSW._02._Code.Combat.Gun
{
    public class GunSkill : MonoBehaviour, IWeapon
    {
        [Header("Skill")]
        [field:SerializeField] public WeaponData GunData { get; private set; }
        [SerializeField] private Transform firePoint;
        [SerializeField] private LayerMask enemyLayers;

        private BaseEntity _owner;

        public void Initialize(BaseEntity owner)
        {
            _owner = owner;
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
            Camera playerCamera = Camera.main;
            if (firePoint == null || playerCamera == null)
                return false;

            if (Mouse.current == null)
                return false;

            Vector2 mouseScreen =
                Mouse.current.position.ReadValue();

            float distanceFromCamera = Mathf.Abs(
                firePoint.position.z -
                playerCamera.transform.position.z
            );

            Vector3 mouseWorld =
                playerCamera.ScreenToWorldPoint(
                    new Vector3(
                        mouseScreen.x,
                        mouseScreen.y,
                        distanceFromCamera
                    )
                );

            Vector2 direction =
                (Vector2)mouseWorld - (Vector2)firePoint.position;

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
            
            float range = GunData != null ? GunData.range : 30f;

            RaycastHit2D hit = Physics2D.Raycast(
                firePoint.position,
                direction,
                range,
                enemyLayers
            );

            if (hit.collider == null)
                return false;

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