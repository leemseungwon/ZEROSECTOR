using System;
using System.Collections;
using System.Collections.Generic;
using LSW._02._Code.Entity;
using LSW._02._Code.Entity.Enemy;
using LSW._02._Code.Entity.Players;
using LSW._02._Code.Entity.Players.Components;
using LSW._02._Code.System;
using LSW._02._Code.System.PoolSystems;
using UnityEngine;

namespace LSW._02._Code.Environments
{
    public class Room : MonoBehaviour
    {
        [Serializable]
        private class EnemySpawn
        {
            public string poolName;
            public Transform spawnPoint;
        }

        [SerializeField] private Collider2D roomCollider;
        [SerializeField, Min(0f)] private float restartDelay = 0.35f;
        [SerializeField] private Transform playerRespawnPoint;
        [SerializeField] private EnemySpawn[] enemies;
        [SerializeField] private RoomDoor roomDoor;

        private readonly List<BaseEnemy> _spawnedEnemies = new();
        private PlayerController _player;
        private PoolSystem _poolSystem;
        private Coroutine _restartCoroutine;
        private bool _isCutsceneActive;

        public static Room ActiveRoom { get; private set; }

        public bool IsCombatActive { get; private set; }
        public bool IsCleared { get; private set; }
        public IReadOnlyList<BaseEnemy> SpawnedEnemies => _spawnedEnemies;

        public event Action OnRoomActivated;
        public event Action OnRoomDeactivated;
        public event Action OnRoomReset;
        public event Action OnRoomCleared;
        public event Action<bool> OnCombatStateChanged;

        public bool CanOpenInventory =>
            !IsCombatActive && !_isCutsceneActive && _player != null && !_player.IsDead;

        private void Awake()
        {
            if (roomCollider == null)
            {
                roomCollider = GetComponentInChildren<Collider2D>();
                if (roomCollider == null)
                    roomCollider = GetComponent<Collider2D>();
            }
            
            else if (roomCollider.gameObject != gameObject)
            {
                var triggerListener = roomCollider.gameObject.GetComponent<RoomTriggerListener>();
                if (triggerListener == null)
                    triggerListener = roomCollider.gameObject.AddComponent<RoomTriggerListener>();
                triggerListener.Initialize(this);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleTriggerEnter(other);
        }
        
        public void HandleTriggerEnter(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null)
                Activate(player);
        }

        private void Update()
        {
            if (ActiveRoom == this)
                UpdateCombatState();
        }

        public void SetCutsceneActive(bool active)
        {
            _isCutsceneActive = active;
        }

        private void Activate(PlayerController player)
        {
            if (ActiveRoom == this)
                return;

            if (ActiveRoom != null)
                ActiveRoom.Deactivate();

            ActiveRoom = this;
            _player = player;

            if (_player != null && _player.GetCompo(out PlayerCombatComponent combat))
            {
                combat.SaveCheckpointGauge();
            }

            _player.OnDeathEvent += HandlePlayerDeath;

            OnRoomActivated?.Invoke();

            if (!IsCleared)
                ResetRoomContents();
        }

        private void Deactivate()
        {
            if (_player != null)
                _player.OnDeathEvent -= HandlePlayerDeath;

            if (_restartCoroutine != null)
            {
                StopCoroutine(_restartCoroutine);
                _restartCoroutine = null;
            }

            DespawnEnemies();
            IsCombatActive = false;

            OnRoomDeactivated?.Invoke();

            if (ActiveRoom == this)
                ActiveRoom = null;
        }

        private void HandlePlayerDeath()
        {
            if (_restartCoroutine == null)
                _restartCoroutine = StartCoroutine(RestartRoom());
        }

        private IEnumerator RestartRoom()
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, restartDelay));

            if (ActiveRoom == this && _player != null)
            {
                Vector3 respawnPosition = playerRespawnPoint != null
                    ? playerRespawnPoint.position
                    : transform.position;

                _player.ResetForRoom(respawnPosition);

                if (!IsCleared)
                    ResetRoomContents();
            }

            _restartCoroutine = null;
        }

        private void ResetRoomContents()
        {
            IsCleared = false;
            DespawnEnemies();
            SpawnEnemies();
            UpdateCombatState();

            if (roomDoor != null)
            {
                roomDoor.ResetDoor();
            }

            OnRoomReset?.Invoke();
        }

        private void SpawnEnemies()
        {
            if (enemies == null)
                return;

            if (_poolSystem == null &&
                SystemHandler.Instance != null &&
                SystemHandler.Instance.GetSystem(out PoolSystem poolSystem))
            {
                _poolSystem = poolSystem;
            }

            foreach (EnemySpawn enemy in enemies)
            {
                if (enemy == null || string.IsNullOrWhiteSpace(enemy.poolName) || enemy.spawnPoint == null)
                {
                    Debug.LogError($"Room {name} has an enemy spawn without a pool name or spawn point.", this);
                    continue;
                }

                if (_poolSystem == null || !_poolSystem.SpawnPool(enemy.poolName, out BaseEnemy spawnedEnemy))
                {
                    Debug.LogError($"Room {name} could not spawn enemy pool '{enemy.poolName}'.", this);
                    continue;
                }

                spawnedEnemy.ResetForRoom(enemy.spawnPoint.position);
                _spawnedEnemies.Add(spawnedEnemy);
            }
        }

        private void DespawnEnemies()
        {
            foreach (BaseEnemy enemy in _spawnedEnemies)
            {
                if (enemy != null)
                    enemy.DespawnFromRoom();
            }

            _spawnedEnemies.Clear();
        }
        
        private void UpdateCombatState()
        {
            bool previousCombat = IsCombatActive;
            IsCombatActive = false;
            foreach (BaseEnemy enemy in _spawnedEnemies)
            {
                if (enemy != null && enemy.gameObject.activeInHierarchy && !enemy.IsDead)
                {
                    IsCombatActive = true;
                    break;
                }
            }

            if (previousCombat != IsCombatActive)
            {
                OnCombatStateChanged?.Invoke(IsCombatActive);
            }

            if (_spawnedEnemies.Count > 0 && !IsCombatActive && !IsCleared)
            {
                ClearRoom();
            }
        }

        public void ClearRoom()
        {
            if (IsCleared)
                return;

            IsCleared = true;

            if (roomDoor != null)
            {
                roomDoor.OpenDoor();
            }

            OnRoomCleared?.Invoke();
        }

        private void OnDisable()
        {
            if (ActiveRoom == this)
                Deactivate();
        }
    }
 
    public class RoomTriggerListener : MonoBehaviour
    {
        private Room _room;

        public void Initialize(Room room)
        {
            _room = room;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_room != null)
                _room.HandleTriggerEnter(other);
        }
    }
}
