using LSW._02._Code.Sos;
using LSW._02._Code.Entity.Players.Components;
using UnityEngine;

namespace LSW._02._Code.Combat
{
    public class WeaponPickup : MonoBehaviour
    {
        [SerializeField] private WeaponData weaponData;

        private Vector3 _spawnPosition;
        private bool _isRuntimeDrop;

        private void Awake()
        {
            _spawnPosition = transform.position;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
            if (inventory == null || !inventory.TryAddWeapon(weaponData))
                return;

            gameObject.SetActive(false);
        }

        public void ResetForRoom()
        {
            if (_isRuntimeDrop)
            {
                gameObject.SetActive(false);
                return;
            }

            transform.position = _spawnPosition;
            gameObject.SetActive(true);
        }

        public void MarkAsRuntimeDrop()
        {
            _isRuntimeDrop = true;
        }
    }
}
