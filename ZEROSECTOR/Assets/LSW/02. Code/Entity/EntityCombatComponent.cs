using LSW._02._Code.Combat.Gun;
using LSW._02._Code.Combat.Katana;
using LSW._02._Code.Common;
using LSW._02._Code.Sos;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace LSW._02._Code.Entity
{
    public class EntityCombatComponent : MonoBehaviour, IEntityComponent
    {
        [Header("Weapons")]
        [SerializeField] private KatanaWeapon katanaWeapon;
        [SerializeField] private GunSkill gunSkill;
        [SerializeField] private WeaponData defaultWeapon;
        [Header("Gauge")] [SerializeField] private float maxGauge = 100f;
        [FormerlySerializedAs("gaugePerHit")]
        [SerializeField] private float gaugePerKill = 20f;
        [SerializeField] private Slider gaugeSlider;

        private BaseEntity _owner;
        private float _currentGauge;
        private WeaponData _equippedWeapon;

        public float CurrentGauge => _currentGauge;
        public float MaxGauge => maxGauge;
        public bool IsGunReady => _currentGauge >= maxGauge;
        public bool IsKatanaEquipped =>
            _equippedWeapon != null && _equippedWeapon.weaponType == WeaponType.Katana;
        public float KatanaAttackCooldown =>
            IsKatanaEquipped ? Mathf.Max(0f, _equippedWeapon.attackCooldown) : 0f;
        public WeaponData DefaultWeapon => defaultWeapon;

        public void Initialize(BaseEntity owner)
        {
            UnsubscribeEvents();

            _owner = owner;

            maxGauge = Mathf.Max(1f, maxGauge);
            gaugePerKill = Mathf.Max(0f, gaugePerKill);
            _currentGauge = 0f;

            if (katanaWeapon != null)
            {
                katanaWeapon.Initialize(_owner);
            }

            _owner.OnEnemyKilled += ChargeGauge;

            if (gunSkill != null)
            {
                gunSkill.Initialize(_owner);
            }

            EquipWeapon(defaultWeapon);
            UpdateGaugeUI();
        }

        public void Reset()
        {
            UnsubscribeEvents();
        }

        private void UnsubscribeEvents()
        {
            if (_owner != null)
                _owner.OnEnemyKilled -= ChargeGauge;
        }
        
        public bool Attack(Vector2? skillDirection = null, bool isAirborne = false)
        {
            if (_equippedWeapon == null)
                return false;

            if (_equippedWeapon.weaponType == WeaponType.Gun)
            {
                if (!IsGunReady || gunSkill == null)
                    return false;

                bool hitEnemy;
                if (skillDirection.HasValue)
                    hitEnemy = gunSkill.TryAttack(skillDirection.Value);
                else
                    hitEnemy = gunSkill.TryAttack();

                if (!hitEnemy)
                    return false;
                _currentGauge = 0f;
                UpdateGaugeUI();
                return true;
            }

            return _equippedWeapon.weaponType == WeaponType.Katana &&
                   katanaWeapon != null &&
                   katanaWeapon.Attack(isAirborne ? skillDirection : null);
        }

        public void EquipWeapon(WeaponData weapon)
        {
            _equippedWeapon = weapon;
            katanaWeapon?.Equip(weapon);
            gunSkill?.Equip(weapon);
        }

        public bool DashAttack(BaseEntity target)
        {
            if (_owner == null || target == null)
                return false;

            return Tools.AttackTarget(_owner, target);
        }

        private void ChargeGauge(BaseEntity _)
        {
            _currentGauge = Mathf.Min(
                _currentGauge + gaugePerKill,
                maxGauge
            );

            UpdateGaugeUI();
        }

        public void ResetForRoom()
        {
            UnsubscribeEvents();

            if (_owner != null)
                _owner.OnEnemyKilled += ChargeGauge;

            UpdateGaugeUI();
        }

        private void UpdateGaugeUI()
        {
            if (gaugeSlider == null)
                return;

            gaugeSlider.maxValue = maxGauge;
            gaugeSlider.value = _currentGauge;
        }
    }
}