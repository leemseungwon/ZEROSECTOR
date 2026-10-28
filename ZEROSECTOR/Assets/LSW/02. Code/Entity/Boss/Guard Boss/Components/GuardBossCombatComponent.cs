
using System;
using System.Collections;
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
        [SerializeField, Min(0f)] private float basicCooldown = 1.4f;

        [Header("Three Hit Combo")]
        [SerializeField, Min(0f)] private float comboCooldown = 4f;

        [Header("Charged Heavy Attack")]
        [SerializeField, Min(0f)] private float heavyCooldown = 6f;

        [Header("Aerial Three Hit Attack")]
        [SerializeField, Min(0f)] private float airComboPreparation = 0.3f;
        [SerializeField, Min(0f)] private float airComboCooldown = 7f;

        [Header("Aerial Dash Attack")]
        [SerializeField, Min(0f)] private float airDashPreparation = 0.24f;
        [SerializeField, Min(0.1f)] private float airDashSpeed = 12f;
        [SerializeField, Min(0f)] private float airDashCooldown = 6f;
        [SerializeField, Min(0f)] private float postAttackRecovery = 0.65f;

        [Header("Encounter Vulnerability")]
        [SerializeField, Min(0f)] private float basicVulnerabilityMin = 0.6f;
        [SerializeField, Min(0f)] private float basicVulnerabilityMax = 0.8f;
        [SerializeField, Min(0f)] private float comboVulnerabilityMin = 1f;
        [SerializeField, Min(0f)] private float comboVulnerabilityMax = 1.3f;
        [SerializeField, Min(0f)] private float slamVulnerabilityMin = 1.5f;
        [SerializeField, Min(0f)] private float slamVulnerabilityMax = 2f;
        [SerializeField, Min(0f)] private float phaseTransitionFreezeDuration = 0.5f;
        [SerializeField, Min(0.05f)] private float phaseOnePatternInterval = 1.25f;
        [SerializeField, Range(0f, 1f)] private float phaseTwoComboStopChance = 0.35f;
        [SerializeField, Min(1)] private int phaseTwoMaxCombosWithoutStop = 3;
        [SerializeField, Min(0f)] private float phaseTwoComboStopVulnerability = 1f;
        [SerializeField, Range(0f, 1f)] private float phaseTwoIntentionalMissChance = 0.25f;
        [SerializeField, Min(0f)] private float phaseTwoIntentionalMissOffset = 3f;
        [SerializeField, Min(0.1f)] private float phaseTwoMinionIntervalMin = 8f;
        [SerializeField, Min(0.1f)] private float phaseTwoMinionIntervalMax = 14f;
        [SerializeField, Min(0f)] private float phaseTwoWallHitVulnerability = 1.1f;

        private GuardBoss _boss;
        private float _postAttackRecoveryRemaining;
        private float _vulnerabilityRemaining;
        private int _currentPhase = 1;
        private int _phaseOneAttackIndex;
        private int _comboStopHitIndex;
        private int _consecutiveCombosWithoutStop;
        private bool _comboWasInterrupted;
        private float _phaseTwoMinionRemaining;
        private readonly Dictionary<AttackPattern, float> _cooldowns = new();

        public float AttackRange => attackRange;
        public float AttackRadius => attackRadius;
        public float HeavyAttackRadius => heavyAttackRadius;
        public float AirComboPreparation => airComboPreparation;
        public float AirDashPreparation => airDashPreparation;
        public float AirDashSpeed => airDashSpeed;
        public bool IsVulnerable => _vulnerabilityRemaining > 0f;
        public float VulnerabilityRemaining => _vulnerabilityRemaining;
        public int CurrentPhase => _currentPhase;
        public float PhaseOnePatternInterval => phaseOnePatternInterval;

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
            bool wasVulnerable = IsVulnerable;
            _vulnerabilityRemaining = Mathf.Max(0f, _vulnerabilityRemaining - deltaTime);
            if (wasVulnerable && !IsVulnerable)
                _boss?.BossAnimation?.SetVulnerable(false);
            TickPhaseTwoMinions(deltaTime);
            List<AttackPattern> patterns = new(_cooldowns.Keys);
            foreach (AttackPattern pattern in patterns)
                _cooldowns[pattern] = Mathf.Max(0f, _cooldowns[pattern] - deltaTime);
        }

        public void ResetCooldowns()
        {
            _postAttackRecoveryRemaining = 0f;
            _vulnerabilityRemaining = 0f;
            _currentPhase = 1;
            _phaseOneAttackIndex = 0;
            _comboStopHitIndex = 0;
            _consecutiveCombosWithoutStop = 0;
            _comboWasInterrupted = false;
            _phaseTwoMinionRemaining = 0f;
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
            if (_currentPhase == 1)
            {
                AttackPattern[] rhythm = { AttackPattern.Basic, AttackPattern.Combo, AttackPattern.Heavy };
                AttackPattern next = rhythm[_phaseOneAttackIndex % rhythm.Length];
                if (IsReady(next) && _postAttackRecoveryRemaining <= 0f)
                {
                    pattern = next;
                    _phaseOneAttackIndex++;
                    return true;
                }

                pattern = default;
                return false;
            }

            return TryChoose(new[] { AttackPattern.Basic, AttackPattern.Combo, AttackPattern.Heavy }, out pattern);
        }

        public bool TryChooseAirAttack(out AttackPattern pattern)
        {
            if (_currentPhase == 1)
            {
                pattern = default;
                return false;
            }

            return TryChoose(new[] { AttackPattern.AirCombo, AttackPattern.AirDash }, out pattern);
        }

        public void OnAttackFinished(AttackPattern pattern, bool hitTarget)
        {
            if (pattern == AttackPattern.Heavy && _currentPhase == 1)
                _boss?.SpawnMinionAfterSlam();

            bool finalPhaseSlam = _currentPhase == 3 && pattern == AttackPattern.Heavy;
            if (_currentPhase == 3 && pattern != AttackPattern.Heavy || hitTarget && !finalPhaseSlam)
                return;

            float duration = pattern switch
            {
                AttackPattern.Basic => Random.Range(basicVulnerabilityMin, basicVulnerabilityMax),
                AttackPattern.Combo => Random.Range(comboVulnerabilityMin, comboVulnerabilityMax),
                AttackPattern.Heavy => finalPhaseSlam
                    ? 1.5f
                    : Random.Range(slamVulnerabilityMin, slamVulnerabilityMax),
                _ => 0f
            };

            OpenVulnerability(duration);
        }

        public void PrepareCombo()
        {
            _comboWasInterrupted = false;
            _comboStopHitIndex = 0;
            if (_currentPhase != 2)
                return;

            bool forceStop = _consecutiveCombosWithoutStop >=
                             Mathf.Max(1, phaseTwoMaxCombosWithoutStop);
            if (forceStop || Random.value < phaseTwoComboStopChance)
                _comboStopHitIndex = Random.Range(1, 4);
        }

        public bool TryStopComboAfterHit(int hitCount)
        {
            if (_currentPhase != 2 || _comboStopHitIndex != hitCount)
                return false;

            _comboWasInterrupted = true;
            _consecutiveCombosWithoutStop = 0;
            OpenVulnerability(phaseTwoComboStopVulnerability);
            return true;
        }

        public void CompleteCombo()
        {
            if (_currentPhase != 2 || _comboWasInterrupted)
                return;

            _consecutiveCombosWithoutStop++;
        }

        public void OnDashHitWall()
        {
            if (_currentPhase == 2)
                OpenVulnerability(phaseTwoWallHitVulnerability);
        }

        private void TickPhaseTwoMinions(float deltaTime)
        {
            if (_currentPhase != 2 || _boss == null)
                return;

            _phaseTwoMinionRemaining -= deltaTime;
            if (_phaseTwoMinionRemaining > 0f)
                return;

            if (_boss.TrySpawnMinion(2))
                _phaseTwoMinionRemaining = Random.Range(phaseTwoMinionIntervalMin,
                    Mathf.Max(phaseTwoMinionIntervalMin, phaseTwoMinionIntervalMax));
            else
                _phaseTwoMinionRemaining = 0.25f;
        }

        public bool ShouldIntentionallyMissBasic()
        {
            return _currentPhase == 2 && Random.value < phaseTwoIntentionalMissChance;
        }

        public bool AttackTarget(float radius, bool forceMiss)
        {
            return AttackTarget(radius, forceMiss ? phaseTwoIntentionalMissOffset : 0f);
        }

        public void OpenVulnerability(float duration)
        {
            if (duration <= 0f || _boss == null || _boss.IsDead)
                return;

            _vulnerabilityRemaining = Mathf.Max(_vulnerabilityRemaining, duration);
            _boss.BossAnimation?.SetVulnerable(true);
            _boss.Movement?.StopHorizontal();
            _boss.StateMachine?.ChangeState(GuardBoss.VulnerableState);
        }

        public void NotifyCountLost(int currentCount)
        {
            int nextPhase = currentCount <= 2 ? 3 : currentCount <= 5 ? 2 : 1;
            if (nextPhase == _currentPhase)
                return;

            _currentPhase = nextPhase;
            if (_currentPhase == 2)
                _phaseTwoMinionRemaining = Random.Range(phaseTwoMinionIntervalMin,
                    Mathf.Max(phaseTwoMinionIntervalMin, phaseTwoMinionIntervalMax));
            _boss?.BossAnimation?.SetPhase(_currentPhase);
            _boss?.NotifyPhaseChanged(_currentPhase);
            if (phaseTransitionFreezeDuration > 0f)
                StartCoroutine(PhaseTransitionFreeze());
        }

        private IEnumerator PhaseTransitionFreeze()
        {
            float previousScale = Time.timeScale;
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(phaseTransitionFreezeDuration);
            if (Time.timeScale == 0f)
                Time.timeScale = previousScale > 0f ? previousScale : 1f;
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
            return AttackTarget(radius, 0f);
        }

        private bool AttackTarget(float radius, float missOffset)
        {
            if (_boss == null || !_boss.HasLivingTarget())
                return false;

            float facing = _boss.Movement != null ? _boss.Movement.FacingDirection : 1f;
            Vector2 center = attackPoint != null
                ? attackPoint.position
                : (Vector2)_boss.transform.position + new Vector2(attackOffset.x * facing, attackOffset.y);
            if (missOffset > 0f && _boss.TargetPlayer != null)
            {
                float awayFromPlayer = Mathf.Sign(center.x - _boss.TargetPlayer.transform.position.x);
                if (Mathf.Approximately(awayFromPlayer, 0f))
                    awayFromPlayer = -facing;
                center.x += awayFromPlayer * missOffset;
            }
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
