using System;

namespace LSW._02._Code.Entity.Enemy.States
{
    public class EnemyIdleState : EntityState
    {
        private readonly BaseEnemy _enemy;

        public EnemyIdleState(BaseEnemy enemy) : base(enemy)
        {
            _enemy = enemy;
        }

        public override void Enter(Action endAction = null)
        {
            base.Enter(endAction);
            _enemy.StopHorizontalMovement();
        }

        public override void Update()
        {
            base.Update();

            if (_enemy.HasLivingTarget())
            {
                if (_enemy.DistanceToTarget() <= _enemy.DetectionRange)
                    _enemy.StateMachine.ChangeState(BaseEnemy.ChaseState);
                else if (StateTimer >= _enemy.IdleDuration)
                    _enemy.StateMachine.ChangeState(BaseEnemy.PatrolState);
                return;
            }

            if (StateTimer >= _enemy.IdleDuration)
                _enemy.StateMachine.ChangeState(BaseEnemy.PatrolState);
        }
    }
}
