using System;
using LSW._02._Code.Entity.Boss.Guard_Boss.Components;
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
            _nextPatternDecisionTime = Random.Range(0.12f, 0.22f);
        }

        public override void Update()
        {
            base.Update();
            if (!_boss.HasLivingTarget())
            {
                _boss.StateMachine.ChangeState(GuardBoss.IdleState);
                return;
            }

            _boss.FaceTarget();
            GuardBossMovementComponent movement = _boss.Movement;
            GuardBossCombatComponent combat = _boss.Combat;
            if (movement == null || combat == null)
                return;

            if (StateTimer < _nextPatternDecisionTime)
                return;
            _nextPatternDecisionTime = StateTimer + Random.Range(0.25f, 0.4f);

            float distance = _boss.DistanceToTarget();
            if (movement.IsGrounded && movement.CanJump && distance <= combat.AttackRange &&
                combat.TryChooseAirAttack(out GuardBossCombatComponent.AttackPattern closeAirAttack) &&
                Random.value < 0.1f)
            {
                _boss.StateMachine.ChangeState(GetAttackState(closeAirAttack));
                return;
            }

            if (movement.IsGrounded && distance <= combat.AttackRange &&
                combat.TryChooseGroundAttack(out GuardBossCombatComponent.AttackPattern groundAttack))
            {
                _boss.StateMachine.ChangeState(GetAttackState(groundAttack));
                return;
            }

            if (movement.IsGrounded && distance <= movement.EvadeDistance && movement.CanEvadeDash &&
                Random.value < 0.12f)
            {
                _boss.StateMachine.ChangeState(GuardBoss.EvadeDashState);
                return;
            }

            if (movement.IsGrounded && distance > combat.AttackRange * 1.5f &&
                movement.CanApproachDash && Random.value < 0.28f)
            {
                _boss.StateMachine.ChangeState(GuardBoss.ApproachDashState);
                return;
            }

            if (movement.IsGrounded && movement.CanJump && distance > combat.AttackRange &&
                Random.value < 0.12f)
            {
                _boss.StateMachine.ChangeState(GuardBoss.JumpState);
                return;
            }

            if (!movement.IsGrounded && movement.CanAirEvadeDash &&
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
                _boss.BossAnimation?.PlayJump();
                _boss.StateMachine.ChangeState(GuardBoss.AirborneState);
            }
        }
    }

    public class GuardBossAirborneState : EntityState
    {
        private readonly GuardBoss _boss;
        private float _nextPatternDecisionTime;

        public GuardBossAirborneState(GuardBoss boss) : base(boss) => _boss = boss;

        public override void Enter(Action endAction = null)
        {
            base.Enter(endAction);
            _nextPatternDecisionTime = Random.Range(0.16f, 0.26f);
            _boss.BossAnimation?.SetMoving(false);
            _boss.BossAnimation?.PlayJump();
        }

        public override void Update()
        {
            base.Update();
            if (!_boss.HasLivingTarget() || _boss.Movement == null || _boss.Combat == null)
                return;

            if (StateTimer < _nextPatternDecisionTime)
                return;
            _nextPatternDecisionTime = StateTimer + Random.Range(0.25f, 0.4f);

            _boss.FaceTarget();
            if (_boss.Combat.TryChooseAirAttack(out GuardBossCombatComponent.AttackPattern airAttack) &&
                Random.value < 0.12f)
            {
                _boss.StateMachine.ChangeState(airAttack == GuardBossCombatComponent.AttackPattern.AirCombo
                    ? GuardBoss.AirComboState
                    : GuardBoss.AirDashAttackState);
                return;
            }

            if (_boss.Movement.CanAirEvadeDash &&
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
}
