using System;
using LSW._02._Code.Entity.Boss.Guard_Boss.Components;

namespace LSW._02._Code.Entity.Boss.Guard_Boss.States
{
    public abstract class GuardBossAttackState : EntityState
    {
        protected readonly GuardBoss Boss;
        protected GuardBossCombatComponent Combat => Boss.Combat;
        protected bool HasAttackAnimationEnded { get; private set; }
        protected virtual bool AllowsAttackMovement => false;

        protected GuardBossAttackState(GuardBoss boss) : base(boss)
        {
            Boss = boss;
        }

        public override void Enter(Action endAction = null)
        {
            base.Enter(endAction);
            HasAttackAnimationEnded = false;
            Boss.Movement?.StopHorizontal();
            if (Boss.BossAnimation != null)
            {
                Boss.BossAnimation.OnAttackCast += HandleAttackCast;
                Boss.BossAnimation.OnAttackAnimationEnd += HandleAttackAnimationEnd;
            }
        }

        public override void Exit()
        {
            Boss.Movement?.EndAirDash();
            if (Boss.BossAnimation != null)
            {
                Boss.BossAnimation.OnAttackCast -= HandleAttackCast;
                Boss.BossAnimation.OnAttackAnimationEnd -= HandleAttackAnimationEnd;
            }
            base.Exit();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!AllowsAttackMovement)
                Boss.Movement?.StopHorizontal();
        }

        protected virtual void HandleAttackCast() { }

        private void HandleAttackAnimationEnd()
        {
            OnAttackAnimationFinished();
            HasAttackAnimationEnded = true;
        }

        protected virtual void OnAttackAnimationFinished() { }

        protected void ReturnToChase()
        {
            Boss.Combat?.StartPostAttackRecovery();
            Boss.StateMachine.ChangeState(Boss.HasLivingTarget()
                ? GuardBoss.ChaseState
                : GuardBoss.IdleState);
        }
    }

    public class GuardBossBasicAttackState : GuardBossAttackState
    {
        private bool _didAttack;
        private bool _hitTarget;
        private bool _forceMiss;

        public GuardBossBasicAttackState(GuardBoss boss) : base(boss) { }

        public override void Enter(Action endAction = null)
        {
            base.Enter(endAction);
            _didAttack = false;
            _hitTarget = false;
            _forceMiss = Combat != null && Combat.ShouldIntentionallyMissBasic();
            Combat?.StartCooldown(GuardBossCombatComponent.AttackPattern.Basic);
            Boss.BossAnimation?.PlayBasicAttack();
        }

        protected override void HandleAttackCast()
        {
            if (_didAttack || Combat == null)
                return;

            _didAttack = true;
            _hitTarget = Combat.AttackTarget(Combat.AttackRadius, _forceMiss);
        }

        protected override void OnAttackAnimationFinished()
            => Combat?.OnAttackFinished(GuardBossCombatComponent.AttackPattern.Basic, _hitTarget);

        public override void Update()
        {
            base.Update();
            if (Combat == null)
            {
                ReturnToChase();
                return;
            }

            if (HasAttackAnimationEnded && !Boss.IsVulnerable)
                ReturnToChase();
        }
    }

    public class GuardBossComboAttackState : GuardBossAttackState
    {
        private int _hitCount;
        private bool _lastHitTarget;

        public GuardBossComboAttackState(GuardBoss boss) : base(boss) { }

        public override void Enter(Action endAction = null)
        {
            base.Enter(endAction);
            _hitCount = 0;
            _lastHitTarget = false;
            Combat?.PrepareCombo();
            Combat?.StartCooldown(GuardBossCombatComponent.AttackPattern.Combo);
            Boss.BossAnimation?.PlayComboAttack();
        }

        public override void Update()
        {
            base.Update();
            if (Combat == null)
            {
                ReturnToChase();
                return;
            }

            if (HasAttackAnimationEnded && !Boss.IsVulnerable)
                ReturnToChase();
        }

        protected override void HandleAttackCast()
        {
            if (Combat == null || _hitCount >= 3)
                return;

            _lastHitTarget = Combat.AttackTarget(Combat.AttackRadius);
            _hitCount++;
            if (Combat.TryStopComboAfterHit(_hitCount))
            {
                Boss.BossAnimation?.PlayIdle();
                return;
            }
        }

        protected override void OnAttackAnimationFinished()
        {
            Combat?.CompleteCombo();
            Combat?.OnAttackFinished(GuardBossCombatComponent.AttackPattern.Combo, _lastHitTarget);
        }
    }

    public class GuardBossHeavyAttackState : GuardBossAttackState
    {
        private bool _didAttack;
        private bool _hitTarget;

        public GuardBossHeavyAttackState(GuardBoss boss) : base(boss) { }

        public override void Enter(Action endAction = null)
        {
            base.Enter(endAction);
            _didAttack = false;
            _hitTarget = false;
            Combat?.StartCooldown(GuardBossCombatComponent.AttackPattern.Heavy);
            Boss.BossAnimation?.PlayHeavyCharge();
        }

        protected override void HandleAttackCast()
        {
            if (_didAttack || Combat == null)
                return;

            _didAttack = true;
            _hitTarget = Combat.AttackTarget(Combat.HeavyAttackRadius);
        }

        protected override void OnAttackAnimationFinished()
            => Combat?.OnAttackFinished(GuardBossCombatComponent.AttackPattern.Heavy, _hitTarget);

        public override void Update()
        {
            base.Update();
            if (Combat == null)
            {
                ReturnToChase();
                return;
            }

            if (HasAttackAnimationEnded && !Boss.IsVulnerable)
                ReturnToChase();
        }
    }

    public class GuardBossAirComboState : GuardBossAttackState
    {
        private bool _launched;
        private int _hitCount;

        public GuardBossAirComboState(GuardBoss boss) : base(boss) { }

        public override void Enter(Action endAction = null)
        {
            base.Enter(endAction);
            _launched = !Boss.IsGrounded;
            _hitCount = 0;
            Combat?.StartCooldown(GuardBossCombatComponent.AttackPattern.AirCombo);
            if (_launched)
            {
                Boss.Movement?.BeginJumpAttackLift();
                Boss.BossAnimation?.PlayAirCombo();
            }
            else
            {
                Boss.Movement?.StartJump();
                Boss.BossAnimation?.PlayJumpPrepare();
            }
        }

        public override void Update()
        {
            base.Update();
            if (Combat == null || Boss.Movement == null)
            {
                ReturnToChase();
                return;
            }

            if (!_launched)
            {
                if (StateTimer >= Combat.AirComboPreparation)
                {
                    _launched = true;
                    Boss.Movement.Jump();
                    Boss.Movement.BeginJumpAttackLift();
                    StateTimer = 0f;
                    Boss.BossAnimation?.PlayAirCombo();
                }
                return;
            }

            if (HasAttackAnimationEnded && !Boss.IsVulnerable)
                ReturnToChase();
        }

        protected override void HandleAttackCast()
        {
            if (Combat == null || _hitCount >= 3)
                return;

            Combat.AttackTarget(Combat.AttackRadius);
            _hitCount++;
        }

        public override void Exit()
        {
            Boss.Movement?.SuspendGravity(false);
            base.Exit();
        }
    }

    public class GuardBossAirDashAttackState : GuardBossAttackState
    {
        private bool _launched;
        private bool _didHit;
        protected override bool AllowsAttackMovement => _launched && !HasAttackAnimationEnded;

        public GuardBossAirDashAttackState(GuardBoss boss) : base(boss) { }

        public override void Enter(Action endAction = null)
        {
            base.Enter(endAction);
            _launched = !Boss.IsGrounded;
            _didHit = false;
            Combat?.StartCooldown(GuardBossCombatComponent.AttackPattern.AirDash);
            if (_launched)
            {
                if (Combat != null)
                    Boss.Movement?.StartAirAttackDash(Combat.AirDashSpeed);
                Boss.Movement?.SuspendGravity(true);
                Boss.BossAnimation?.PlayDash();
            }
            else
            {
                Boss.Movement?.StartJump();
                Boss.BossAnimation?.PlayJumpPrepare();
            }
        }

        public override void Update()
        {
            base.Update();
            if (Combat == null || Boss.Movement == null)
            {
                ReturnToChase();
                return;
            }

            if (!_launched)
            {
                if (StateTimer >= Combat.AirDashPreparation)
                {
                    _launched = true;
                    Boss.Movement.Jump();
                    Boss.Movement.StartAirAttackDash(Combat.AirDashSpeed);
                    Boss.Movement.SuspendGravity(true);
                    StateTimer = 0f;
                    Boss.BossAnimation?.PlayDash();
                }
                return;
            }

            if (HasAttackAnimationEnded)
            {
                ReturnToChase();
            }
        }

        protected override void HandleAttackCast()
        {
            if (!_launched || _didHit || Combat == null)
                return;

            _didHit = Combat.AttackTarget(Combat.AttackRadius);
        }

        public override void Exit()
        {
            base.Exit();
            Boss.Movement?.SuspendGravity(false);
        }

    }
}
