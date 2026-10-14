using LSW._02._Code.Entity.Players;
using LSW._02._Code.System;
using LSW._02._Code.System.HandlePlayerSystems;
using UnityEngine;

namespace LSW._02._Code.UI
{
    public class InGameUI : MonoBehaviour
    {
        private PlayerController _player;

        private void Start()
        {
            BindPlayer();
        }

        private void Update()
        {
            if (_player == null)
                BindPlayer();
        }

        private void BindPlayer()
        {
            if (SystemHandler.Instance == null || 
                !SystemHandler.Instance.GetSystem(out HandlePlayerSystem playerSystem) ||
                playerSystem.Player == null)
                return;
            

            _player = playerSystem.Player;
        }
    }
}
