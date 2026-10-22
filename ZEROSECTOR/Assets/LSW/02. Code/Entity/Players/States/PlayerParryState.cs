using System;

namespace LSW._02._Code.Entity.Players.States
{
    public class PlayerParryState : EntityState
    {
        private readonly PlayerController _player;

        public PlayerParryState(PlayerController player) : base(player)
        {
            _player = player;
        }

        public override void Enter(Action endAction = null)
        {
            base.Enter(endAction);
            _player.BeginParryState();
        }

        public override void Exit()
        {
            _player.EndParryState();
            base.Exit();
        }
    }
}
