using LSW._02._Code.Entity.Players;
using LSW._02._Code.System.PoolSystems;
using UnityEngine;
using LSW._02._Code.System;

namespace LSW._02._Code.Entity.Enemy
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class EnemyProjectile : MonoBehaviour, IPoolable
    {
        [SerializeField] private string poolName = "EnemyProjectile";
        [SerializeField] private LayerMask collisionLayers = (1 << 0) | (1 << 6) | (1 << 7);

        private BaseEnemy _owner;
        private Collider2D[] _ignoredOwnerColliders;
        private Collider2D[] _projectileColliders;
        private Rigidbody2D _rigidbody;
        private PoolSystem _poolSystem;
        private float _remainingLifetime;
        private bool _consumed;

        public void Initialize()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _projectileColliders = GetComponents<Collider2D>();
            if (SystemHandler.Instance != null)
                SystemHandler.Instance.GetSystem(out _poolSystem);
        }

        public void Spawn()
        {
            _consumed = false;
            _remainingLifetime = 0f;
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.angularVelocity = 0f;
        }

        public void Launch(BaseEnemy owner, Vector2 position, Vector2 direction, 
            float speed, float lifetime)
        {
            _owner = owner;
            _consumed = false;
            _remainingLifetime = Mathf.Max(0f, lifetime);
            transform.position = position;
            _rigidbody.position = position;
            direction = direction.normalized;
            _rigidbody.linearVelocity = direction * speed;
            transform.right = direction;

            if (_owner != null)
            {
                _ignoredOwnerColliders = _owner.GetComponentsInChildren<Collider2D>();
                foreach (Collider2D ownerCollider in _ignoredOwnerColliders)
                {
                    foreach (Collider2D projectileCollider in _projectileColliders)
                        Physics2D.IgnoreCollision(projectileCollider, ownerCollider);
                }
            }
        }

        private void Update()
        {
            if (_consumed)
                return;

            _remainingLifetime -= Time.deltaTime;
            if (_remainingLifetime <= 0f)
                ReturnToPool();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_consumed || other == null)
                return;

            if (_owner != null && other.transform.IsChildOf(_owner.transform))
                return;

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                player.ReceiveHit(_owner);
                ReturnToPool();
                return;
            }

            if ((collisionLayers.value & (1 << other.gameObject.layer)) != 0)
                ReturnToPool();
        }

        public void DestroyByAttack()
        {
            ReturnToPool();
        }

        public void Despawn()
        {
            ResetOwnerCollisionIgnores();
            _consumed = true;
            _remainingLifetime = 0f;
            _owner = null;
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.angularVelocity = 0f;
            _rigidbody.position = Vector2.zero;
            transform.rotation = Quaternion.identity;
        }

        private void ReturnToPool()
        {
            if (_consumed)
                return;

            _consumed = true;
            if (_poolSystem == null || !_poolSystem.HasPool(poolName))
            {
                Debug.LogError($"Projectile pool '{poolName}' is not registered.", this);
                return;
            }

            _poolSystem.DespawnPool(poolName, this);
        }

        private void ResetOwnerCollisionIgnores()
        {
            if (_ignoredOwnerColliders == null)
                return;

            foreach (Collider2D ownerCollider in _ignoredOwnerColliders)
            {
                if (ownerCollider == null)
                    continue;

                foreach (Collider2D projectileCollider in _projectileColliders)
                {
                    if (projectileCollider != null)
                        Physics2D.IgnoreCollision(projectileCollider, ownerCollider, false);
                }
            }

            _ignoredOwnerColliders = null;
        }
    }
}
