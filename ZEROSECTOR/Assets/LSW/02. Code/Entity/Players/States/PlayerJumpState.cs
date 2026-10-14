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
                _player.Jump();
                _animation.SetParam(_animation.JumpAnimationData);    
            }
        }

        public override void Update()
        {
            base.Update();

            if (_player.IsDead || _player.IsStunned)
                return;

            if (StateTimer < 0.05f)
                return;

            if (_player.IsGrounded)
            {
                _player.ConsumeJumpInput();

                _player.StateMachine.ChangeState(_player.MoveInput != 0f ? 
                    PlayerController.MoveState : PlayerController.IdleState
                );
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (_player.IsDead || _player.IsStunned)
                return;
            _player.Move();
        }
    }
}