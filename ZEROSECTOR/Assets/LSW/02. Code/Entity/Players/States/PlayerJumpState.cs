using System;

namespace LSW._02._Code.Entity.Players.States
{
    public class PlayerJumpState : EntityState
    {
        private readonly PlayerController _player;
        private readonly PlayerAnimation _animation;

        public PlayerJumpState(PlayerController player) : base(player)
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

            if (_player.HasJumpRequest)
            {
                PlayJumpIfRequested();
            }
        }

        public override void Update()
        {
            base.Update();

            if (_player.IsDead)
                return;

            if (PlayJumpIfRequested())
                return;

            if (StateTimer < 0.05f)
                return;

            if (_player.IsGrounded)
            {
                _player.ConsumeJumpInput();
                var velocity = _player.Rigidbody.linearVelocity;
                velocity.y = 0f;
                _player.Rigidbody.linearVelocity = velocity;

                _player.StateMachine.ChangeState(_player.MoveInput != 0f ? 
                    PlayerController.MoveState : PlayerController.IdleState
                );
                return;
            }

            if (_player.Rigidbody.linearVelocity.y <= 0f)
                _player.StateMachine.ChangeState(PlayerController.FallState);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (_player.IsDead)
                return;
            _player.Move();
        }

        private bool PlayJumpIfRequested()
        {
            if (!_player.Jump())
                return false;

            _animation.PlayJump();
            return true;
        }
    }
}