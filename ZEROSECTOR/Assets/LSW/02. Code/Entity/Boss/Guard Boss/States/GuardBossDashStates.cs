using System;

namespace LSW._02._Code.Entity.Boss.Guard_Boss.States
{
    public class GuardBossApproachDashState : EntityState
    {
        private readonly GuardBoss _boss;

        public GuardBossApproachDashState(GuardBoss boss) : base(boss) => _boss = boss;

        public override void Enter(Action endAction = null)
        {
            base.Enter(endAction);
            _boss.Movement?.StartApproachDash();
            _boss.BossAnimation?.PlayDash();
        }

        public override void Update()
        {
            base.Update();
            if (_boss.Movement == null || StateTimer >= _boss.Movement.ApproachDashDuration)
            {
                _boss.Movement?.StopHorizontal();
                ReturnToChase();
            }
        }

        private void ReturnToChase() => _boss.StateMachine.ChangeState(
            _boss.HasLivingTarget() ? GuardBoss.ChaseState : GuardBoss.IdleState);
    }

    public class GuardBossEvadeDashState : EntityState
    {
        private readonly GuardBoss _boss;

        public GuardBossEvadeDashState(GuardBoss boss) : base(boss) => _boss = boss;

        public override void Enter(Action endAction = null)
        {
            base.Enter(endAction);
            _boss.Movement?.FaceTarget();
            _boss.Movement?.StartEvadeDash(_boss.Movement.EvadeDashSpeed);
            _boss.BossAnimation?.PlayDash();
        }

        public override void Update()
        {
            base.Update();
            if (_boss.Movement == null || StateTimer >= _boss.Movement.EvadeDashDuration)
            {
                _boss.Movement?.StopHorizontal();
                _boss.StateMachine.ChangeState(_boss.HasLivingTarget()
                    ? GuardBoss.ChaseState
                    : GuardBoss.IdleState);
            }
        }
    }

    public class GuardBossAirEvadeDashState : EntityState
    {
        private readonly GuardBoss _boss;

        public GuardBossAirEvadeDashState(GuardBoss boss) : base(boss) => _boss = boss;

        public override void Enter(Action endAction = null)
        {
            base.Enter(endAction);
            _boss.Movement?.FaceTarget();
            _boss.Movement?.StartAirEvadeDash();
            _boss.BossAnimation?.PlayDash();
        }

        public override void Update()
        {
            base.Update();
            if (_boss.Movement == null || StateTimer >= _boss.Movement.AirEvadeDashDuration)
            {
                _boss.Movement?.StopHorizontal();
                _boss.StateMachine.ChangeState(_boss.HasLivingTarget()
                    ? GuardBoss.ChaseState
                    : GuardBoss.IdleState);
            }
        }
    }
}
