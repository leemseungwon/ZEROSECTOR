using System;
using LSW._02._Code.Entity.Boss.Guard_Boss.Components;

namespace LSW._02._Code.Entity.Boss.Guard_Boss.States
{
    public abstract class GuardBossAttackState : EntityState
    {
        protected readonly GuardBoss Boss;
        protected GuardBossCombatComponent Combat => Boss.Combat;
        protected virtual bool AllowsHorizontalMovement => false;

        protected GuardBossAttackState(GuardBoss boss) : base(boss)
        {
            Boss = boss;
        }

        public override void Enter(Action endAction = null)
        {
            base.Enter(endAction);
            if (!AllowsHorizontalMovement)
                Boss.Movement?.StopHorizontal();
            Boss.FaceTarget();
            if (Boss.BossAnimation != null)
                Boss.BossAnimation.OnAttackCast += HandleAttackCast;
        }

        public override void Exit()
        {
            if (Boss.BossAnimation != null)
                Boss.BossAnimation.OnAttackCast -= HandleAttackCast;
            base.Exit();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!AllowsHorizontalMovement)
                Boss.Movement?.StopHorizontal();
        }

        protected virtual void HandleAttackCast() { }

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

        public GuardBossBasicAttackState(GuardBoss boss) : base(boss) { }

        public override void Enter(Action endAction = null)
        {
            base.Enter(endAction);
            _didAttack = false;
            Combat?.StartCooldown(GuardBossCombatComponent.AttackPattern.Basic);
            Boss.BossAnimation?.PlayBasicAttack();
        }

        protected override void HandleAttackCast()
        {
            if (_didAttack || Combat == null)
                return;

            _didAttack = true;
            Combat.AttackTarget(Combat.AttackRadius);
        }

        public override void Update()
        {
            base.Update();
            if (Combat == null)
            {
                ReturnToChase();
                return;
            }

            if (!_didAttack && StateTimer >= Combat.BasicWindup)
                HandleAttackCast();

            if (StateTimer >= Combat.BasicWindup + Combat.BasicRecovery)
                ReturnToChase();
        }
    }

    public class GuardBossComboAttackState : GuardBossAttackState
    {
        private int _hitCount;
        private float _nextHitTime;
        private float _lastHitTime;

        public GuardBossComboAttackState(GuardBoss boss) : base(boss) { }

        public override void Enter(Action endAction = null)
        {
            base.Enter(endAction);
            _hitCount = 0;
            _nextHitTime = Combat != null ? Combat.ComboFirstWindup : 0f;
            _lastHitTime = 0f;
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

            if (_hitCount < 3 && StateTimer >= _nextHitTime)
                HandleAttackCast();

            if (_hitCount >= 3 && StateTimer >= _lastHitTime + Combat.ComboRecovery)
                ReturnToChase();
        }

        protected override void HandleAttackCast()
        {
            if (Combat == null || _hitCount >= 3)
                return;

            Combat.AttackTarget(Combat.AttackRadius);
            _hitCount++;
            _lastHitTime = StateTimer;
            _nextHitTime += Combat.ComboHitInterval;
        }
    }

    public class GuardBossHeavyAttackState : GuardBossAttackState
    {
        private bool _didAttack;

        public GuardBossHeavyAttackState(GuardBoss boss) : base(boss) { }

        public override void Enter(Action endAction = null)
        {
            base.Enter(endAction);
            _didAttack = false;
            Combat?.StartCooldown(GuardBossCombatComponent.AttackPattern.Heavy);
            Boss.BossAnimation?.PlayHeavyCharge();
        }

        protected override void HandleAttackCast()
        {
            if (_didAttack || Combat == null)
                return;

            _didAttack = true;
            Combat.AttackTarget(Combat.HeavyAttackRadius);
        }

        public override void Update()
        {
            base.Update();
            if (Combat == null)
            {
                ReturnToChase();
                return;
            }

            if (!_didAttack && StateTimer >= Combat.HeavyChargeTime)
                HandleAttackCast();

            if (StateTimer >= Combat.HeavyChargeTime + Combat.HeavyRecovery)
                ReturnToChase();
        }
    }

    public class GuardBossAirComboState : GuardBossAttackState
    {
        private bool _launched;
        private int _hitCount;
        private float _nextHitTime;
        private float _lastHitTime;

        public GuardBossAirComboState(GuardBoss boss) : base(boss) { }

        public override void Enter(Action endAction = null)
        {
            base.Enter(endAction);
            _launched = !Boss.IsGrounded;
            _hitCount = 0;
            _lastHitTime = 0f;
            Combat?.StartCooldown(GuardBossCombatComponent.AttackPattern.AirCombo);
            if (_launched)
            {
                _nextHitTime = Combat != null ? Combat.AirComboFirstWindup : 0f;
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
                    StateTimer = 0f;
                    _nextHitTime = Combat.AirComboFirstWindup;
                    _lastHitTime = 0f;
                    Boss.BossAnimation?.PlayAirCombo();
                }
                return;
            }

            if (_hitCount < 3 && StateTimer >= _nextHitTime)
            {
                Combat.AttackTarget(Combat.AttackRadius);
                _hitCount++;
                _lastHitTime = StateTimer;
                _nextHitTime += Combat.AirComboHitInterval;
            }

            if (_hitCount >= 3 && StateTimer >= _lastHitTime + Combat.AirComboRecovery)
                ReturnToChase();
        }
    }

    public class GuardBossAirDashAttackState : GuardBossAttackState
    {
        private bool _launched;
        private bool _didHit;
        protected override bool AllowsHorizontalMovement =>
            _launched && Combat != null && StateTimer < Combat.AirDashDuration;

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
                    StateTimer = 0f;
                    Boss.BossAnimation?.PlayDash();
                }
                return;
            }

            if (!_didHit && StateTimer >= Combat.AirDashHitDelay)
                HandleAttackCast();

            if (StateTimer >= Combat.AirDashDuration)
            {
                Boss.Movement.StopHorizontal();
                ReturnToChase();
            }
        }

        protected override void HandleAttackCast()
        {
            if (!_launched || _didHit || Combat == null)
                return;

            _didHit = Combat.AttackTarget(Combat.AttackRadius);
        }

    }
}
