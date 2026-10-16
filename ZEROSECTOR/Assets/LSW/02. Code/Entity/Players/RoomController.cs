using System.Collections;
using System.Collections.Generic;
using LSW._02._Code.Combat;
using LSW._02._Code.Entity.Rooms;
using UnityEngine;

namespace LSW._02._Code.Entity.Players
{
    [RequireComponent(typeof(PlayerController))]
    public class RoomController : MonoBehaviour
    {
        [SerializeField] private float restartDelay = 0.35f;
        [SerializeField] private BaseEntity[] roomEntities;
        [SerializeField] private WeaponPickup[] roomPickups;

        private readonly Dictionary<BaseEntity, Vector3> _startPositions = new();
        private readonly HashSet<WeaponPickup> _pickups = new();
        private PlayerController _player;
        private Coroutine _restartCoroutine;
        private bool _isCutsceneActive;

        public bool IsCombatActive { get; private set; }
        public bool CanOpenInventory =>
            !IsCombatActive && !_isCutsceneActive && _player != null && !_player.IsDead;

        private void Start()
        {
            _player = GetComponent<PlayerController>();
            RegisterEntity(_player);

            if (roomEntities != null)
            {
                foreach (BaseEntity entity in roomEntities)
                    RegisterEntity(entity);
            }

            if (roomPickups != null)
            {
                foreach (WeaponPickup pickup in roomPickups)
                    RegisterPickup(pickup);
            }

            _player.OnDeathEvent += HandlePlayerDeath;
            UpdateCombatState();
        }

        public void RegisterEntity(BaseEntity entity)
        {
            if (entity == null || _startPositions.ContainsKey(entity))
                return;

            _startPositions.Add(entity, entity.transform.position);
        }

        public void RegisterPickup(WeaponPickup pickup)
        {
            if (pickup != null)
                _pickups.Add(pickup);
        }

        private void Update()
        {
            UpdateCombatState();
        }

        private void UpdateCombatState()
        {
            IsCombatActive = false;
            foreach (BaseEntity entity in _startPositions.Keys)
            {
                if (entity != null && entity != _player && !entity.IsDead)
                {
                    IsCombatActive = true;
                    break;
                }
            }
        }

        public void SetCutsceneActive(bool active)
        {
            _isCutsceneActive = active;
        }

        private void HandlePlayerDeath()
        {
            if (Room.ActiveRoom != null)
                return;

            if (_restartCoroutine == null)
                _restartCoroutine = StartCoroutine(RestartRoom());
        }

        private IEnumerator RestartRoom()
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, restartDelay));

            foreach (KeyValuePair<BaseEntity, Vector3> entry in _startPositions)
            {
                if (entry.Key != null)
                    entry.Key.ResetForRoom(entry.Value);
            }

            foreach (WeaponPickup pickup in _pickups)
            {
                if (pickup == null)
                    continue;

                pickup.ResetForRoom();
            }

            UpdateCombatState();
            _restartCoroutine = null;
        }

        private void OnDestroy()
        {
            if (_player != null)
                _player.OnDeathEvent -= HandlePlayerDeath;

            if (_restartCoroutine != null)
                StopCoroutine(_restartCoroutine);
        }
    }
}
