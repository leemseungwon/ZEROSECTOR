
using System.Collections.Generic;
using LSW._02._Code.Entity.Boss;
using LSW._02._Code.Entity.Enemy;
using LSW._02._Code.Entity.Players;
using LSW._02._Code.Environments;
using LSW._02._Code.System;
using LSW._02._Code.System.HandlePlayerSystems;
using LSW._02._Code.System.PoolSystems;
using UnityEngine;
using UnityEngine.UI;

namespace LSW._02._Code.UI
{
    public class InGameUI : MonoBehaviour
    {
        [field:SerializeField] public Slider GaugeSlider { get; private set; }
        [field:SerializeField] public ShootPointUI ShootPoint { get; private set; }
        
        [Header("Enemy Direction Arrow UI")]
        [SerializeField] private string enemyArrowPoolName;
        [SerializeField] private Camera targetCamera;

        [Header("Boss Status UI" )]
        [SerializeField] private BossStatusUI bossStatusUI;
        
        private PlayerController _player;
        private PoolSystem _poolSystem;
        private Canvas _canvas;
        private bool _isBound;
        private readonly Dictionary<BaseEnemy, EnemyDirectionArrowUI> _enemyArrows = new();
        private BossRoom _bossRoom;
        private BaseBoss _trackedBoss;

        private void Start()
        {
            _canvas = GetComponent<Canvas>();
            BindPlayer();
            SystemHandler.Instance.GetSystem(out _poolSystem);
        }

        private void Update()
        {
            if (!_isBound)
                BindPlayer();

            UpdateBossRoomBinding();
        }

        private void UpdateBossRoomBinding()
        {
            BossRoom activeRoom = BossRoom.ActiveRoom;
            if (activeRoom != _bossRoom)
            {
                UnbindBossRoom();
                _bossRoom = activeRoom;
                if (_bossRoom != null)
                {
                    _bossRoom.OnRoomReset += HandleBossRoomReset;
                    _bossRoom.OnRoomCleared += HideBossStatus;
                    _bossRoom.OnRoomDeactivated += HideBossStatus;
                }
            }

            if (_bossRoom != null && _bossRoom.SpawnedBoss != _trackedBoss)
                BindBoss(_bossRoom.SpawnedBoss);
        }

        private void HandleBossRoomReset()
        {
            BindBoss(_bossRoom != null ? _bossRoom.SpawnedBoss : null);
        }

        private void BindBoss(BaseBoss boss)
        {
            _trackedBoss = boss;
            if (_trackedBoss == null || _trackedBoss.IsDead || !_trackedBoss.gameObject.activeInHierarchy)
                bossStatusUI?.Hide();
            else
                bossStatusUI?.ShowBoss(_trackedBoss);
        }

        private void HideBossStatus()
        {
            bossStatusUI?.Hide();
        }

        private void UnbindBossRoom()
        {
            if (_bossRoom != null)
            {
                _bossRoom.OnRoomReset -= HandleBossRoomReset;
                _bossRoom.OnRoomCleared -= HideBossStatus;
                _bossRoom.OnRoomDeactivated -= HideBossStatus;
            }

            _trackedBoss = null;
            HideBossStatus();
        }

        private void OnDestroy()
        {
            UnbindBossRoom();
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
        
        public void CreateEnemyArrows(List<BaseEnemy> spawnedEnemies, bool isCleared)
        {
            ClearEnemyArrows();

            if (isCleared || _canvas == null)
                return;

            Camera cam = targetCamera != null ? targetCamera : Camera.main;

            if (cam == null)
                return;

            foreach (BaseEnemy enemy in spawnedEnemies)
            {
                if (enemy == null || enemy.IsDead || !enemy.gameObject.activeInHierarchy)
                    continue;

                if (_poolSystem.SpawnPool(enemyArrowPoolName, out EnemyDirectionArrowUI arrow))
                {
                    arrow.Initialize(enemy.transform, _canvas, cam);
                    _enemyArrows[enemy] = arrow;
                }
            }
        }

        public void ClearEnemyArrow(BaseEnemy enemy)
        {
            if (enemy == null || !_enemyArrows.TryGetValue(enemy, out EnemyDirectionArrowUI arrow))
                return;

            if (arrow != null && _poolSystem != null)
                _poolSystem.DespawnPool(enemyArrowPoolName, arrow);

            _enemyArrows.Remove(enemy);
        }

        public void ClearEnemyArrows()
        {
            foreach (EnemyDirectionArrowUI arrow in _enemyArrows.Values)
            {
                if (arrow != null)
                {
                    _poolSystem.DespawnPool(enemyArrowPoolName, arrow);
                }
            }

            _enemyArrows.Clear();
        }
    }
}
