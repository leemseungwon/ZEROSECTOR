using System;

namespace LSW._02._Code.Entity.Players.States
{
    public class PlayerFallState : EntityState
    {
        private readonly PlayerController _player;
        private readonly PlayerAnimation _animation;

        public PlayerFallState(PlayerController player) : base(player)
        {
            _player = player;
            _animation = player.Animation as PlayerAnimation;
        }

        public override void Enter(Action endAction = null)
        {
            base.Enter(endAction);
            _animation?.PlayFall();
        }

        public override void Update()
        {
            base.Update();

            if (_player.IsDead)
                return;

            if (_player.HasJumpRequest)
            {
                _player.StateMachine.ChangeState(PlayerController.JumpState);
                return;
            }

            if (!_player.IsGrounded)
                return;

            _player.ConsumeJumpInput();
            _player.StateMachine.ChangeState(_player.MoveInput != 0f
                ? PlayerController.MoveState
                : PlayerController.IdleState);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!_player.IsDead)
                _player.Move();
        }
    }
}
