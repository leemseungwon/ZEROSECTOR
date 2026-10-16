using System.Collections;
using System.Collections.Generic;
using System;
using LSW._02._Code.Combat;
using LSW._02._Code.Entity.Enemy;
using LSW._02._Code.Entity.Players;
using LSW._02._Code.Sos.EntityStatDatas;
using LSW._02._Code.System;
using LSW._02._Code.System.PoolSystems;
using UnityEngine;

namespace LSW._02._Code.Entity.Rooms
{
    [RequireComponent(typeof(Collider2D))]
    public class Room : MonoBehaviour
    {
        [Serializable]
        private class EnemySpawn
        {
            public string poolName;
            public Transform spawnPoint;
        }

        [SerializeField, Min(0f)] private float restartDelay = 0.35f;
        [SerializeField] private Transform playerRespawnPoint;
        [SerializeField] private EnemySpawn[] enemies;
        [SerializeField] private WeaponPickup[] roomPickups;

        private readonly List<BaseEnemy> _spawnedEnemies = new();
        private readonly HashSet<WeaponPickup> _pickups = new();
        private PlayerController _player;
        private PoolSystem _poolSystem;
        private Coroutine _restartCoroutine;
        private bool _isCutsceneActive;

        public static Room ActiveRoom { get; private set; }

        public bool IsCombatActive { get; private set; }
        public bool CanOpenInventory =>
            !IsCombatActive && !_isCutsceneActive && _player != null && !_player.IsDead;

        private void Awake()
        {
            Collider2D roomCollider = GetComponent<Collider2D>();
            if (roomCollider == null || !roomCollider.isTrigger)
                Debug.LogError($"{nameof(Room)} on {name} requires a trigger Collider2D.", this);

            if (playerRespawnPoint == null)
                Debug.LogError($"{nameof(Room)} on {name} requires a player respawn point.", this);

            if (roomPickups != null)
            {
                foreach (WeaponPickup pickup in roomPickups)
                    RegisterPickup(pickup);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
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

        public void RegisterPickup(WeaponPickup pickup)
        {
            if (pickup != null)
                _pickups.Add(pickup);
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
            _player.OnDeathEvent += HandlePlayerDeath;
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
            ResetPickups();
            IsCombatActive = false;

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
                ResetRoomContents();
            }

            _restartCoroutine = null;
        }

        private void ResetRoomContents()
        {
            DespawnEnemies();
            ResetPickups();
            SpawnEnemies();
            UpdateCombatState();
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

        private void ResetPickups()
        {
            foreach (WeaponPickup pickup in _pickups)
            {
                if (pickup != null)
                    pickup.ResetForRoom();
            }
        }

        private void UpdateCombatState()
        {
            IsCombatActive = false;
            foreach (BaseEnemy enemy in _spawnedEnemies)
            {
                if (enemy != null && enemy.gameObject.activeInHierarchy && !enemy.IsDead)
                {
                    IsCombatActive = true;
                    break;
                }
            }
        }

        private void OnDisable()
        {
            if (ActiveRoom == this)
                Deactivate();
        }
    }
}
