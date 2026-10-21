using LSW._02._Code.Combat.Gun;
using LSW._02._Code.Combat.Katana;
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
        
        [Header("Gauge")] 
        [SerializeField] private float maxGauge = 100f;
        [FormerlySerializedAs("gaugePerHit")]
        [SerializeField] private float gaugePerKill = 20f;
        [SerializeField] private Slider gaugeSlider;

        private BaseEntity _owner;
        private float _currentGauge;
        private float _checkpointGauge;

        public float CurrentGauge => _currentGauge;
        public float MaxGauge => maxGauge;
        public bool IsGunReady => _currentGauge >= maxGauge;
        public float KatanaAttackCooldown => katanaWeapon != null 
            ? Mathf.Max(0f, katanaWeapon.WeaponData.attackCooldown) : 0f;

        public void Initialize(BaseEntity owner)
        {
            UnsubscribeEvents();

            _owner = owner;

            maxGauge = Mathf.Max(1f, maxGauge);
            gaugePerKill = Mathf.Max(0f, gaugePerKill);
            _currentGauge = 0f;
            _checkpointGauge = 0f;

            if (katanaWeapon != null)
            {
                katanaWeapon.Initialize(_owner);
            }

            _owner.OnEnemyKilled += ChargeGauge;

            if (gunSkill != null)
            {
                gunSkill.Initialize(_owner);
            }
            
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
            if (katanaWeapon == null)
                return false;

            return katanaWeapon != null &&
                   katanaWeapon.Attack(isAirborne ? skillDirection : null);
        }

        public bool ConsumeGunGauge()
        {
            if (!IsGunReady)
                return false;

            _currentGauge = 0f;
            UpdateGaugeUI();
            return true;
        }

        public void GunCast()
        {
            ExecuteGunRaycast();
        }

        public bool ExecuteGunRaycast(Vector2? skillDirection = null)
        {
            if (gunSkill == null)
                return false;

            if (skillDirection.HasValue)
                return gunSkill.TryAttack(skillDirection.Value);

            return gunSkill.TryAttack();
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

        public void SaveCheckpointGauge()
        {
            _checkpointGauge = _currentGauge;
        }

        public void ResetForRoom()
        {
            UnsubscribeEvents();

            if (_owner != null)
                _owner.OnEnemyKilled += ChargeGauge;

            _currentGauge = _checkpointGauge;
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