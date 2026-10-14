using LSW._02._Code.Entity;
using LSW._02._Code.Sos;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LSW._02._Code.Combat.Gun
{
    public class GunSkill : MonoBehaviour, IWeapon
    {
        [Header("Skill")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private AttackData attackData;
        [SerializeField] private float range = 30f;
        [SerializeField] private LayerMask enemyLayers;

        private BaseEntity _owner;

        public void Initialize(BaseEntity owner)
        {
            _owner = owner;
        }

        // 플레이어: 마우스 방향으로 발사
        public void Attack()
        {
            if (firePoint == null || Camera.main == null)
                return;

            if (Mouse.current == null)
                return;

            Vector2 mouseScreen =
                Mouse.current.position.ReadValue();

            float distanceFromCamera = Mathf.Abs(
                firePoint.position.z -
                Camera.main.transform.position.z
            );

            Vector3 mouseWorld =
                Camera.main.ScreenToWorldPoint(
                    new Vector3(
                        mouseScreen.x,
                        mouseScreen.y,
                        distanceFromCamera
                    )
                );

            Vector2 direction =
                (Vector2)mouseWorld -
                (Vector2)firePoint.position;

            Attack(direction);
        }

        // 적 AI: 전달받은 방향으로 발사
        public void Attack(Vector2 direction)
        {
            if (firePoint == null || _owner == null)
                return;

            if (direction.sqrMagnitude <= 0.0001f)
                return;

            direction.Normalize();

            RaycastHit2D hit = Physics2D.Raycast(
                firePoint.position,
                direction,
                range,
                enemyLayers
            );

            if (hit.collider == null)
                return;

            BaseEntity target =
                hit.collider.GetComponentInParent<BaseEntity>();

            if (target == null || target == _owner)
                return;

            Tools.ReceiveDamageToTarget(
                _owner,
                target,
                attackData
            );
        }
    }
}