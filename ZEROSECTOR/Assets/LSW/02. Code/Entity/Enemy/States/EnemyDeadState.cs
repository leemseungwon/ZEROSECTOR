using System;

namespace LSW._02._Code.Entity.Enemy.States
{
    public class EnemyDeadState : EntityState
    {
        private readonly BaseEnemy _enemy;
        private readonly BaseEnemyAnimation _animation;
        private bool _despawned;

        public EnemyDeadState(BaseEnemy enemy) : base(enemy)
        {
            _enemy = enemy;
            _enemy.GetCompo(out _animation);
        }

        public override void Enter(Action endAction = null)
        {
            base.Enter(endAction);
            _despawned = false;
            _enemy.StopHorizontalMovement();
            if (_animation != null)
                _animation.OnDead += OnDeadAnimationEnd;
            _enemy.PlayDeadAnimation();
        }

        public override void Exit()
        {
            if (_animation != null)
                _animation.OnDead -= OnDeadAnimationEnd;

            base.Exit();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            _enemy.StopHorizontalMovement();
        }

        private void OnDeadAnimationEnd()
        {
            if (_despawned)
                return;

            _despawned = true;
            if (_animation != null)
                _animation.OnDead -= OnDeadAnimationEnd;
            _enemy.DespawnAfterDeath();
        }
    }
}
