using System;

namespace LSW._02._Code.Entity.Enemy.States
{
    public class EnemyAttackState : EntityState
    {
        private readonly BaseEnemy _enemy;
        private readonly BaseEnemyAnimation _animation;
        private bool _attackCast;

        public EnemyAttackState(BaseEnemy enemy) : base(enemy)
        {
            _enemy = enemy;
            _enemy.GetCompo(out _animation);
        }

        public override void Enter(Action endAction = null)
        {
            base.Enter(endAction);
            _attackCast = false;
            _enemy.StopHorizontalMovement();
            _enemy.FaceTarget();
            if (_animation != null)
                _animation.OnAttackCast += AttackCast;
            _enemy.PlayAttackAnimation();
        }

        public override void Update()
        {
            base.Update();

            if (!_enemy.HasLivingTarget() ||
                _enemy.DistanceToTarget() > _enemy.AttackRange)
            {
                _enemy.StateMachine.ChangeState(BaseEnemy.ChaseState);
                return;
            }

            if (StateTimer >= _enemy.AttackCooldown)
                _enemy.StateMachine.ChangeState(BaseEnemy.ChaseState);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            _enemy.StopHorizontalMovement();
        }

        public override void Exit()
        {
            if (_animation != null)
                _animation.OnAttackCast -= AttackCast;

            base.Exit();
        }

        private void AttackCast()
        {
            if (_attackCast)
                return;

            _attackCast = true;
            _enemy.AttackTarget();
        }
    }
}
