using LSW._02._Code.Common;
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
        [SerializeField] private LayerMask enemyLayers;

        private BaseEntity _owner;
        private WeaponData _weaponData;

        public void Initialize(BaseEntity owner)
        {
            _owner = owner;
        }

        public void Equip(WeaponData weaponData)
        {
            _weaponData = weaponData != null && weaponData.weaponType == WeaponType.Gun
                ? weaponData
                : null;
        }

        public bool Attack()
        {
            return TryAttack();
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
            if (firePoint == null || _owner == null || _weaponData == null)
                return false;

            if (direction.sqrMagnitude <= 0.0001f)
                return false;

            direction.Normalize();

            RaycastHit2D hit = Physics2D.Raycast(
                firePoint.position,
                direction,
                _weaponData.range,
                enemyLayers
            );

            if (hit.collider == null)
                return false;

            BaseEntity target =
                hit.collider.GetComponentInParent<BaseEntity>();

            if (target == null || target == _owner)
                return false;

            return Tools.AttackTarget(_owner, target);
        }
    }
}