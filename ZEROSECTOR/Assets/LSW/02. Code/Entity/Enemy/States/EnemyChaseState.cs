
namespace LSW._02._Code.Entity.Enemy.States
{
    public class EnemyChaseState : EntityState
    {
        private readonly BaseEnemy _enemy;

        public EnemyChaseState(BaseEnemy enemy) : base(enemy)
        {
            _enemy = enemy;
        }

        public override void Update()
        {
            base.Update();

            if (!_enemy.HasLivingTarget() || _enemy.DistanceToTarget() > _enemy.DetectionRange ||
                _enemy.IsTargetBlockedByWall())
            {
                _enemy.StateMachine.ChangeState(_enemy.PatrolWhenIdle ? BaseEnemy.PatrolState
                    : BaseEnemy.IdleState);
                return;
            }

            _enemy.FaceTarget();

            if (_enemy.DistanceToTarget() <= _enemy.AttackRange)
            {
                _enemy.StateMachine.ChangeState(BaseEnemy.AttackState);
                return;
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            _enemy.MoveTowardsTarget();
        }
    }
}
