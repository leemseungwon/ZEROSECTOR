
using System;
using System.Collections.Generic;
using LSW._02._Code.Entity.Players;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LSW._02._Code.Entity.Boss.Guard_Boss.Components
{
    public class GuardBossCombatComponent : MonoBehaviour, IEntityComponent
    {
        public enum AttackPattern
        {
            Basic,
            Combo,
            Heavy,
            AirCombo,
            AirDash
        }

        [Header("Hit Detection")]
        [SerializeField] private LayerMask playerLayers = 1 << 7;
        [SerializeField] private Transform attackPoint;
        [SerializeField, Min(0f)] private float attackRange = 2.3f;
        [SerializeField, Min(0f)] private float attackRadius = 1.4f;
        [SerializeField] private Vector2 attackOffset = new(1.1f, 0.4f);
        [SerializeField, Min(0f)] private float heavyAttackRadius = 2.2f;

        [Header("Basic Attack")]
        [SerializeField, Min(0f)] private float basicWindup = 0.3f;
        [SerializeField, Min(0f)] private float basicRecovery = 0.15f;
        [SerializeField, Min(0f)] private float basicCooldown = 1.4f;

        [Header("Three Hit Combo")]
        [SerializeField, Min(0f)] private float comboFirstWindup = 0.12f;
        [SerializeField, Min(0.05f)] private float comboHitInterval = 0.32f;
        [SerializeField, Min(0f)] private float comboRecovery = 0.23f;
        [SerializeField, Min(0f)] private float comboCooldown = 4f;

        [Header("Charged Heavy Attack")]
        [SerializeField, Min(0f)] private float heavyChargeTime = 0.62f;
        [SerializeField, Min(0f)] private float heavyRecovery = 0.25f;
        [SerializeField, Min(0f)] private float heavyCooldown = 6f;

        [Header("Aerial Three Hit Attack")]
        [SerializeField, Min(0f)] private float airComboPreparation = 0.3f;
        [SerializeField, Min(0f)] private float airComboFirstWindup = 0.14f;
        [SerializeField, Min(0.05f)] private float airComboHitInterval = 0.22f;
        [SerializeField, Min(0f)] private float airComboRecovery = 0.25f;
        [SerializeField, Min(0f)] private float airComboCooldown = 7f;

        [Header("Aerial Dash Attack")]
        [SerializeField, Min(0f)] private float airDashPreparation = 0.24f;
        [SerializeField, Min(0.1f)] private float airDashSpeed = 12f;
        [SerializeField, Min(0f)] private float airDashDuration = 0.28f;
        [SerializeField, Min(0f)] private float airDashHitDelay = 0.18f;
        [SerializeField, Min(0f)] private float airDashCooldown = 6f;
        [SerializeField, Min(0f)] private float postAttackRecovery = 0.65f;

        private GuardBoss _boss;
        private float _postAttackRecoveryRemaining;
        private readonly Dictionary<AttackPattern, float> _cooldowns = new();

        public float AttackRange => attackRange;
        public float AttackRadius => attackRadius;
        public float HeavyAttackRadius => heavyAttackRadius;
        public float BasicWindup => basicWindup;
        public float BasicRecovery => basicRecovery;
        public float ComboFirstWindup => comboFirstWindup;
        public float ComboHitInterval => comboHitInterval;
        public float ComboRecovery => comboRecovery;
        public float HeavyChargeTime => heavyChargeTime;
        public float HeavyRecovery => heavyRecovery;
        public float AirComboPreparation => airComboPreparation;
        public float AirComboFirstWindup => airComboFirstWindup;
        public float AirComboHitInterval => airComboHitInterval;
        public float AirComboRecovery => airComboRecovery;
        public float AirDashPreparation => airDashPreparation;
        public float AirDashSpeed => airDashSpeed;
        public float AirDashDuration => airDashDuration;
        public float AirDashHitDelay => airDashHitDelay;

        public void Initialize(BaseEntity owner)
        {
            _boss = owner as GuardBoss;
            ResetCooldowns();
        }

        public void Reset()
        {
            ResetCooldowns();
        }

        public void TickCooldowns(float deltaTime)
        {
            _postAttackRecoveryRemaining = Mathf.Max(0f, _postAttackRecoveryRemaining - deltaTime);
            List<AttackPattern> patterns = new(_cooldowns.Keys);
            foreach (AttackPattern pattern in patterns)
                _cooldowns[pattern] = Mathf.Max(0f, _cooldowns[pattern] - deltaTime);
        }

        public void ResetCooldowns()
        {
            _postAttackRecoveryRemaining = 0f;
            _cooldowns.Clear();
            foreach (AttackPattern pattern in Enum.GetValues(typeof(AttackPattern)))
                _cooldowns[pattern] = 0f;
        }

        public bool IsReady(AttackPattern pattern)
        {
            return !_cooldowns.TryGetValue(pattern, out float remaining) || remaining <= 0f;
        }

        public void StartPostAttackRecovery()
        {
            _postAttackRecoveryRemaining = postAttackRecovery;
        }

        public bool TryChooseGroundAttack(out AttackPattern pattern)
        {
            return TryChoose(new[] { AttackPattern.Basic, AttackPattern.Combo, AttackPattern.Heavy }, out pattern);
        }

        public bool TryChooseAirAttack(out AttackPattern pattern)
        {
            return TryChoose(new[] { AttackPattern.AirCombo, AttackPattern.AirDash }, out pattern);
        }

        public void StartCooldown(AttackPattern pattern)
        {
            _cooldowns[pattern] = pattern switch
            {
                AttackPattern.Basic => basicCooldown,
                AttackPattern.Combo => comboCooldown,
                AttackPattern.Heavy => heavyCooldown,
                AttackPattern.AirCombo => airComboCooldown,
                AttackPattern.AirDash => airDashCooldown,
                _ => 0f
            };
        }

        public bool AttackTarget(float radius)
        {
            if (_boss == null || !_boss.HasLivingTarget())
                return false;

            float facing = _boss.Movement != null ? _boss.Movement.FacingDirection : 1f;
            Vector2 center = attackPoint != null
                ? attackPoint.position
                : (Vector2)_boss.transform.position + new Vector2(attackOffset.x * facing, attackOffset.y);
            Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, playerLayers);
            foreach (Collider2D hit in hits)
            {
                if (hit.GetComponentInParent<PlayerController>() != _boss.TargetPlayer)
                    continue;

                _boss.TargetPlayer.ReceiveHit(_boss);
                return true;
            }

            return false;
        }

        private bool TryChoose(AttackPattern[] candidates, out AttackPattern pattern)
        {
            if (_postAttackRecoveryRemaining > 0f)
            {
                pattern = default;
                return false;
            }

            List<AttackPattern> ready = new();
            foreach (AttackPattern candidate in candidates)
            {
                if (IsReady(candidate))
                    ready.Add(candidate);
            }

            if (ready.Count == 0)
            {
                pattern = default;
                return false;
            }

            pattern = ready[Random.Range(0, ready.Count)];
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            float facing = _boss != null && _boss.Movement != null
                ? _boss.Movement.FacingDirection
                : 1f;
            Vector2 center = attackPoint != null
                ? attackPoint.position
                : (Vector2)transform.position + new Vector2(attackOffset.x * facing, attackOffset.y);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(center, attackRadius);
            Gizmos.color = new Color(1f, 0.4f, 0f);
            Gizmos.DrawWireSphere(center, heavyAttackRadius);
        }
    }
}
