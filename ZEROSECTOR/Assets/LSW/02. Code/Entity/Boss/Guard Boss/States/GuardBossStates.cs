using System;
using LSW._02._Code.Entity.Boss.Guard_Boss.Components;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LSW._02._Code.Entity.Boss.Guard_Boss.States
{
    public class GuardBossIdleState : EntityState
    {
        private readonly GuardBoss _boss;

        public GuardBossIdleState(GuardBoss boss) : base(boss) => _boss = boss;

        public override void Enter(Action endAction = null)
        {
            base.Enter(endAction);
            _boss.Movement?.StopHorizontal();
        }

        public override void Update()
        {
            base.Update();
            if (_boss.HasLivingTarget())
                _boss.StateMachine.ChangeState(GuardBoss.ChaseState);
        }
    }

    public class GuardBossChaseState : EntityState
    {
        private readonly GuardBoss _boss;
        private float _nextPatternDecisionTime;

        public GuardBossChaseState(GuardBoss boss) : base(boss) => _boss = boss;

        public override void Enter(Action endAction = null)
        {
            base.Enter(endAction);
            _nextPatternDecisionTime = _boss.CurrentPhase == 1 && _boss.Combat != null
                ? _boss.Combat.PhaseOnePatternInterval
                : Random.Range(0.12f, 0.22f);
        }

        public override void Update()
        {
            base.Update();
            if (_boss.IsVulnerable)
            {
                _boss.StateMachine.ChangeState(GuardBoss.VulnerableState);
                return;
            }

            if (!_boss.HasLivingTarget())
            {
                _boss.StateMachine.ChangeState(GuardBoss.IdleState);
                return;
            }

            // Air dash and aerial attack states can finish before the boss lands.
            // Keep the state machine aligned with the airborne animation/physics.
            if (_boss.Movement != null && !_boss.Movement.IsGrounded)
            {
                _boss.StateMachine.ChangeState(GuardBoss.AirborneState);
                return;
            }

            _boss.FaceTarget();
            GuardBossMovementComponent movement = _boss.Movement;
            GuardBossCombatComponent combat = _boss.Combat;
            if (movement == null || combat == null)
                return;

            if (StateTimer < _nextPatternDecisionTime)
                return;
            _nextPatternDecisionTime = StateTimer + (_boss.CurrentPhase == 1
                ? combat.PhaseOnePatternInterval
                : Random.Range(0.25f, 0.4f));

            float distance = _boss.DistanceToTarget();
            float horizontalDistance = Mathf.Abs(
                _boss.TargetPlayer.transform.position.x - _boss.transform.position.x);
            float targetHeight = movement.HeightDifferenceTo(_boss.TargetPlayer.transform);
            bool playerIsGrounded = _boss.TargetPlayer.IsGrounded;
            bool playerIsAirborneAbove = !playerIsGrounded &&
                                         targetHeight >= movement.JumpTargetHeightThreshold;
            bool playerIsOnElevatedPlatform = playerIsGrounded &&
                                              targetHeight >= movement.ElevatedPlatformHeightThreshold;
            bool targetIsReachableByJump = movement.CanJumpToHeight(targetHeight);

            if (_boss.CurrentPhase > 1 && movement.IsGrounded && movement.CanJump && targetIsReachableByJump &&
                (playerIsAirborneAbove || playerIsOnElevatedPlatform))
            {
                _boss.StateMachine.ChangeState(GuardBoss.JumpState);
                return;
            }

            bool waitingBelowUnreachablePlatform = targetHeight > movement.MaxJumpReachHeight &&
                                                    horizontalDistance < movement.ApproachDashTriggerDistance;

            if (!waitingBelowUnreachablePlatform && movement.IsGrounded && distance <= combat.AttackRange &&
                combat.TryChooseGroundAttack(out GuardBossCombatComponent.AttackPattern groundAttack))
            {
                _boss.StateMachine.ChangeState(GetAttackState(groundAttack));
                return;
            }

            if (_boss.CurrentPhase > 1 && !waitingBelowUnreachablePlatform && movement.IsGrounded &&
                distance <= movement.EvadeDistance && movement.CanEvadeDash &&
                Random.value < 0.12f)
            {
                _boss.StateMachine.ChangeState(GuardBoss.EvadeDashState);
                return;
            }

            if (!waitingBelowUnreachablePlatform && movement.IsGrounded &&
                distance >= movement.ApproachDashTriggerDistance && movement.CanApproachDash)
            {
                _boss.StateMachine.ChangeState(GuardBoss.ApproachDashState);
                return;
            }

            if (_boss.CurrentPhase > 1 && !movement.IsGrounded && movement.CanAirEvadeDash &&
                distance <= movement.EvadeDistance && Random.value < 0.1f)
            {
                _boss.StateMachine.ChangeState(GuardBoss.AirEvadeDashState);
                return;
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!_boss.HasLivingTarget() || _boss.Movement == null || _boss.Combat == null)
                return;

            _boss.Movement.MoveTowardTarget(_boss.Combat.AttackRange * 0.8f);
            if (!_boss.Movement.IsGrounded)
                _boss.BossAnimation?.SetMoving(false);
        }

        private static string GetAttackState(GuardBossCombatComponent.AttackPattern pattern)
        {
            return pattern switch
            {
                GuardBossCombatComponent.AttackPattern.Basic => GuardBoss.BasicAttackState,
                GuardBossCombatComponent.AttackPattern.Combo => GuardBoss.ComboAttackState,
                GuardBossCombatComponent.AttackPattern.Heavy => GuardBoss.HeavyAttackState,
                GuardBossCombatComponent.AttackPattern.AirCombo => GuardBoss.AirComboState,
                GuardBossCombatComponent.AttackPattern.AirDash => GuardBoss.AirDashAttackState,
                _ => GuardBoss.ChaseState
            };
        }
    }

    public class GuardBossJumpState : EntityState
    {
        private readonly GuardBoss _boss;
        private bool _jumped;

        public GuardBossJumpState(GuardBoss boss) : base(boss) => _boss = boss;

        public override void Enter(Action endAction = null)
        {
            base.Enter(endAction);
            _jumped = false;
            _boss.Movement?.StartJump();
            _boss.Movement?.StopHorizontal();
            _boss.BossAnimation?.PlayJumpPrepare();
        }

        public override void Update()
        {
            base.Update();
            if (_boss.Movement == null)
                return;

            if (!_jumped && StateTimer >= _boss.Movement.JumpPreparation)
            {
                _jumped = true;
                _boss.Movement.Jump();
                _boss.StateMachine.ChangeState(GuardBoss.AirborneState);
            }
        }
    }

    public class GuardBossAirborneState : EntityState
    {
        private readonly GuardBoss _boss;
        private float _nextPatternDecisionTime;
        private bool _fallAnimationPlayed;

        public GuardBossAirborneState(GuardBoss boss) : base(boss) => _boss = boss;

        public override void Enter(Action endAction = null)
        {
            base.Enter(endAction);
            _nextPatternDecisionTime = Random.Range(0.16f, 0.26f);
            _fallAnimationPlayed = false;
            _boss.BossAnimation?.SetMoving(false);
            if (_boss.Movement != null && _boss.Movement.VerticalVelocity > 0.01f)
                _boss.BossAnimation?.PlayJump();
            else
            {
                _fallAnimationPlayed = true;
                _boss.BossAnimation?.PlayFall();
            }
        }

        public override void Update()
        {
            base.Update();
            if (_boss.Movement == null)
                return;

            if (!_fallAnimationPlayed && _boss.Movement.VerticalVelocity <= 0.01f)
            {
                _fallAnimationPlayed = true;
                _boss.BossAnimation?.PlayFall();
            }

            if (!_boss.HasLivingTarget() || _boss.Combat == null)
                return;

            if (StateTimer < _nextPatternDecisionTime)
                return;
            _nextPatternDecisionTime = StateTimer + Random.Range(0.25f, 0.4f);

            _boss.FaceTarget();
            if (_boss.DistanceToTarget() <= _boss.Combat.AttackRange &&
                _boss.Combat.TryChooseAirAttack(out GuardBossCombatComponent.AttackPattern airAttack))
            {
                _boss.StateMachine.ChangeState(airAttack == GuardBossCombatComponent.AttackPattern.AirCombo
                    ? GuardBoss.AirComboState
                    : GuardBoss.AirDashAttackState);
                return;
            }

            if (_boss.CurrentPhase > 1 && _boss.Movement.CanAirEvadeDash &&
                _boss.DistanceToTarget() <= _boss.Movement.EvadeDistance && Random.value < 0.1f)
                _boss.StateMachine.ChangeState(GuardBoss.AirEvadeDashState);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (_boss.HasLivingTarget() && _boss.Movement != null && _boss.Combat != null)
                _boss.Movement.MoveTowardTarget(_boss.Combat.AttackRange * 0.8f);
        }
    }

    public class GuardBossLandingState : EntityState
    {
        private readonly GuardBoss _boss;

        public GuardBossLandingState(GuardBoss boss) : base(boss) => _boss = boss;

        public override void Enter(Action endAction = null)
        {
            base.Enter(endAction);
            _boss.Movement?.StopHorizontal();
            _boss.BossAnimation?.PlayLanding();
        }

        public override void Update()
        {
            base.Update();
            if (StateTimer >= _boss.LandingDuration)
                _boss.StateMachine.ChangeState(_boss.HasLivingTarget()
                    ? GuardBoss.ChaseState
                    : GuardBoss.IdleState);
        }
    }

    public class GuardBossHitState : EntityState
    {
        private readonly GuardBoss _boss;

        public GuardBossHitState(GuardBoss boss) : base(boss) => _boss = boss;

        public override void Enter(Action endAction = null)
        {
            base.Enter(endAction);
            _boss.Movement?.StopHorizontal();
            _boss.BossAnimation?.PlayHit();
        }

        public override void Update()
        {
            base.Update();
            if (StateTimer >= _boss.HitStunDuration)
                _boss.StateMachine.ChangeState(_boss.HasLivingTarget()
                    ? GuardBoss.ChaseState
                    : GuardBoss.IdleState);
        }
    }

    public class GuardBossDeadState : EntityState
    {
        private readonly GuardBoss _boss;
        private bool _despawned;

        public GuardBossDeadState(GuardBoss boss) : base(boss) => _boss = boss;

        public override void Enter(Action endAction = null)
        {
            base.Enter(endAction);
            if (!_boss.IsDead)
            {
                _boss.StateMachine.ChangeState(_boss.HasLivingTarget()
                    ? GuardBoss.ChaseState
                    : GuardBoss.IdleState);
                return;
            }

            _despawned = false;
            _boss.Movement?.StopHorizontal();
            if (_boss.BossAnimation != null)
                _boss.BossAnimation.OnDead += HandleDeathAnimationEnd;
            _boss.BossAnimation?.PlayDead();
        }

        public override void Update()
        {
            base.Update();
            if (StateTimer >= _boss.DeathFallbackDuration)
                HandleDeathAnimationEnd();
        }

        public override void Exit()
        {
            if (_boss.BossAnimation != null)
                _boss.BossAnimation.OnDead -= HandleDeathAnimationEnd;
            base.Exit();
        }

        private void HandleDeathAnimationEnd()
        {
            if (_despawned)
                return;

            _despawned = true;
            _boss.BossAnimation.OnDead -= HandleDeathAnimationEnd;
            _boss.gameObject.SetActive(false);
        }
    }

    public class GuardBossVulnerableState : EntityState
    {
        private readonly GuardBoss _boss;

        public GuardBossVulnerableState(GuardBoss boss) : base(boss) => _boss = boss;

        public override void Enter(Action endAction = null)
        {
            base.Enter(endAction);
            _boss.Movement?.StopHorizontal();
            _boss.BossAnimation?.SetMoving(false);
        }

        public override void Update()
        {
            base.Update();
            if (_boss.IsDead || _boss.IsVulnerable)
                return;

            _boss.StateMachine.ChangeState(_boss.HasLivingTarget()
                ? GuardBoss.ChaseState
                : GuardBoss.IdleState);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            _boss.Movement?.StopHorizontal();
        }
    }
}
