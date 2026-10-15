using System;
using System.Collections.Generic;
using LSW._02._Code.Sos;
using UnityEngine;

namespace LSW._02._Code.Entity.Players.Components
{
    public class PlayerInventory : MonoBehaviour, IEntityComponent
    {
        [SerializeField] private WeaponData[] startingWeapons;

        private readonly List<WeaponData> _weapons = new();
        private EntityCombatComponent _combat;

        public IReadOnlyList<WeaponData> Weapons => _weapons;
        public WeaponData EquippedWeapon { get; private set; }

        public event Action OnInventoryChanged;
        public event Action<WeaponData> OnWeaponEquipped;

        public void Initialize(BaseEntity owner)
        {
            if (owner.GetCompo(out EntityCombatComponent combat))
                _combat = combat;

            _weapons.Clear();
            AddStartingWeapons();
            EquipStartingWeapon();
        }

        public bool TryAddWeapon(WeaponData weapon)
        {
            if (weapon == null || _weapons.Contains(weapon))
                return false;

            _weapons.Add(weapon);
            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool Equip(WeaponData weapon)
        {
            if (weapon == null || !_weapons.Contains(weapon))
                return false;

            EquippedWeapon = weapon;
            _combat?.EquipWeapon(weapon);
            OnWeaponEquipped?.Invoke(weapon);
            OnInventoryChanged?.Invoke();
            return true;
        }

        public void ResetForRoom()
        {
            _weapons.Clear();
            AddStartingWeapons();
            EquipStartingWeapon();
            OnInventoryChanged?.Invoke();
        }

        private void AddStartingWeapons()
        {
            if (startingWeapons == null)
                return;

            foreach (WeaponData weapon in startingWeapons)
            {
                if (weapon != null && !_weapons.Contains(weapon))
                    _weapons.Add(weapon);
            }
        }

        private void EquipStartingWeapon()
        {
            EquippedWeapon = null;

            if (startingWeapons != null)
            {
                foreach (WeaponData weapon in startingWeapons)
                {
                    if (weapon != null && Equip(weapon))
                        return;
                }
            }

            WeaponData fallbackWeapon = _combat != null ? _combat.DefaultWeapon : null;
            if (fallbackWeapon == null)
            {
                _combat?.EquipWeapon(null);
                return;
            }

            if (!_weapons.Contains(fallbackWeapon))
                _weapons.Add(fallbackWeapon);

            Equip(fallbackWeapon);
        }

        public void Reset() { }
    }
}
