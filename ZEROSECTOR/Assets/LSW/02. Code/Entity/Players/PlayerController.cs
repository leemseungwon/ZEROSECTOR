
using System.Collections.Generic;
using LSW._02._Code.Common;
using LSW._02._Code.Entity.Players.Components;
using LSW._02._Code.Entity.Players.States;
using LSW._02._Code.Sos;
using LSW._02._Code.Sos.EntityStatDatas;
using UnityEngine;

namespace LSW._02._Code.Entity.Players
{
    public class PlayerController : BaseEntity
    {
        [Header("Input")]
        [field:SerializeField] public InputData inputData;

        [Header("Camera")]
        [field:SerializeField] public Camera PlayerCamera { get; private set; }
        
        [Header("Movement")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.15f;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField, Range(0f, 80f)] private float maxWalkableSlopeAngle = 55f;

        [Header("Jump")]
        [SerializeField, Min(1)] private int maxJumpCount = 2;

        [Header("Custom Gravity")]
        [SerializeField, Min(0f)] private float gravityAcceleration = 9.81f;
        [SerializeField, Min(0f)] private float maxFallSpeed = 20f;

        [Header("Dash")]
        [SerializeField] private float dashDistance = 2.5f;
        [SerializeField] private float dashDuration = 0.16f;
        [SerializeField] private float dashCooldown = 0.35f;
        [SerializeField] private float dashHitRadius = 0.35f;
        [SerializeField] private LayerMask dashCollisionLayers =
            (1 << 0) | (1 << 6) | (1 << 8);
        [SerializeField, Min(0f)] private float postDashInvincibilityDuration = 0.2f;

        [Header("Parry")]
        [SerializeField, Min(0f)] private float parryCooldown = 0.5f;
        [SerializeField, Min(0f)] private float postParryInvincibilityDuration = 0.5f;
        
        public float CurrentJumpForce { get; private set; }
        public bool IsGrounded { get; private set; }
        public bool IsDashing => StateMachine?.CurrentState?.stateName == DashState;
        public bool IsParrying => StateMachine?.CurrentState?.stateName == ParryState;
        public bool WasParrySucceeded => _parrySucceeded;
        public float MoveInput => inputData != null ? inputData.MoveInput : 0f;
        public bool HasJumpRequest => _jumpRequested;
        public Vector2 AimDirection
        {
            get
            {
                Camera aimCamera = PlayerCamera != null ? PlayerCamera : Camera.main;
                if (aimCamera == null || inputData == null)
                    return new Vector2(_facingDirection, 0f);

                float depth = Mathf.Abs(transform.position.z - aimCamera.transform.position.z);
                Vector3 mouseWorld = aimCamera.ScreenToWorldPoint(
                    new Vector3(inputData.MousePosition.x, inputData.MousePosition.y, depth));
                Vector2 direction = (Vector2)mouseWorld - (Vector2)transform.position;
                return direction.sqrMagnitude > 0.0001f
                    ? direction.normalized
                    : new Vector2(_facingDirection, 0f);
            }
        }

        private Vector3 _initialScale;
        private bool _jumpRequested;
        private int _jumpCount;
        private float _dashCooldownRemaining;
        private float _katanaAttackCooldownRemaining;
        private float _parryCooldownRemaining;
        private bool _parrySucceeded;
        private BaseEntity _counterDashTarget;
        private float _facingDirection = 1f;
        private PlayerCombatComponent _combat;
        private PlayerAnimation _playerAnimation;
        private Collider2D _bodyCollider;
        private Vector2 _groundNormal = Vector2.up;
        private readonly RaycastHit2D[] _groundHitBuffer = new RaycastHit2D[8];
        private readonly RaycastHit2D[] _dashHitBuffer = new RaycastHit2D[16];

        public const string IdleState = "PlayerIdleState";
        public const string MoveState = "PlayerMoveState";
        public const string JumpState = "PlayerJumpState";
        public const string FallState = "PlayerFallState";
        public const string AttackState = "PlayerAttackState";
        public const string DashState = "PlayerDashState";
        public const string ParryState = "PlayerParryState";
        public const string UseGunState = "PlayerUseGunState";

        protected override void Awake()
        {
            base.Awake();
            Rigidbody.gravityScale = 0f;
            _bodyCollider = GetComponent<Collider2D>();

            if (inputData == null)
            {
                return;
            }

            GetCompo(out _combat);
            GetCompo(out _playerAnimation);
            if (_playerAnimation != null)
            {
                _playerAnimation.OnGunCast += ExecuteGunCast;
            }
            inputData.InitializeInput();
            inputData.OnJumpPressed += HandleJumpPressed;
            inputData.OnAttackPressed += RequestAttack;
            inputData.OnDashPressed += StartDash;
            inputData.OnReflectPressed += StartParry;
            inputData.OnReflectReleased += HandleParryReleased;
            inputData.OnGunPressed += ShootGun;
            
            InitStates();
            
            if (Animation != null)
                _initialScale = Animation.transform.localScale;
        }
        
        public void GunCast()
        {
            StateData? currentStateData = StateMachine.CurrentState;
            if(currentStateData.HasValue && currentStateData.Value.State is PlayerUseGunState useGunState)
                useGunState.ExecuteGunCast();
        }

        public void ExecuteGunCast()
        {
            GunCast();
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            CheckGround(true);

            Vector2 velocity = Rigidbody.linearVelocity;
            if (IsDashing)
            {
                velocity.y = 0f;
                Rigidbody.linearVelocity = velocity;
                return;
            }

            bool isJumping = StateMachine != null &&
                             StateMachine.CurrentState.HasValue &&
                             StateMachine.CurrentState.Value.stateName == JumpState;

            if (IsGrounded && !isJumping)
            {
                float input = IsParrying ? 0f : MoveInput;
                if (Mathf.Abs(input) > 0.01f)
                {
                    Vector2 slopeTangent = new Vector2(_groundNormal.y, -_groundNormal.x).normalized;
                    if (slopeTangent.x * input < 0f)
                        slopeTangent = -slopeTangent;

                    velocity = slopeTangent * (CurrentMoveSpeed * Mathf.Abs(input));
                }
                else
                {
                    velocity.y = 0f;
                }
            }
            else
            {
                velocity.y = Mathf.Max(
                    velocity.y - gravityAcceleration * Time.fixedDeltaTime,
                    -maxFallSpeed);
            }

            Rigidbody.linearVelocity = velocity;
        }
        
        private void InitStates()
        {
            StateMachine = new EntityStateMachine
            (
                new List<StateData>{
                    new StateData
                    {
                        stateName = IdleState,
                        State = new PlayerIdleState(this)
                    },
                    new StateData
                    {
                        stateName = MoveState,
                        State = new PlayerMoveState(this)
                    },
                    new StateData
                    {
                        stateName = JumpState,
                        State = new PlayerJumpState(this)
                    },
                    new StateData
                    {
                        stateName = FallState,
                        State = new PlayerFallState(this)
                    },
                    new StateData
                    {
                        stateName = AttackState,
                        State = new PlayerAttackState(this)
                    },
                    new StateData
                    {
                        stateName = DashState,
                        State = new PlayerDashState(this)
                    },
                    new StateData
                    {
                        stateName = ParryState,
                        State = new PlayerParryState(this)
                    },
                    new StateData
                    {
                        stateName = UseGunState,
                        State = new PlayerUseGunState(this)
                    }
                }
            );
        }

        public override void InitStat()
        {
            base.InitStat();
            if(StatData is PlayerStatData playerStatData)
            {
                CurrentJumpForce = playerStatData.initJumpForce;
                if (playerStatData.postDashInvincibilityDuration > 0f)
                    postDashInvincibilityDuration = playerStatData.postDashInvincibilityDuration;
                if (playerStatData.postParryInvincibilityDuration > 0f)
                    postParryInvincibilityDuration = playerStatData.postParryInvincibilityDuration;
            }
        }

        public override void ChangeEntityStat(EntityStatType type, MathOperation operation, float value)
        {
            switch (type)
            {
                case EntityStatType.JumpForce:
                    CurrentJumpForce = operation switch
                    {
                        MathOperation.Add => CurrentJumpForce + value,
                        MathOperation.Subtract => CurrentJumpForce - value,
                        _ => CurrentJumpForce
                    };
                    break;
                default:
                    base.ChangeEntityStat(type, operation, value);
                    break;
            }
        }

        private void Start()
        {
            StateMachine.Initialize(IdleState);
            CheckGround();
        }

        private void HandleJumpPressed()
        {
            if (!IsDead && !IsParrying &&
                (IsGrounded || _jumpCount < Mathf.Max(1, maxJumpCount)))
                _jumpRequested = true;
        }

        protected override void Update()
        {
            base.Update();
            UpdateActionTimers();
            UpdateFacingDirection();
            CheckGround();
        }

        private void UpdateActionTimers()
        {
            _dashCooldownRemaining = Mathf.Max(0f, _dashCooldownRemaining - Time.deltaTime);
            _katanaAttackCooldownRemaining =
                Mathf.Max(0f, _katanaAttackCooldownRemaining - Time.deltaTime);
            _parryCooldownRemaining = Mathf.Max(0f, _parryCooldownRemaining - Time.deltaTime);
        }

        private void StartDash()
        {
            if (IsDead || IsDashing || IsParrying || _dashCooldownRemaining > 0f)
                return;

            _dashCooldownRemaining = Mathf.Max(0f, dashCooldown);
            StateMachine.ChangeState(DashState);
        }

        public void PerformDash()
        {
            if (_counterDashTarget != null)
            {
                BaseEntity target = _counterDashTarget;
                _counterDashTarget = null;
                PerformCounterDash(target);
                return;
            }

            Vector2 direction = AimDirection;

            if (IsGrounded)
            {
                direction.y = 0f;
            }

            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector2.right * _facingDirection;
            }
            else
            {
                direction.Normalize();
            }

            float distance = Mathf.Max(0f, dashDistance);
            Collider2D bodyCollider = GetComponent<Collider2D>();
            Vector2 castOrigin = bodyCollider != null
                ? bodyCollider.bounds.center
                : Rigidbody.position;
            RaycastHit2D hit = GetDashBlockingHit(
                bodyCollider, castOrigin, direction, distance);

            bool hitEnemy = false;
            if (hit.collider != null)
            {
                BaseEntity target = hit.collider.GetComponentInParent<BaseEntity>();
                if (target != null && target != this && !target.IsDead)
                {
                    hitEnemy = _combat != null && _combat.DashAttack(target);
                }
            }

            MoveDashSafely(bodyCollider, direction * distance, out bool blockedVertical);
            Vector2 velocity = Rigidbody.linearVelocity;
            velocity.x = 0f;
            if (blockedVertical)
                velocity.y = 0f;
            Rigidbody.linearVelocity = velocity;

            if (Mathf.Abs(direction.x) > 0.01f)
            {
                _facingDirection = Mathf.Sign(direction.x);
                if (Animation != null)
                {
                    Animation.transform.localScale = new Vector3(
                        Mathf.Abs(_initialScale.x) * _facingDirection,
                        _initialScale.y,
                        _initialScale.z);
                }
            }

            if (Animation is PlayerAnimation playerAnimation)
                playerAnimation.PlayDash(hitEnemy);
            
            if (Mathf.Abs(direction.x) > 0.01f)
            {
                _facingDirection = Mathf.Sign(direction.x);

                if (Animation != null)
                {
                    Animation.transform.localScale = new Vector3(
                        Mathf.Abs(_initialScale.x) * _facingDirection,
                        _initialScale.y,
                        _initialScale.z);
                }
            }
        }

        public float DashDuration => Mathf.Max(0.01f, dashDuration);

        private void PerformCounterDash(BaseEntity target)
        {
            bool hitEnemy = false;
            if (target != null && !target.IsDead)
            {
                float direction = Mathf.Sign(target.transform.position.x - transform.position.x);
                if (Mathf.Approximately(direction, 0f))
                    direction = _facingDirection;

                Collider2D playerCollider = GetComponent<Collider2D>();
                Collider2D targetCollider = target.GetComponent<Collider2D>();

                float playerHalfWidth = playerCollider != null ? playerCollider.bounds.extents.x : dashHitRadius;
                float targetHalfWidth = targetCollider != null ? targetCollider.bounds.extents.x : 0f;
                float standOff = playerHalfWidth + targetHalfWidth + 0.02f;

                Vector2 destination = new Vector2(
                    target.transform.position.x - direction * standOff,
                    target.transform.position.y);

                Rigidbody.position = destination;
                Physics2D.SyncTransforms();
                Rigidbody.linearVelocity = Vector2.zero;
                _facingDirection = direction;

                if (Animation != null)
                {
                    Animation.transform.localScale = new Vector3(
                        Mathf.Abs(_initialScale.x) * direction,
                        _initialScale.y,
                        _initialScale.z);
                }

                hitEnemy = _combat != null && _combat.DashAttack(target);
            }

            if (Animation is PlayerAnimation playerAnimation)
                playerAnimation.PlayDash(hitEnemy);
        }

        private RaycastHit2D GetDashBlockingHit(Collider2D bodyCollider, Vector2 castOrigin,
            Vector2 direction, float distance)
        {
            var filter = new ContactFilter2D();
            filter.SetLayerMask(dashCollisionLayers);
            filter.useTriggers = false;

            int hitCount;
            if (bodyCollider != null)
            {
                hitCount = bodyCollider.Cast(
                    direction, filter, _dashHitBuffer, distance);
            }
            else
            {
                hitCount = Physics2D.CircleCast(
                    castOrigin,
                    dashHitRadius,
                    direction,
                    filter,
                    _dashHitBuffer,
                    distance);
            }

            RaycastHit2D closestHit = default;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit2D hit = _dashHitBuffer[i];
                if (hit.collider == null)
                    continue;

                BaseEntity target = hit.collider.GetComponentInParent<BaseEntity>();
                if (target == this || (target != null && target.IsDead))
                    continue;

                bool isGroundContactAlongOrAwayFromSurface =
                    hit.normal.y > 0.5f &&
                    Vector2.Dot(direction, hit.normal) >= -0.01f;
                if (isGroundContactAlongOrAwayFromSurface)
                    continue;

                if (closestHit.collider == null || hit.distance < closestHit.distance)
                    closestHit = hit;
            }

            return closestHit;
        }

        private void MoveDashSafely(Collider2D bodyCollider, Vector2 displacement, out bool blockedVertical)
        {
            blockedVertical = false;

            Vector2 position = Rigidbody.position;
            float horizontalDistance = Mathf.Abs(displacement.x);
            if (horizontalDistance > 0.0001f)
            {
                Vector2 direction = Vector2.right * Mathf.Sign(displacement.x);
                Vector2 castOrigin = bodyCollider != null ? bodyCollider.bounds.center : position;
                RaycastHit2D hit = GetDashBlockingHit(bodyCollider, castOrigin, direction, horizontalDistance);
                float travel = hit.collider != null
                    ? Mathf.Max(0f, hit.distance - 0.02f)
                    : horizontalDistance;
                Rigidbody.position = position + direction * travel;
                Physics2D.SyncTransforms();
            }

            float verticalDistance = Mathf.Abs(displacement.y);
            if (verticalDistance > 0.0001f)
            {
                Vector2 direction = Vector2.up * Mathf.Sign(displacement.y);
                Vector2 castOrigin = bodyCollider != null ? bodyCollider.bounds.center : Rigidbody.position;
                RaycastHit2D hit = GetDashBlockingHit(bodyCollider, castOrigin, direction, verticalDistance);
                float travel = hit.collider != null
                    ? Mathf.Max(0f, hit.distance - 0.02f)
                    : verticalDistance;
                blockedVertical = hit.collider != null;
                Rigidbody.position += direction * travel;
            }
        }

        private void StartParry()
        {
            if (IsDead || IsDashing || IsParrying || _parryCooldownRemaining > 0f)
                return;

            _parryCooldownRemaining = parryCooldown;
            StateMachine.ChangeState(ParryState);
        }

        private void HandleParryReleased()
        {
            if (!IsParrying)
                return;

            if (!IsGrounded)
            {
                StateMachine.ChangeState(Rigidbody.linearVelocity.y > 0f
                    ? JumpState
                    : FallState);
                return;
            }

            StateMachine.ChangeState(MoveInput != 0f ? MoveState : IdleState);
        }

        public void BeginParryState()
        {
            _parrySucceeded = false;
            _jumpRequested = false;
            Vector2 velocity = Rigidbody.linearVelocity;
            velocity.x = 0f;
            Rigidbody.linearVelocity = velocity;
            if (Animation is PlayerAnimation playerAnimation)
                playerAnimation.PlayParry(true);
        }

        public void EndParryState()
        {
            if (Animation is PlayerAnimation playerAnimation)
            {
                playerAnimation.PlayParry(false);
                if (!_parrySucceeded)
                    playerAnimation.PlayParryFail();
            }
        }

        private void CheckGround()
        {
            CheckGround(false);
        }

        private void CheckGround(bool snapToSurface)
        {
            bool hasGroundBelow = TryGetGroundHit(out RaycastHit2D groundHit);
            _groundNormal = hasGroundBelow ? groundHit.normal : Vector2.up;
            IsGrounded = hasGroundBelow && groundHit.distance <= 0.1f;

            bool isJumping = StateMachine != null && StateMachine.CurrentState.HasValue &&
                             StateMachine.CurrentState.Value.stateName == JumpState;
            bool shouldFollowGround = snapToSurface && hasGroundBelow &&
                                      groundHit.distance <= groundCheckRadius &&
                                      !IsDashing && !isJumping && Mathf.Abs(MoveInput) > 0.01f;
            if (shouldFollowGround)
            {
                if (groundHit.distance > 0.005f)
                    Rigidbody.position += Vector2.down * groundHit.distance;

                Vector2 velocity = Rigidbody.linearVelocity;
                velocity.y = 0f;
                Rigidbody.linearVelocity = velocity;
                IsGrounded = true;
            }

            if (Animation is PlayerAnimation playerAnimation)
                playerAnimation.SetGrounded(IsGrounded);

            if (IsGrounded && Rigidbody.linearVelocity.y <= 0f)
                _jumpCount = 0;
        }

        private bool TryGetGroundHit(out RaycastHit2D groundHit)
        {
            groundHit = default;
            if (_bodyCollider == null)
                _bodyCollider = GetComponent<Collider2D>();

            if (_bodyCollider == null)
                return false;

            float castDistance = Mathf.Max(groundCheckRadius, 0.05f) + 0.1f;
            float minNormalY = Mathf.Cos(maxWalkableSlopeAngle * Mathf.Deg2Rad);

            var filter = new ContactFilter2D();
            filter.SetLayerMask(groundLayer);
            filter.useTriggers = false;
            int hitCount = _bodyCollider.Cast(Vector2.down, filter, _groundHitBuffer, castDistance);
            for (int i = 0; i < hitCount; i++)
                ConsiderGroundHit(_groundHitBuffer[i], minNormalY, ref groundHit);

            return groundHit.collider != null;
        }

        private static void ConsiderGroundHit(RaycastHit2D candidate, float minNormalY,
            ref RaycastHit2D closestHit)
        {
            if (candidate.collider == null || candidate.collider.isTrigger || candidate.normal.y < minNormalY)
                return;

            if (closestHit.collider == null || candidate.distance < closestHit.distance)
                closestHit = candidate;
        }

        public void Move()
        {
            if (IsDead || IsDashing || IsParrying)
                return;

            Vector2 velocity = Rigidbody.linearVelocity;
            velocity.x = MoveInput * CurrentMoveSpeed;
            Rigidbody.linearVelocity = velocity;
        }
        
        private void UpdateFacingDirection()
        {
            bool isAttackAnimationPlaying =
                StateMachine?.CurrentState?.stateName == AttackState ||
                _playerAnimation != null && _playerAnimation.IsDashAttackAnimationPlaying;

            if (Animation == null || IsDashing || isAttackAnimationPlaying)
                return;

            float directionX;
            
            if (Mathf.Abs(MoveInput) > 0.01f)
            {
                directionX = Mathf.Sign(MoveInput);
            }
            else
            {
                directionX = Mathf.Sign(AimDirection.x);
            }

            if (Mathf.Abs(directionX) < 0.001f)
                return;

            _facingDirection = directionX;

            Vector3 scale = Animation.transform.localScale;
            scale.x = Mathf.Abs(_initialScale.x) * _facingDirection;
            Animation.transform.localScale = scale;
        }

        public override bool ReceiveHit(BaseEntity attacker)
        {
            if (IsDead)
                return false;

            if (GetCompo(out EntityBuffComponent buff) && buff.IsInvincible)
                return false;

            if (IsDashing)
                return true;

            if (IsParrying)
            {
                StartCounterDash(attacker);
                return true;
            }
            
            Rigidbody.linearVelocity = Vector2.zero;
            
            return base.ReceiveHit(attacker);
        }

        private void StartCounterDash(BaseEntity target)
        {
            if (target == null || target.IsDead ||
                StateMachine == null || StateMachine.CurrentState == null)
                return;

            _parrySucceeded = true;
            _counterDashTarget = target;
            _dashCooldownRemaining = Mathf.Max(0f, dashCooldown);
            ApplyPostParryInvincibility();
            StateMachine.ChangeState(DashState);
        }

        public void ApplyPostDashInvincibility()
        {
            if (postDashInvincibilityDuration > 0f && GetCompo(out EntityBuffComponent buff))
            {
                buff.ApplyInvincibility(postDashInvincibilityDuration);
            }
        }

        public void ApplyPostParryInvincibility()
        {
            if (postParryInvincibilityDuration > 0f && GetCompo(out EntityBuffComponent buff))
            {
                buff.ApplyInvincibility(postParryInvincibilityDuration);
            }
        }

        public void ConsumeParrySuccess()
        {
            _parrySucceeded = false;
        }

        public override void ResetForRoom(Vector3 position)
        {
            base.ResetForRoom(position);
            _jumpRequested = false;
            _jumpCount = 0;
            _dashCooldownRemaining = 0f;
            _katanaAttackCooldownRemaining = 0f;
            if (IsParrying)
                _parrySucceeded = true;
            _parryCooldownRemaining = 0f;
            _counterDashTarget = null;
            StateMachine?.RestartState(IdleState);
            _parrySucceeded = false;
            CheckGround();
        }

        public bool Jump()
        {
            if (!_jumpRequested || IsDead || (!IsGrounded && _jumpCount >= Mathf.Max(1, maxJumpCount)))
                return false;

            _jumpRequested = false;
            _jumpCount++;

            Vector2 velocity = Rigidbody.linearVelocity;
            velocity.y = CurrentJumpForce;
            Rigidbody.linearVelocity = velocity;

            return true;
        }

        public void ConsumeJumpInput()
        {
            _jumpRequested = false;
        }
        
        public void RequestAttack()
        {
            if (IsDead || !IsGrounded || IsParrying || StateMachine == null || StateMachine.CurrentState == null ||
                StateMachine.CurrentState.Value.stateName == AttackState)
                return;
            
            if (_combat != null && _katanaAttackCooldownRemaining > 0f)
                return;

            StateMachine.ChangeState(AttackState);

            if (_combat != null)
                _katanaAttackCooldownRemaining = _combat.KatanaAttackCooldown;
        }

        public void ShootGun()
        {
            if (IsDead || IsDashing || IsParrying || _combat == null)
                return;

            if (!_combat.IsGunReady || StateMachine == null)
                return;

            StateMachine.ChangeState(UseGunState);
        }
        
        public bool CanUseGun()
        {
            return !IsDashing && !IsParrying &&
                   _combat != null && _combat.IsGunReady;
        }

        public bool ConsumeGunGauge()
        {
            return _combat != null && _combat.ConsumeGunGauge();
        }

        public void SetGunReadyAnimation()
        {
            if (_playerAnimation != null &&
                _playerAnimation.ReadyGunAnimationData != null)
            {
                _playerAnimation.SetParam(_playerAnimation.ReadyGunAnimationData);
            }
        }

        public void ResetGunReadyAnimation()
        {
            if (_playerAnimation != null)
                _playerAnimation.SetParam(_playerAnimation.ShootGunAnimationData);
        }

        public void ExecuteGunRaycast()
        {
            if (!IsDead && _combat != null)
                _combat.ExecuteGunRaycast();
        }
        
        public override void OnDestroy()
        {
            if (_playerAnimation != null)
            {
                _playerAnimation.OnGunCast -= ExecuteGunCast;
            }

            if (inputData != null)
            {
                inputData.OnJumpPressed -= HandleJumpPressed;
                inputData.OnAttackPressed -= RequestAttack;
                inputData.OnDashPressed -= StartDash;
                inputData.OnReflectPressed -= StartParry;
                inputData.OnReflectReleased -= HandleParryReleased;
                inputData.OnGunPressed -= ShootGun;
            }

            base.OnDestroy();
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck == null)
                return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
