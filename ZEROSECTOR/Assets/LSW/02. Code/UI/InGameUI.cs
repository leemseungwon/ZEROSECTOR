
using LSW._02._Code.Entity.Players;
using LSW._02._Code.Environments;
using LSW._02._Code.System;
using LSW._02._Code.System.HandlePlayerSystems;
using UnityEngine;

namespace LSW._02._Code.UI
{
    public class InGameUI : MonoBehaviour
    {
        private PlayerController _player;
        private bool _isBound;

        private void Start()
        {
            BindPlayer();
        }

        private void Update()
        {
            if (!_isBound)
                BindPlayer();
        }

        private void BindPlayer()
        {
            _player = GetComponentInParent<PlayerController>();

            if (_player == null && SystemHandler.Instance != null &&
                SystemHandler.Instance.GetSystem(out HandlePlayerSystem playerSystem))
            {
                _player = playerSystem.Player;
            }
            
            _isBound = true;
        }
    }
}
