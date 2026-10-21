using System;

namespace LSW._02._Code.Entity.Players.States
{
    public class PlayerMoveState : EntityState
    {
        private readonly PlayerController _player;
        private readonly PlayerAnimation _animation;

        public PlayerMoveState(PlayerController player) : base(player)
        {
            _player = player;
            if (_player != null && _player.Animation is PlayerAnimation animation)
            {
                _animation = animation;
            }
        }

        public override void Enter(Action endAction = null)
        {
            base.Enter(endAction);
            if (_player.IsGrounded)
            {
                var velocity = _player.Rigidbody.linearVelocity;
                velocity.y = 0f;
                _player.Rigidbody.linearVelocity = velocity;
            }
            _animation.SetParam(_animation.IsMoveAnimationData, true);
        }

        public override void Update()
        {
            base.Update();

            if (_player.IsDead)
                return;

            if (!_player.IsGrounded)
            {
                _player.StateMachine.ChangeState(_player.Rigidbody.linearVelocity.y > 0f
                    ? PlayerController.JumpState
                    : PlayerController.FallState);
                return;
            }

            if (_player.HasJumpRequest)
            {
                _player.StateMachine.ChangeState(PlayerController.JumpState);
                return;
            }

            if (_player.MoveInput == 0f)
            {
                _player.StateMachine.ChangeState(PlayerController.IdleState);
                return;
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (_player.IsDead)
                return;

            _player.Move();
        }
    }
}