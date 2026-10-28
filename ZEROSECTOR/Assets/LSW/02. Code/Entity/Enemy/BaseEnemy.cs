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
    public abstract class BaseEnemy : BaseEntity, IPoolable
    {
        public const string IdleState = "EnemyIdleState";
        public const string AttackState = "EnemyAttackState";
        public const string ChaseState = "EnemyChaseState";
        public const string PatrolState = "EnemyPatrolState";
        public const string DeadState = "EnemyDeadState";

        [Header("Target")]
        [SerializeField, Min(0f)] private float detectionRange = 6f;
        [SerializeField, Min(0f)] private float attackRange = 1f;
        [SerializeField] private LayerMask wallLayers = (1 << 0) | (1 << 6);

        [Header("Ledge Check")]
        [SerializeField] private LayerMask groundLayers = (1 << 0) | (1 << 6);
        [SerializeField, Min(0f)] private float ledgeCheckForward = 0.08f;
        [SerializeField, Min(0.01f)] private float ledgeProbeDistance = 0.35f;
        [SerializeField, Range(0f, 80f)] private float maxWalkableSlopeAngle = 55f;

        [Header("Combat")]
        [SerializeField, Min(0f)] private float attackCooldown = 1f;

        private bool _patrolWhenIdle = true;
        private float _patrolDistance = 2f;
        private float _idleDuration = 0.5f;

        private PoolSystem _poolSystem;
        private BaseEnemyAnimation _baseEnemyAnimation;
        private PlayerController _targetPlayer;
        private Collider2D _collider;
        private Vector3 _initialPosition;
        private Vector3 _initialVisualScale;
        private float _patrolDirection = 1f;
        private bool _statesInitialized;
        private readonly RaycastHit2D[] _wallHitBuffer = new RaycastHit2D[16];
        private readonly RaycastHit2D[] _groundHitBuffer = new RaycastHit2D[8];

        public PlayerController TargetPlayer => _targetPlayer;
        public float DetectionRange => detectionRange;
        public float AttackRange => attackRange;
        public LayerMask WallLayers => wallLayers;
        public float PatrolDistance => _patrolDistance;
        public float IdleDuration => _idleDuration;
        public bool PatrolWhenIdle => _patrolWhenIdle;
        public float AttackCooldown => attackCooldown;
        public bool IsAttacking => StateMachine?.CurrentState?.stateName == AttackState;

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
            MoveTowardsX(_initialPosition.x + _patrolDirection * _patrolDistance);
        }

        public bool IsAtPatrolPoint()
        {
            float destination = _initialPosition.x + _patrolDirection * _patrolDistance;
            return Mathf.Abs(destination - transform.position.x) <= 0.05f;
        }

        public void ConfigurePatrol(bool patrolWhenIdle, float patrolDistance, float idleDuration)
        {
            _patrolWhenIdle = patrolWhenIdle;
            _patrolDistance = Mathf.Max(0f, patrolDistance);
            _idleDuration = Mathf.Max(0f, idleDuration);
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
            if (IsAttacking || !HasLivingTarget() || IsTargetBlockedByWall())
                return;

            FaceDirection(_targetPlayer.transform.position.x - transform.position.x);
        }

        public abstract void AttackTarget();

        public void PlayAttackAnimation()
        {
            if (Animation != null && _baseEnemyAnimation != null && _baseEnemyAnimation.AttackAnimationData != null)
                Animation.SetParam(_baseEnemyAnimation.AttackAnimationData);
        }
        
        public virtual void PlayDeadAnimation()
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
            if (Mathf.Abs(deltaX) > 0.05f &&
                !HasGroundAhead(Mathf.Sign(deltaX), CurrentMoveSpeed * Time.fixedDeltaTime))
            {
                StopHorizontalMovement();
                return;
            }

            Vector2 velocity = Rigidbody.linearVelocity;
            float speed = Mathf.Abs(deltaX) <= 0.05f
                ? 0f
                : Mathf.Sign(deltaX) * CurrentMoveSpeed;
            if (Mathf.Abs(speed) > 0.01f && TryGetGroundSurface(out RaycastHit2D groundHit) &&
                groundHit.distance <= 0.05f && velocity.y <= 0.1f)
            {
                Vector2 tangent = new Vector2(groundHit.normal.y, -groundHit.normal.x).normalized;
                if (tangent.x * speed < 0f)
                    tangent = -tangent;
                velocity = tangent * Mathf.Abs(speed);
            }
            else
            {
                velocity.x = speed;
            }

            FaceDirection(deltaX);
            Rigidbody.linearVelocity = velocity;
            SetMovingAnimation(Mathf.Abs(speed) > 0.01f);
        }

        private bool TryGetGroundSurface(out RaycastHit2D groundHit)
        {
            groundHit = default;
            if (_collider == null)
                _collider = GetComponent<Collider2D>();
            if (_collider == null)
                return false;

            var filter = new ContactFilter2D();
            filter.SetLayerMask(groundLayers);
            filter.useTriggers = false;
            int hitCount = _collider.Cast(Vector2.down, filter, _groundHitBuffer, 0.08f);
            float minNormalY = Mathf.Cos(maxWalkableSlopeAngle * Mathf.Deg2Rad);
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit2D hit = _groundHitBuffer[i];
                if (hit.collider == null || hit.collider.isTrigger || hit.normal.y < minNormalY ||
                    hit.collider.transform.IsChildOf(transform))
                    continue;

                if (groundHit.collider == null || hit.distance < groundHit.distance)
                    groundHit = hit;
            }

            return groundHit.collider != null;
        }

        private bool HasGroundAhead(float direction, float additionalDistance)
        {
            if (_collider == null)
                _collider = GetComponent<Collider2D>();

            Bounds bounds = _collider != null
                ? _collider.bounds
                : new Bounds(transform.position, new Vector3(0.5f, 1f, 0f));
            float probeOffset = bounds.extents.x + ledgeCheckForward + additionalDistance;
            float allowedRise = probeOffset * Mathf.Tan(maxWalkableSlopeAngle * Mathf.Deg2Rad);
            Vector2 probeOrigin = new(
                bounds.center.x + direction * probeOffset,
                bounds.min.y + allowedRise + 0.02f);

            RaycastHit2D hit = Physics2D.Raycast(probeOrigin, Vector2.down,
                allowedRise + ledgeProbeDistance, groundLayers);
            return hit.collider != null && !hit.collider.isTrigger &&
                   hit.normal.y >= Mathf.Cos(maxWalkableSlopeAngle * Mathf.Deg2Rad) &&
                   !hit.collider.transform.IsChildOf(transform);
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

        protected PoolSystem PoolSystem => _poolSystem;
        protected float InitialFacingDirection => Mathf.Sign(_initialVisualScale.x);

        protected void CachePoolSystem()
        {
            if (_poolSystem != null)
                return;

            SystemHandler handler = SystemHandler.Instance;
            if (handler != null)
                handler.GetSystem(out _poolSystem);
        }
    }
}
