using LSW._02._Code.Combat;
using LSW._02._Code.Entity.Players;
using UnityEngine;

namespace LSW._02._Code.Entity.Enemy
{
    public class WeaponDropComponent : MonoBehaviour, IEntityComponent
    {
        [SerializeField, Range(0f, 1f)] private float dropChance = 0.2f;
        [SerializeField] private WeaponPickup[] possibleDrops;

        private BaseEntity _owner;

        public void Initialize(BaseEntity owner)
        {
            if (_owner != null)
                _owner.OnKilledBy -= TryDropWeapon;

            _owner = owner;
            _owner.OnKilledBy += TryDropWeapon;
        }

        private void TryDropWeapon(BaseEntity killer)
        {
            if (killer is not PlayerController || possibleDrops == null ||
                possibleDrops.Length == 0 || Random.value > dropChance)
                return;

            WeaponPickup pickup = possibleDrops[Random.Range(0, possibleDrops.Length)];
            if (pickup != null)
            {
                WeaponPickup droppedWeapon = Instantiate(pickup, transform.position, Quaternion.identity);
                droppedWeapon.MarkAsRuntimeDrop();
                if (killer.TryGetComponent(out RoomController roomController))
                    roomController.RegisterPickup(droppedWeapon);
            }
        }

        public void Reset()
        {
            if (_owner != null)
                _owner.OnKilledBy -= TryDropWeapon;

            _owner = null;
        }
    }
}
