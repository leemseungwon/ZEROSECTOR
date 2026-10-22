using System;
using UnityEngine;

namespace LSW._02._Code.Entity.Players.States
{
    public class PlayerAttackState : EntityState
    {
        private readonly PlayerController _player;
        private readonly PlayerAnimation _animation;
        private readonly EntityCombatComponent _combat;

        public PlayerAttackState(PlayerController player) : base(player)
        {
            _player = player;
            if (_player != null && _player.Animation is PlayerAnimation animation)
            {
                _animation = animation;
            }
            if(_player != null)
            {
                _player.GetCompo(out _combat);
            }
        }

        public override void Enter(Action endAction = null)
        {
            base.Enter(endAction);

            if (_animation == null || _combat == null)
                return;

            if (!_player.IsGrounded)
            {
                _player.StateMachine.ChangeState(_player.Rigidbody.linearVelocity.y > 0f
                    ? PlayerController.JumpState
                    : PlayerController.FallState);
                return;
            }

            _animation.OnAttackCast += CastAttack;
            _animation.OnAttackAnimationEnd += EndAttack;
            _animation.SetParam(_animation.AttackAnimationData);
        }

        public override void Update()
        {
            base.Update();

            if (_player.IsDead)
                return;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (_player.IsDead)
                return;

            _player.Move();
        }

        private void CastAttack()
        {
            if (_player.IsGrounded)
                _combat.Attack(_player.AimDirection);
        }

        public override void Exit()
        {
            if (_animation != null)
            {
                _animation.OnAttackCast -= CastAttack;
                _animation.OnAttackAnimationEnd -= EndAttack;
            }

            base.Exit();
        }

        private void EndAttack()
        {
            if (_player.IsDead)
                return;

            if (!_player.IsGrounded)
            {
                _player.StateMachine.ChangeState(_player.Rigidbody.linearVelocity.y > 0f
                    ? PlayerController.JumpState
                    : PlayerController.FallState);
                return;
            }

            if (_player.MoveInput != 0f)
            {
                _player.StateMachine.ChangeState(PlayerController.MoveState);
            }
            else
            {
                _player.StateMachine.ChangeState(PlayerController.IdleState);
            }
        }
    }
}