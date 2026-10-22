
using LSW._02._Code.Entity.Players;
using LSW._02._Code.System;
using LSW._02._Code.System.HandlePlayerSystems;
using UnityEngine;
using UnityEngine.UI;

namespace LSW._02._Code.UI
{
    public class InGameUI : MonoBehaviour
    {
        [field:SerializeField] public Slider GaugeSlider { get; private set; }
        [field:SerializeField] public ShootPointUI ShootPoint { get; private set; }
        
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
        
        public void UpdateGauge(float value, float max) 
            => GaugeSlider.value = Mathf.Clamp01(value / max);

        public void ShowPoint(float duration)
        {
            ShootPoint.gameObject.SetActive(true);
            ShootPoint.Show(duration);
        }
    }
}
