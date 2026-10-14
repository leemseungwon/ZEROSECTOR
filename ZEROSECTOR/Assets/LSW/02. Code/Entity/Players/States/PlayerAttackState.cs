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

            _animation.OnAttackCast += CastAttack;
            _animation.OnAttackAnimationEnd += EndAttack;
            _animation.SetParam(_animation.AttackAnimationData);
        }

        public override void Update()
        {
            base.Update();

            if (_player.IsDead || _player.IsStunned)
                return;
            
            if (!_player.IsGrounded)
            {
                _player.StateMachine.ChangeState(PlayerController.JumpState);
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (_player.IsDead || _player.IsStunned)
                return;

            _player.Move();
        }

        private void CastAttack()
        {
            if(_combat == null)
                return;
            Vector2 mouseDir = ((Vector2)_player.transform.position - _player.inputData.MousePosition).normalized;
            _combat.Attack(mouseDir);
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

            if (_player.IsStunned)
            {
                _player.StateMachine.ChangeState(PlayerController.IdleState);
                return;
            }
            
            if (!_player.IsGrounded)
            {
                _player.StateMachine.ChangeState(PlayerController.JumpState);
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