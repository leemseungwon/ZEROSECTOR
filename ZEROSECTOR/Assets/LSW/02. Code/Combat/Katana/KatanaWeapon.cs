using System.Collections.Generic;
using LSW._02._Code.Common;
using LSW._02._Code.Entity;
using LSW._02._Code.Sos;
using UnityEngine;

namespace LSW._02._Code.Combat.Katana
{
    public class KatanaWeapon : MonoBehaviour, IWeapon
    {
        [Header("Attack")]
        [SerializeField] private Transform attackPoint;
        [SerializeField] private LayerMask enemyLayers;

        private BaseEntity _owner;
        private WeaponData _weaponData;

        public void Initialize(BaseEntity owner)
        {
            _owner = owner;
        }

        public void Equip(WeaponData weaponData)
        {
            _weaponData = weaponData != null && weaponData.weaponType == WeaponType.Katana
                ? weaponData
                : null;
        }

        public bool Attack()
        {
            return Attack(null);
        }

        public bool Attack(Vector2? direction)
        {
            if (attackPoint == null || _owner == null || _weaponData == null)
                return false;
            
            Vector2 attackPosition = attackPoint.transform.position;
            if (direction.HasValue && Mathf.Abs(direction.Value.y) > 0.7f)
            {
                Collider2D ownerCollider = _owner.GetComponent<Collider2D>();
                Vector2 ownerCenter = ownerCollider != null
                    ? ownerCollider.bounds.center
                    : _owner.transform.position;
                attackPosition = ownerCenter +
                                 Vector2.up * Mathf.Sign(direction.Value.y) * _weaponData.attackRadius;
            }

            Collider2D[] hits = Physics2D.OverlapCircleAll(attackPosition,
                _weaponData.attackRadius, enemyLayers);

            HashSet<BaseEntity> hitEntities = new();
            bool killedEnemy = false;

            foreach (Collider2D hit in hits)
            {
                BaseEntity target =
                    hit.GetComponentInParent<BaseEntity>();

                if (target == null ||
                    target == _owner ||
                    !hitEntities.Add(target))
                {
                    continue;
                }

                bool hitTarget = Tools.AttackTarget(_owner, target);

                if (hitTarget && target.IsDead)
                    killedEnemy = true;
            }

            return killedEnemy;
        }

        private void OnDrawGizmosSelected()
        {
            if (attackPoint == null || _weaponData == null)
                return;

            Gizmos.DrawWireSphere(attackPoint.transform.position, _weaponData.attackRadius);
        }
    }
}