using System.Collections.Generic;
using LSW._02._Code.Common;
using LSW._02._Code.Entity.Enemy.States;
using LSW._02._Code.Entity.Players;
using LSW._02._Code.Sos.EntityStatDatas;
using LSW._02._Code.System;
using LSW._02._Code.System.HandlePlayerSystems;
using LSW._02._Code.System.PoolSystems;
using UnityEngine;

namespace LSW._02._Code.Entity.Enemy
{
    public class BaseEnemy : BaseEntity, IPoolable
    {
        public const string IdleState = "EnemyIdleState";
        public const string AttackState = "EnemyAttackState";
        public const string ChaseState = "EnemyChaseState";
        public const string PatrolState = "EnemyPatrolState";
        public const string DeadState = "EnemyDeadState";

        [Header("Target")]
        [SerializeField, Min(0f)] private float detectionRange = 6f;
        [SerializeField, Min(0f)] private float attackRange = 1f;
        [SerializeField, Min(0f)] private float attackHitRadius = 1.2f;
        [SerializeField] private Vector2 attackHitOffset = new(0.7f, 0.25f);
        [SerializeField] private LayerMask attackHitLayers = 1 << 7;
        [SerializeField] private LayerMask wallLayers = (1 << 0) | (1 << 6);

        [Header("Patrol")]
        [SerializeField] private bool patrolWhenIdle = true;
        [SerializeField, Min(0f)] private float patrolDistance = 2f;
        [SerializeField, Min(0f)] private float idleDuration = 0.5f;

        [Header("Combat")]
        [SerializeField, Min(0f)] private float attackCooldown = 1f;
        [SerializeField] private string projectilePrefabName;
        [SerializeField, Min(0f)] private float projectileSpeed = 8f;
        [SerializeField, Min(0.01f)] private float projectileLifetime = 4f;
        [SerializeField] private Transform firePoint;

        private PoolSystem _poolSystem;
        private BaseEnemyAnimation _baseEnemyAnimation;
        private PlayerController _targetPlayer;
        private Collider2D _collider;
        private Vector3 _initialPosition;
        private Vector3 _initialVisualScale;
        private float _patrolDirection = 1f;
        private bool _statesInitialized;
        private readonly RaycastHit2D[] _wallHitBuffer = new RaycastHit2D[16];

        public PlayerController TargetPlayer => _targetPlayer;
        public float DetectionRange => detectionRange;
        public float AttackRange => attackRange;
        public LayerMask WallLayers => wallLayers;
        public float PatrolDistance => patrolDistance;
        public float IdleDuration => idleDuration;
        public bool PatrolWhenIdle => patrolWhenIdle;
        public float AttackCooldown => attackCooldown;

        protected override void Awake()
        {
            base.Awake();
            _collider = GetComponent<Collider2D>();
            _initialPosition = transform.position;
            _initialVisualScale = Animation != null
                ? Animation.transform.localScale
                : Vector3.one;
            
            if(SystemHandler.Instance.GetSystem(out HandlePlayerSystem playerSystem))
            {
                _targetPlayer = playerSystem.Player;
            }
            
            GetCompo(out _baseEnemyAnimation);
            CachePoolSystem();
            InitializeStates();
            ResolvePlayerTarget();
        }

        protected virtual void Start()
        {
            if (StateMachine.CurrentState == null)
                StateMachine.Initialize(IsDead ? DeadState : IdleState);

        }

        private void InitializeStates()
        {
            if (_statesInitialized)
                return;

            StateMachine = new EntityStateMachine(new List<StateData>
            {
                new() { stateName = IdleState, State = new EnemyIdleState(this) },
                new() { stateName = AttackState, State = new EnemyAttackState(this) },
                new() { stateName = ChaseState, State = new EnemyChaseState(this) },
                new() { stateName = PatrolState, State = new EnemyPatrolState(this) },
                new() { stateName = DeadState, State = new EnemyDeadState(this) }
            });
            _statesInitialized = true;
        }

        protected override void Update()
        {
            ResolvePlayerTarget();
            base.Update();
        }

        public override bool ReceiveHit(BaseEntity attacker)
        {
            if (IsDead)
                return false;

            return base.ReceiveHit(attacker);
        }

        protected override void OnDeath(BaseEntity attacker)
        {
            base.OnDeath(attacker);
            if (StateMachine == null)
                return;

            if (StateMachine.CurrentState == null)
                StateMachine.Initialize(DeadState);
            else
                StateMachine.ChangeState(DeadState);
        }

        public override void ResetForRoom(Vector3 position)
        {
            gameObject.SetActive(true);
            base.ResetForRoom(position);
            Rigidbody.bodyType = RigidbodyType2D.Dynamic;
            _initialPosition = position;
            _patrolDirection = 1f;

            if (Animation != null)
                Animation.transform.localScale = _initialVisualScale;

            InitializeStates();
            StateMachine.RestartState(IdleState);
        }

        public void Initialize()
        {
            CachePoolSystem();
            InitializeStates();
        }

        public void Spawn()
        {
            ResetForRoom(transform.position);
        }

        public void Despawn()
        {
            ResetRuntimeState();
        }

        public bool HasLivingTarget()
        {
            return _targetPlayer != null && !_targetPlayer.IsDead;
        }

        public bool IsTargetBlockedByWall()
        {
            if (!HasLivingTarget())
                return true;

            Vector2 start = _collider != null ? (Vector2)_collider.bounds.center : (Vector2)transform.position;
            Collider2D targetCollider = _targetPlayer.GetComponent<Collider2D>();
            Vector2 targetPos = targetCollider != null ? (Vector2)targetCollider.bounds.center : (Vector2)_targetPlayer.transform.position;

            Vector2 direction = targetPos - start;
            float distance = direction.magnitude;
            if (distance <= 0.0001f)
                return false;

            var filter = new ContactFilter2D();
            filter.SetLayerMask(wallLayers);
            filter.useTriggers = false;

            int hitCount = Physics2D.Raycast(start, direction.normalized, filter, _wallHitBuffer, distance);
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit2D hit = _wallHitBuffer[i];
                if (hit.collider == null || hit.collider.isTrigger)
                    continue;

                if (hit.collider.transform.IsChildOf(transform) || hit.collider.transform.IsChildOf(_targetPlayer.transform))
                    continue;

                return true;
            }

            return false;
        }

        public bool HasLineOfSightToTarget()
        {
            return HasLivingTarget() && !IsTargetBlockedByWall();
        }

        public float DistanceToTarget()
        {
            if (!HasLivingTarget())
                return float.PositiveInfinity;

            return Vector2.Distance(transform.position, _targetPlayer.transform.position);
        }

        public void MoveTowardsTarget()
        {
            if (!HasLivingTarget() || IsTargetBlockedByWall())
            {
                StopHorizontalMovement();
                return;
            }

            MoveTowardsX(_targetPlayer.transform.position.x);
        }

        public void MoveTowardsPatrolPoint()
        {
            MoveTowardsX(_initialPosition.x + _patrolDirection * patrolDistance);
        }

        public bool IsAtPatrolPoint()
        {
            float destination = _initialPosition.x + _patrolDirection * patrolDistance;
            return Mathf.Abs(destination - transform.position.x) <= 0.05f;
        }

        public void ReversePatrolDirection()
        {
            _patrolDirection *= -1f;
        }

        public void StopHorizontalMovement()
        {
            Vector2 velocity = Rigidbody.linearVelocity;
            velocity.x = 0f;
            Rigidbody.linearVelocity = velocity;
            SetMovingAnimation(false);
        }

        public void FaceTarget()
        {
            if (!HasLivingTarget() || IsTargetBlockedByWall())
                return;

            FaceDirection(_targetPlayer.transform.position.x - transform.position.x);
        }

        public void AttackTarget()
        {
            if (!HasLivingTarget() || IsTargetBlockedByWall())
                return;

            float facingDirection = Animation != null
                ? Mathf.Sign(Animation.transform.localScale.x)
                : Mathf.Sign(_initialVisualScale.x);
            Vector2 attackCenter = (Vector2)transform.position +
                                   new Vector2(attackHitOffset.x * facingDirection, attackHitOffset.y);
            Collider2D[] hits = Physics2D.OverlapCircleAll(
                attackCenter,
                attackHitRadius,
                attackHitLayers);

            foreach (Collider2D hit in hits)
            {
                if (hit.GetComponentInParent<PlayerController>() != _targetPlayer)
                    continue;

                _targetPlayer.ReceiveHit(this);
                return;
            }
        }

        public void FireProjectile()
        {
            if (string.IsNullOrEmpty(projectilePrefabName))
                return;

            float facingDirection = Animation != null
                ? Mathf.Sign(Animation.transform.localScale.x)
                : Mathf.Sign(_initialVisualScale.x);
            Vector2 spawnPosition = firePoint != null
                ? firePoint.position
                : (Vector2)transform.position + new Vector2(attackHitOffset.x * facingDirection, attackHitOffset.y);
            Vector2 direction = (Vector2)_targetPlayer.transform.position - spawnPosition;
            direction.y += 0.5f;
            
            if (direction.sqrMagnitude <= 0.0001f)
                return;

            if (_poolSystem != null && _poolSystem.SpawnPool(projectilePrefabName, out EnemyProjectile projectile))
            {
                projectile.Launch(
                    this, spawnPosition, direction.normalized, projectileSpeed, projectileLifetime);
            }
        }

        public void PlayAttackAnimation()
        {
            if (Animation != null && _baseEnemyAnimation != null && _baseEnemyAnimation.AttackAnimationData != null)
                Animation.SetParam(_baseEnemyAnimation.AttackAnimationData);
        }

        public void PlayDeadAnimation()
        {
            if (Animation != null && _baseEnemyAnimation != null && _baseEnemyAnimation.DeadAnimationData != null)
                Animation.SetParam(_baseEnemyAnimation.DeadAnimationData);
        }

        public void DespawnAfterDeath()
        {
            DespawnFromRoom();
        }

        public void DespawnFromRoom()
        {
            if (StatData is EnemyStatData statData &&
                !string.IsNullOrWhiteSpace(statData.poolName))
            {
                CachePoolSystem();
                if (_poolSystem != null && _poolSystem.HasPool(statData.poolName))
                {
                    _poolSystem.DespawnPool(statData.poolName, this);
                    return;
                }
            }

            gameObject.SetActive(false);
        }

        private void MoveTowardsX(float destinationX)
        {
            float deltaX = destinationX - transform.position.x;
            FaceDirection(deltaX);

            Vector2 velocity = Rigidbody.linearVelocity;
            velocity.x = Mathf.Abs(deltaX) <= 0.05f
                ? 0f
                : Mathf.Sign(deltaX) * CurrentMoveSpeed;
            Rigidbody.linearVelocity = velocity;
            SetMovingAnimation(Mathf.Abs(velocity.x) > 0.01f);
        }

        private void FaceDirection(float direction)
        {
            if (Animation == null || Mathf.Abs(direction) <= 0.01f)
                return;

            Vector3 scale = _initialVisualScale;
            scale.x = Mathf.Abs(scale.x) * Mathf.Sign(direction);
            Animation.transform.localScale = scale;
        }

        private void SetMovingAnimation(bool moving)
        {
            if (Animation != null && Animation.IsMoveAnimationData != null)
                Animation.SetParam(Animation.IsMoveAnimationData, moving);
        }

        private void ResolvePlayerTarget()
        {
            if (_targetPlayer != null)
                return;

            SystemHandler handler = SystemHandler.Instance;
            if (handler != null &&
                handler.GetSystem(out HandlePlayerSystem playerSystem))
            {
                _targetPlayer = playerSystem.Player;
            }
        }

        private void CachePoolSystem()
        {
            if (_poolSystem != null)
                return;

            SystemHandler handler = SystemHandler.Instance;
            if (handler != null)
                handler.GetSystem(out _poolSystem);
        }

        private void OnDrawGizmosSelected()
        {
            float facingDirection = Animation != null
                ? Mathf.Sign(Animation.transform.localScale.x)
                : 1f;
            Vector2 attackCenter = (Vector2)transform.position +
                                   new Vector2(attackHitOffset.x * facingDirection, attackHitOffset.y);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackCenter, attackHitRadius);
        }
    }
}
