
namespace LSW._02._Code.Entity.Enemy.States
{
    public class EnemyPatrolState : EntityState
    {
        private readonly BaseEnemy _enemy;

        public EnemyPatrolState(BaseEnemy enemy) : base(enemy)
        {
            _enemy = enemy;
        }

        public override void Update()
        {
            base.Update();

            if (_enemy.HasLivingTarget() &&
                _enemy.DistanceToTarget() <= _enemy.DetectionRange &&
                !_enemy.IsTargetBlockedByWall())
            {
                _enemy.StateMachine.ChangeState(BaseEnemy.ChaseState);
                return;
            }

            if (!_enemy.PatrolWhenIdle)
            {
                _enemy.StateMachine.ChangeState(BaseEnemy.IdleState);
                return;
            }

            if (_enemy.PatrolDistance <= 0f)
            {
                _enemy.StateMachine.ChangeState(BaseEnemy.IdleState);
                return;
            }

            if (_enemy.IsAtPatrolPoint())
            {
                _enemy.ReversePatrolDirection();
                _enemy.StateMachine.ChangeState(BaseEnemy.IdleState);
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            _enemy.MoveTowardsPatrolPoint();
        }
    }
}
