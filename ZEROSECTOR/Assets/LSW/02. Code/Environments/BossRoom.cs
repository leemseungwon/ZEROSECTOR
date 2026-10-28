using System;
using System.Collections;
using LSW._02._Code.Entity;
using LSW._02._Code.Entity.Boss;
using LSW._02._Code.Entity.Players;
using LSW._02._Code.Entity.Players.Components;
using LSW._02._Code.System;
using LSW._02._Code.System.HandlePlayerSystems;
using LSW._02._Code.System.PoolSystems;
using UnityEngine;

namespace LSW._02._Code.Environments
{
    public class BossRoom : MonoBehaviour
    {
        [SerializeField] private Collider2D roomCollider;
        [SerializeField, Min(0f)] private float restartDelay = 0.35f;
        [SerializeField] private Transform playerRespawnPoint;
        [SerializeField] private string bossPoolName = "Boss_Guard";
        [SerializeField] private Transform bossSpawnPoint;
        [SerializeField] private RoomDoor roomDoor;

        private PlayerController _player;
        private BaseBoss _spawnedBoss;
        private PoolSystem _poolSystem;
        private Coroutine _restartCoroutine;
        private bool _isCutsceneActive;

        public static BossRoom ActiveRoom { get; private set; }

        public bool IsCombatActive { get; private set; }
        public bool IsCleared { get; private set; }
        public BaseBoss SpawnedBoss => _spawnedBoss;
        public bool CanOpenInventory =>
            !IsCombatActive && !_isCutsceneActive && _player != null && !_player.IsDead;

        public event Action OnRoomActivated;
        public event Action OnRoomDeactivated;
        public event Action OnRoomReset;
        public event Action OnRoomCleared;
        public event Action<bool> OnCombatStateChanged;

        private void Awake()
        {
            if (roomCollider == null)
            {
                roomCollider = GetComponentInChildren<Collider2D>();
                if (roomCollider == null)
                    roomCollider = GetComponent<Collider2D>();
            }

            if (roomCollider != null && roomCollider.gameObject != gameObject)
            {
                BossRoomTriggerListener listener = roomCollider.GetComponent<BossRoomTriggerListener>();
                if (listener == null)
                    listener = roomCollider.gameObject.AddComponent<BossRoomTriggerListener>();
                listener.Initialize(this);
            }

            CachePoolSystem();
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

            if (Room.ActiveRoom != null)
                Room.ActiveRoom.DeactivateFromTransition();

            if (ActiveRoom != null)
                ActiveRoom.Deactivate();

            ActiveRoom = this;
            _player = player;
            if (_player.GetCompo(out PlayerCombatComponent combat))
                combat.SaveCheckpointGauge();

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

            DespawnBoss();
            SetCombatState(false);
            OnRoomDeactivated?.Invoke();

            if (ActiveRoom == this)
                ActiveRoom = null;
        }

        public void DeactivateFromTransition()
        {
            Deactivate();
        }

        private void HandlePlayerDeath(BaseEntity _)
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
            DespawnBoss();
            if (roomDoor != null)
                roomDoor.ResetDoor();

            SpawnBoss();
            UpdateCombatState();
            OnRoomReset?.Invoke();
        }

        private void SpawnBoss()
        {
            CachePoolSystem();
            if (string.IsNullOrWhiteSpace(bossPoolName) || bossSpawnPoint == null)
            {
                Debug.LogError("BossRoom needs a boss pool name and spawn point.", this);
                return;
            }

            if (_poolSystem == null || !_poolSystem.SpawnPool(bossPoolName, out _spawnedBoss))
            {
                Debug.LogError($"Boss pool '{bossPoolName}' could not spawn a BaseBoss.", this);
                _spawnedBoss = null;
                return;
            }

            _spawnedBoss.OnDeathEvent += HandleBossDeath;
            _spawnedBoss.ResetForRoom(bossSpawnPoint.position);
        }

        private void HandleBossDeath(BaseEntity _)
        {
            ClearRoom();
        }

        private void DespawnBoss()
        {
            if (_spawnedBoss == null)
                return;

            _spawnedBoss.OnDeathEvent -= HandleBossDeath;
            if (_poolSystem != null && _poolSystem.HasPool(bossPoolName))
                _poolSystem.DespawnPool(bossPoolName, _spawnedBoss);
            else
            {
                _spawnedBoss.Despawn();
                _spawnedBoss.gameObject.SetActive(false);
            }

            _spawnedBoss = null;
        }

        private void UpdateCombatState()
        {
            bool combatActive = _spawnedBoss != null &&
                                _spawnedBoss.gameObject.activeInHierarchy &&
                                !_spawnedBoss.IsDead;
            SetCombatState(combatActive);

            if (_spawnedBoss != null && !combatActive && !IsCleared)
                ClearRoom();
        }

        private void SetCombatState(bool active)
        {
            if (IsCombatActive == active)
                return;

            IsCombatActive = active;
            OnCombatStateChanged?.Invoke(IsCombatActive);
        }

        public void ClearRoom()
        {
            if (IsCleared)
                return;

            IsCleared = true;
            SetCombatState(false);
            roomDoor?.OpenDoor();
            OnRoomCleared?.Invoke();
        }

        private void CachePoolSystem()
        {
            if (_poolSystem != null || SystemHandler.Instance == null)
                return;

            SystemHandler.Instance.GetSystem(out _poolSystem);
        }

        private void OnDisable()
        {
            if (ActiveRoom == this)
                Deactivate();
        }
    }

    public class BossRoomTriggerListener : MonoBehaviour
    {
        private BossRoom _room;

        public void Initialize(BossRoom room)
        {
            _room = room;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            _room?.HandleTriggerEnter(other);
        }
    }
}
