using System;

namespace LSW._02._Code.Entity.Players.States
{
    public class PlayerDashState : EntityState
    {
        private readonly PlayerController _player;

        public PlayerDashState(PlayerController player) : base(player)
        {
            _player = player;
        }

        public override void Enter(Action endAction = null)
        {
            base.Enter(endAction);
            _player.PerformDash();
        }

        public override void Exit()
        {
            if (_player.WasParrySucceeded)
            {
                _player.ApplyPostParryInvincibility();
                _player.ConsumeParrySuccess();
            }
            else
            {
                _player.ApplyPostDashInvincibility();
            }

            base.Exit();
        }

        public override void Update()
        {
            base.Update();

            if (_player.IsDead || StateTimer < _player.DashDuration)
                return;

            if (!_player.IsGrounded)
            {
                _player.StateMachine.ChangeState(
                    _player.Rigidbody.linearVelocity.y > 0f
                        ? PlayerController.JumpState
                        : PlayerController.FallState);
                return;
            }

            _player.StateMachine.ChangeState(_player.MoveInput != 0f
                ? PlayerController.MoveState
                : PlayerController.IdleState);
        }
    }
}
