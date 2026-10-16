
using System.Collections.Generic;
using LSW._02._Code.Common;
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

        [Header("Parry")]
        [SerializeField, Min(0f)] private float parryCooldown = 0.5f;

        public float CurrentJumpForce { get; private set; }
        public bool IsGrounded { get; private set; }
        public bool IsDashing => StateMachine?.CurrentState?.stateName == DashState;
        public bool IsParrying => _isParrying;
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
        private bool _isParrying;
        private bool _parrySucceeded;
        private BaseEntity _counterDashTarget;
        private float _facingDirection = 1f;
        private EntityCombatComponent _combat;
        private readonly RaycastHit2D[] _dashHitBuffer = new RaycastHit2D[16];
        
        public const string IdleState = "PlayerIdleState";
        public const string MoveState = "PlayerMoveState";
        public const string JumpState = "PlayerJumpState";
        public const string FallState = "PlayerFallState";
        public const string AttackState = "PlayerAttackState";
        public const string DashState = "PlayerDashState";

        protected override void Awake()
        {
            base.Awake();
            Rigidbody.gravityScale = 0f;

            if (inputData == null)
            {
                return;
            }

            GetCompo(out _combat);
            inputData.InitializeInput();
            inputData.OnJumpPressed += HandleJumpPressed;
            inputData.OnAttackPressed += RequestAttack;
            inputData.OnDashPressed += StartDash;
            inputData.OnReflectPressed += StartParry;
            inputData.OnReflectReleased += EndParry;
            
            InitStates();
            
            if (Animation != null)
                _initialScale = Animation.transform.localScale;
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            Vector2 velocity = Rigidbody.linearVelocity;
            if (IsDashing)
            {
                velocity.y = 0f;
                Rigidbody.linearVelocity = velocity;
                return;
            }

            if (IsGrounded && velocity.y <= 0f)
            {
                velocity.y = 0f;
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
            if (!IsDead && (IsGrounded || _jumpCount < Mathf.Max(1, maxJumpCount)))
                _jumpRequested = true;
        }

        protected override void Update()
        {
            base.Update();
            UpdateActionTimers();
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
            if (IsDead || IsDashing || _dashCooldownRemaining > 0f)
                return;

            _dashCooldownRemaining = Mathf.Max(0f, dashCooldown);
            EndParry();
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

            Vector2 direction;
            if (IsGrounded)
            {
                float dashDirection = Mathf.Abs(MoveInput) > 0.01f
                    ? Mathf.Sign(MoveInput)
                    : _facingDirection;
                direction = Vector2.right * dashDirection;
            }
            else
            {
                direction = AimDirection;
            }

            float distance = Mathf.Max(0f, dashDistance);
            Collider2D bodyCollider = GetComponent<Collider2D>();
            Vector2 castOrigin = bodyCollider != null
                ? bodyCollider.bounds.center
                : Rigidbody.position;
            RaycastHit2D hit = GetDashBlockingHit(
                bodyCollider, castOrigin, direction, distance);

            float travelDistance = distance;
            bool hitEnemy = false;
            if (hit.collider != null)
            {
                BaseEntity target = hit.collider.GetComponentInParent<BaseEntity>();
                if (target != null && target != this && !target.IsDead)
                {
                    travelDistance = Mathf.Max(0f, hit.distance - 0.02f);
                    hitEnemy = _combat != null && _combat.DashAttack(target);
                }
                else
                {
                    travelDistance = Mathf.Max(0f, hit.distance - 0.02f);
                }
            }

            Rigidbody.position += direction * travelDistance;
            Rigidbody.linearVelocity = new Vector2(0f, Rigidbody.linearVelocity.y);

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
                float playerHalfWidth = playerCollider != null
                    ? playerCollider.bounds.extents.x
                    : 0f;
                float targetHalfWidth = targetCollider != null
                    ? targetCollider.bounds.extents.x
                    : 0f;
                float standOff = playerHalfWidth + targetHalfWidth + 0.02f;
                Vector2 destination = new Vector2(
                    target.transform.position.x - direction * standOff,
                    target.transform.position.y);
                Vector2 delta = destination - Rigidbody.position;
                float distance = delta.magnitude;
                Vector2 dashDirection = distance > 0.0001f
                    ? delta / distance
                    : Vector2.right * direction;
                Collider2D bodyCollider = GetComponent<Collider2D>();
                Vector2 castOrigin = bodyCollider != null
                    ? bodyCollider.bounds.center
                    : Rigidbody.position;
                RaycastHit2D hit = GetDashBlockingHit(
                    bodyCollider, castOrigin, dashDirection, distance);
                float travelDistance = hit.collider != null
                    ? Mathf.Max(0f, hit.distance - 0.02f)
                    : distance;

                Rigidbody.position += dashDirection * travelDistance;
                Rigidbody.linearVelocity = Vector2.zero;
                _facingDirection = direction;

                if (Animation != null)
                {
                    Animation.transform.localScale = new Vector3(
                        Mathf.Abs(_initialScale.x) * direction,
                        _initialScale.y,
                        _initialScale.z);
                }

                BaseEntity hitTarget = hit.collider != null
                    ? hit.collider.GetComponentInParent<BaseEntity>()
                    : null;
                bool blockedByWall = hit.collider != null &&
                                     (hitTarget == null || hitTarget == this);
                if (!blockedByWall &&
                    (hitTarget == target || hit.collider == null))
                {
                    hitEnemy = _combat != null && _combat.DashAttack(target);
                }
            }

            if (Animation is PlayerAnimation playerAnimation)
                playerAnimation.PlayDash(hitEnemy);
        }

        private RaycastHit2D GetDashBlockingHit(
            Collider2D bodyCollider,
            Vector2 castOrigin,
            Vector2 direction,
            float distance)
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

                bool isGroundContactDuringHorizontalDash =
                    IsGrounded &&
                    Mathf.Abs(direction.y) <= 0.01f &&
                    hit.normal.y > 0.5f;
                if (isGroundContactDuringHorizontalDash)
                    continue;

                if (closestHit.collider == null || hit.distance < closestHit.distance)
                    closestHit = hit;
            }

            return closestHit;
        }

        private void StartParry()
        {
            if (IsDead || IsDashing || _isParrying || _parryCooldownRemaining > 0f)
                return;

            _isParrying = true;
            _parrySucceeded = false;
            _parryCooldownRemaining = parryCooldown;
            if (Animation is PlayerAnimation playerAnimation)
                playerAnimation.PlayParry(true);
        }

        private void EndParry()
        {
            if (!_isParrying)
                return;

            _isParrying = false;
            if (Animation is PlayerAnimation playerAnimation)
            {
                playerAnimation.PlayParry(false);
                if (!_parrySucceeded)
                    playerAnimation.PlayParryFail();
            }
        }

        private void CheckGround()
        {
            if (groundCheck == null)
            {
                IsGrounded = false;
                return;
            }

            IsGrounded = Physics2D.OverlapCircle(
                groundCheck.position,
                groundCheckRadius,
                groundLayer
            );

            if (Animation is PlayerAnimation playerAnimation)
                playerAnimation.SetGrounded(IsGrounded);

            if (IsGrounded && Rigidbody.linearVelocity.y <= 0f)
                _jumpCount = 0;
        }

        public void Move()
        {
            if (IsDead || IsDashing)
                return;

            Vector2 velocity = Rigidbody.linearVelocity;
            velocity.x = MoveInput * CurrentMoveSpeed;
            Rigidbody.linearVelocity = velocity;
            
            if (Animation != null)
            {
                if (MoveInput > 0)
                {
                    _facingDirection = 1f;
                    Animation.transform.localScale = new Vector3(Mathf.Abs(_initialScale.x),
                        _initialScale.y, _initialScale.z);
                }
                else if (MoveInput < 0)
                {
                    _facingDirection = -1f;
                    Animation.transform.localScale = new Vector3(-Mathf.Abs(_initialScale.x),
                        _initialScale.y, _initialScale.z);
                }
            }
        }

        public override bool ReceiveHit(BaseEntity attacker)
        {
            if (IsDead)
                return false;

            if (IsDashing)
                return true;

            if (IsParrying)
            {
                Vector2 toAttacker = attacker != null
                    ? (Vector2)attacker.transform.position - (Vector2)transform.position
                    : Vector2.zero;
                bool attackerIsInFront =
                    toAttacker.sqrMagnitude > 0f &&
                    Vector2.Dot(toAttacker.normalized, Vector2.right * _facingDirection) > 0f;

                if (attackerIsInFront)
                {
                    _parrySucceeded = true;
                    EndParry();
                    StartCounterDash(attacker);
                    return true;
                }
            }

            return base.ReceiveHit(attacker);
        }

        private void StartCounterDash(BaseEntity target)
        {
            if (target == null || target.IsDead ||
                StateMachine == null || StateMachine.CurrentState == null)
                return;

            _counterDashTarget = target;
            _dashCooldownRemaining = Mathf.Max(0f, dashCooldown);
            StateMachine.ChangeState(DashState);
        }

        public override void ResetForRoom(Vector3 position)
        {
            base.ResetForRoom(position);
            _jumpRequested = false;
            _jumpCount = 0;
            _dashCooldownRemaining = 0f;
            _katanaAttackCooldownRemaining = 0f;
            if (_isParrying && Animation is PlayerAnimation playerAnimation)
                playerAnimation.PlayParry(false);
            _isParrying = false;
            _parrySucceeded = false;
            _parryCooldownRemaining = 0f;
            _counterDashTarget = null;
            StateMachine?.RestartState(IdleState);
            CheckGround();
        }

        public bool Jump()
        {
            if (!_jumpRequested || IsDead ||
                (!IsGrounded && _jumpCount >= Mathf.Max(1, maxJumpCount)))
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
            if (IsDead || IsParrying || StateMachine == null || StateMachine.CurrentState == null ||
                StateMachine.CurrentState.Value.stateName == AttackState)
                return;

            bool usesKatana = _combat != null && _combat.IsKatanaEquipped;
            if (usesKatana && _katanaAttackCooldownRemaining > 0f)
                return;

            StateMachine.ChangeState(AttackState);

            if (usesKatana)
                _katanaAttackCooldownRemaining = _combat.KatanaAttackCooldown;
        }

        public override void OnDestroy()
        {
            if (inputData != null)
            {
                inputData.OnJumpPressed -= HandleJumpPressed;
                inputData.OnAttackPressed -= RequestAttack;
                inputData.OnDashPressed -= StartDash;
                inputData.OnReflectPressed -= StartParry;
                inputData.OnReflectReleased -= EndParry;
            }

            base.OnDestroy();
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck == null)
                return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position,
                groundCheckRadius);
        }
    }
}