using System;
using System.Collections.Generic;
using LSW._02._Code.Entity;
using LSW._02._Code.Sos;
using UnityEngine;

namespace LSW._02._Code.Combat.Katana
{
    public class KatanaWeapon : MonoBehaviour, IWeapon
    {
        [Header("Attack")]
        [SerializeField] private Transform attackPoint;
        [SerializeField] private AttackData attackData;
        [SerializeField] private LayerMask enemyLayers;

        private BaseEntity _owner;

        public event Action OnHitEnemy;

        public void Initialize(BaseEntity owner)
        {
            _owner = owner;
        }

        public void Attack()
        {
            if (attackPoint == null || _owner == null)
                return;
            
            Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.transform.position,
                attackData.attackRadius, enemyLayers);

            HashSet<BaseEntity> hitEntities = new();

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

                Tools.ReceiveDamageToTarget(
                    _owner,
                    target,
                    attackData
                );

                OnHitEnemy?.Invoke();
            }
            
            Debug.Log("Attack");
        }

        private void OnDrawGizmosSelected()
        {
            if (attackPoint == null) return;

            Gizmos.DrawWireSphere(attackPoint.transform.position, attackData.attackRadius);
        }
    }
}