using System;
using LSW._02._Code.System;
using LSW._02._Code.System.HandlePlayerSystems;
using LSW._02._Code.UI;
using UnityEngine;

namespace LSW._02._Code.Entity.Players.States
{
    public class PlayerUseGunState : EntityState
    {
        private readonly PlayerController _player;
        private readonly InGameUI _inGameUI;
        private readonly PlayerAnimation _animation;

        private float _readyShootTimer;
        private float _readyShootDuration = 0.5f;
        private bool _isReadyShoot;
        private Vector2 _aimDirection;

        public PlayerUseGunState(PlayerController player) : base(player)
        {
            _player = player;
            if (_player.Animation != null && _player.Animation is PlayerAnimation animation)
            {
                _animation = animation;
            }

            if (SystemHandler.Instance.GetSystem(out HandlePlayerSystem playerSystem))
            {
                _inGameUI = playerSystem.InGameUI;
            }
        }

        public override void Enter(Action endAction = null)
        {
            base.Enter(endAction);

            if (_player.IsDead || !_player.CanUseGun())
            {
                return;
            }

            if (!_player.ConsumeGunGauge())
            {
                return;
            }

            if (_animation != null)
            {
                _animation.OnGunAnimationEnd += GunAnimationEnd;
            }
            
            if(_inGameUI != null)
                _inGameUI.ShowPoint(_readyShootDuration);
            
            _player.Rigidbody.linearVelocity = Vector3.zero;

            _isReadyShoot = true;
            _readyShootTimer = _readyShootDuration;

            _player.SetGunReadyAnimation();
            Time.timeScale = 0.5f;
        }

        public override void Exit()
        {
            if (_animation != null)
            {
                _animation.OnGunAnimationEnd -= GunAnimationEnd;
            }
            
            if (_isReadyShoot)
            {
                _isReadyShoot = false;
                Time.timeScale = 1f;
            }
            
            base.Exit();
        }

        private void GunAnimationEnd()
        {
            if(_player != null)
            {
                _player.StateMachine.ChangeState(PlayerController.IdleState);
            }
        }

        public override void Update()
        {
            base.Update();
            
            if (!_isReadyShoot)
                return;

            _readyShootTimer -= Time.unscaledDeltaTime;

            if (_readyShootTimer <= 0f)
            {
                _isReadyShoot = false;
                Time.timeScale = 1f;
                _player.ResetGunReadyAnimation();
            }
        }

        public void ExecuteGunCast()
        {
            if (_player.IsDead)
                return;

            _player.ExecuteGunRaycast();
        }
    }
}