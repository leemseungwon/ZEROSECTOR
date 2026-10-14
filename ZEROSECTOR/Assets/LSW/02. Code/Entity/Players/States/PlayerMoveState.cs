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
            _animation.SetParam(_animation.IsMoveAnimationData, true);
        }

        public override void Update()
        {
            base.Update();

            if (_player.IsDead || _player.IsStunned)
                return;

            if (!_player.IsGrounded)
            {
                _player.StateMachine.ChangeState(PlayerController.JumpState);
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

            if (_player.IsDead || _player.IsStunned)
                return;

            _player.Move();
        }
    }
}