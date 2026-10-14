using LSW._02._Code.Combat.Gun;
using LSW._02._Code.Combat.Katana;
using UnityEngine;
using UnityEngine.UI;

namespace LSW._02._Code.Entity
{
    public class EntityCombatComponent : MonoBehaviour, IEntityComponent
    {
        [Header("Weapons")]
        [SerializeField] private KatanaWeapon katanaWeapon;
        [SerializeField] private GunSkill gunSkill;

        [Header("Gauge")] [SerializeField] private float maxGauge = 100f;
        [SerializeField] private float gaugePerHit = 20f;
        [SerializeField] private Slider gaugeSlider;

        private BaseEntity _owner;
        private float _currentGauge;

        public float CurrentGauge => _currentGauge;
        public float MaxGauge => maxGauge;
        public bool IsGunReady => _currentGauge >= maxGauge;

        public void Initialize(BaseEntity owner)
        {
            UnsubscribeEvents();

            _owner = owner;

            maxGauge = Mathf.Max(1f, maxGauge);
            gaugePerHit = Mathf.Max(0f, gaugePerHit);
            _currentGauge = 0f;

            if (katanaWeapon != null)
            {
                katanaWeapon.Initialize(_owner);
                katanaWeapon.OnHitEnemy += ChargeGauge;
            }

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
            if (katanaWeapon != null)
                katanaWeapon.OnHitEnemy -= ChargeGauge;
        }
        
        public void Attack(Vector2? skillDirection = null)
        {
            if (IsGunReady)
            {
                if (gunSkill == null)
                    return;

                if (skillDirection.HasValue)
                    gunSkill.Attack(skillDirection.Value);
                else
                    gunSkill.Attack();

                _currentGauge = 0f;
                UpdateGaugeUI();
            }
            else
            {
                katanaWeapon?.Attack();
            }
        }

        private void ChargeGauge()
        {
            _currentGauge = Mathf.Min(
                _currentGauge + gaugePerHit,
                maxGauge
            );

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