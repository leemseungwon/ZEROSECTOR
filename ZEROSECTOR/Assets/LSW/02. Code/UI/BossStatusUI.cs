
using LSW._02._Code.Entity;
using LSW._02._Code.Entity.Boss;
using UnityEngine;
using UnityEngine.UI;

namespace LSW._02._Code.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class BossStatusUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Slider invincibilitySlider;

        private BaseBoss _boss;
        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Update()
        {
            if (_boss == null)
                return;

            if (_boss.IsDead || !_boss.gameObject.activeInHierarchy)
            {
                Hide();
                return;
            }

            Refresh();
        }

        public void ShowBoss(BaseBoss boss)
        {
            if (_boss != boss)
            {
                UnsubscribeFromBoss();
                _boss = boss;
                if (_boss != null)
                    _boss.OnDeathEvent += HandleBossDeath;
            }

            if (_boss == null || _boss.IsDead || !_boss.gameObject.activeInHierarchy)
            {
                Hide();
                return;
            }

            if (_canvasGroup != null)
                _canvasGroup.alpha = 1;

            Refresh();
        }

        public void Hide()
        {
            UnsubscribeFromBoss();
            _boss = null;
            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;
        }

        private void Refresh()
        {
            if (_boss == null)
                return;
            
            if (healthSlider != null)
            {
                healthSlider.maxValue = Mathf.Max(1, _boss.MaxHealth);
                healthSlider.value = Mathf.Clamp(_boss.CurrentHealthCount, 0, _boss.MaxHealth);
            }

            float remainingInvincibility = _boss.BuffComponent != null
                ? _boss.BuffComponent.RemainingInvincibilityTime
                : 0f;

            if (invincibilitySlider != null)
            {
                invincibilitySlider.minValue = 0f;
                invincibilitySlider.maxValue = Mathf.Max(0.01f, _boss.MaxInvincibilityDuration);
                invincibilitySlider.value = Mathf.Clamp(remainingInvincibility, 0f,
                    invincibilitySlider.maxValue);
            }

        }

        private void HandleBossDeath(BaseEntity _)
        {
            Hide();
        }

        private void OnDisable()
        {
            UnsubscribeFromBoss();
            _boss = null;
        }

        private void OnDestroy()
        {
            UnsubscribeFromBoss();
        }

        private void UnsubscribeFromBoss()
        {
            if (_boss != null)
                _boss.OnDeathEvent -= HandleBossDeath;
        }
    }
}
