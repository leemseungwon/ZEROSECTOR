using UnityEngine;

namespace LSW._02._Code.Entity.Boss.Guard_Boss.Components
{
    public class GuardBossMovementComponent : MonoBehaviour, IEntityComponent
    {
        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField, Min(0.005f)] private float groundCheckRadius = 0.05f;
        [SerializeField, Min(0f)] private float groundProbeDistance = 0.04f;
        [SerializeField] private LayerMask groundLayers = (1 << 0) | (1 << 6);

        [Header("Run & Jump")]
        [SerializeField, Min(0f)] private float runSpeed = 4f;
        [SerializeField, Min(0f)] private float gravity = 22f;
        [SerializeField, Min(0f)] private float maxFallSpeed = 22f;
        [SerializeField, Min(0f)] private float jumpForce = 11f;
        [SerializeField, Min(0f)] private float jumpPreparation = 0.35f;
        [SerializeField, Min(0f)] private float jumpCooldown = 3f;

        [Header("Approach Dash")]
        [SerializeField, Min(0.1f)] private float approachDashSpeed = 12f;
        [SerializeField, Min(0f)] private float approachDashDuration = 0.35f;
        [SerializeField, Min(0f)] private float approachDashCooldown = 4f;

        [Header("Ground Evasion Dash")]
        [SerializeField, Min(0.1f)] private float evadeDashSpeed = 13f;
        [SerializeField, Min(0f)] private float evadeDashDuration = 0.3f;
        [SerializeField, Min(0f)] private float evadeDashCooldown = 4.5f;
        [SerializeField, Min(0f)] private float evadeDistance = 3f;

        [Header("Air Evasion Dash")]
        [SerializeField, Min(0.1f)] private float airEvadeDashSpeed = 13f;
        [SerializeField, Min(0f)] private float airEvadeDashDuration = 0.3f;
        [SerializeField, Min(0f)] private float airEvadeDashCooldown = 5f;

        private GuardBoss _boss;
        private Collider2D _bodyCollider;
        private Vector3 _initialVisualScale = Vector3.one;
        private float _facingDirection = 1f;
        private float _jumpCooldownRemaining;
        private float _approachDashCooldownRemaining;
        private float _evadeDashCooldownRemaining;
        private float _airEvadeDashCooldownRemaining;

        public bool IsGrounded { get; private set; }
        public float FacingDirection => _facingDirection;
        public float JumpPreparation => jumpPreparation;
        public float ApproachDashSpeed => approachDashSpeed;
        public float ApproachDashDuration => approachDashDuration;
        public float EvadeDashSpeed => evadeDashSpeed;
        public float EvadeDashDuration => evadeDashDuration;
        public float AirEvadeDashSpeed => airEvadeDashSpeed;
        public float AirEvadeDashDuration => airEvadeDashDuration;
        public float EvadeDistance => evadeDistance;
        public bool CanJump => _jumpCooldownRemaining <= 0f;
        public bool CanApproachDash => _approachDashCooldownRemaining <= 0f;
        public bool CanEvadeDash => _evadeDashCooldownRemaining <= 0f;
        public bool CanAirEvadeDash => _airEvadeDashCooldownRemaining <= 0f;

        public void Initialize(BaseEntity owner)
        {
            _boss = owner as GuardBoss;
            _bodyCollider = owner.GetComponent<Collider2D>();
        }

        public void Reset()
        {
            ResetCooldowns();
        }

        public void SetInitialVisualScale(Vector3 scale)
        {
            _initialVisualScale = scale;
            _facingDirection = Mathf.Sign(scale.x);
            if (Mathf.Approximately(_facingDirection, 0f))
                _facingDirection = 1f;
        }

        public void TickCooldowns(float deltaTime)
        {
            _jumpCooldownRemaining = Mathf.Max(0f, _jumpCooldownRemaining - deltaTime);
            _approachDashCooldownRemaining = Mathf.Max(0f, _approachDashCooldownRemaining - deltaTime);
            _evadeDashCooldownRemaining = Mathf.Max(0f, _evadeDashCooldownRemaining - deltaTime);
            _airEvadeDashCooldownRemaining = Mathf.Max(0f, _airEvadeDashCooldownRemaining - deltaTime);
        }

        public void ResetCooldowns()
        {
            _jumpCooldownRemaining = 0f;
            _approachDashCooldownRemaining = 0f;
            _evadeDashCooldownRemaining = 0f;
            _airEvadeDashCooldownRemaining = 0f;
        }

        public void RefreshGrounded()
        {
            if (_boss == null)
            {
                IsGrounded = false;
                return;
            }

            if (_boss.Rigidbody != null && _boss.Rigidbody.linearVelocity.y > 0.01f)
            {
                IsGrounded = false;
                return;
            }

            if (_bodyCollider == null)
                _bodyCollider = _boss.GetComponent<Collider2D>();

            Vector2 feetPosition = _bodyCollider != null
                ? new Vector2(_bodyCollider.bounds.center.x, _bodyCollider.bounds.min.y + 0.01f)
                : groundCheck != null
                    ? (Vector2)groundCheck.position
                    : (Vector2)transform.position + Vector2.down * 0.55f;

            bool overlapsGround = Physics2D.OverlapCircle(feetPosition, groundCheckRadius, groundLayers) != null;
            bool reachesGround = Physics2D.Raycast(
                feetPosition, Vector2.down, groundProbeDistance, groundLayers);
            IsGrounded = overlapsGround || reachesGround;
        }

        public void ApplyGravity(float deltaTime)
        {
            if (_boss == null || _boss.Rigidbody == null || _boss.IsDead)
                return;

            Vector2 velocity = _boss.Rigidbody.linearVelocity;
            if (IsGrounded && velocity.y <= 0f)
                velocity.y = 0f;
            else
                velocity.y = Mathf.Max(velocity.y - gravity * deltaTime, -maxFallSpeed);
            _boss.Rigidbody.linearVelocity = velocity;
        }

        public void MoveTowardTarget(float stoppingDistance)
        {
            if (_boss == null || !_boss.HasLivingTarget())
            {
                StopHorizontal();
                return;
            }

            float deltaX = _boss.TargetPlayer.transform.position.x - _boss.transform.position.x;
            FaceDirection(Mathf.Abs(deltaX) > 0.01f ? Mathf.Sign(deltaX) : _facingDirection);
            float speed = Mathf.Abs(deltaX) > stoppingDistance ? _facingDirection * runSpeed : 0f;
            SetHorizontalVelocity(speed);
            _boss.BossAnimation?.SetMoving(IsGrounded && Mathf.Abs(speed) > 0.01f);
        }

        public void FaceTarget()
        {
            if (_boss == null || !_boss.HasLivingTarget())
                return;

            float deltaX = _boss.TargetPlayer.transform.position.x - _boss.transform.position.x;
            if (Mathf.Abs(deltaX) > 0.01f)
                FaceDirection(Mathf.Sign(deltaX));
        }

        public void FaceDirection(float direction)
        {
            if (Mathf.Abs(direction) <= 0.01f)
                return;

            _facingDirection = Mathf.Sign(direction);
            if (_boss == null || _boss.Animation == null)
                return;

            Vector3 scale = _initialVisualScale;
            scale.x = Mathf.Abs(scale.x) * _facingDirection;
            _boss.Animation.transform.localScale = scale;
        }

        public void StartJump()
        {
            _jumpCooldownRemaining = jumpCooldown;
        }

        public void Jump()
        {
            if (_boss == null || _boss.Rigidbody == null)
                return;

            _boss.ApplyJumpInvincibility();
            Vector2 velocity = _boss.Rigidbody.linearVelocity;
            velocity.y = jumpForce;
            _boss.Rigidbody.linearVelocity = velocity;
        }

        public void StartApproachDash()
        {
            _boss?.ApplyDashInvincibility();
            _approachDashCooldownRemaining = approachDashCooldown;
            _boss?.BossAnimation?.SetMoving(false);
            SetHorizontalVelocity(_facingDirection * approachDashSpeed);
        }

        public void StartEvadeDash(float speed)
        {
            _boss?.ApplyDashInvincibility();
            _evadeDashCooldownRemaining = evadeDashCooldown;
            FaceDirection(-_facingDirection);
            _boss?.BossAnimation?.SetMoving(false);
            SetHorizontalVelocity(_facingDirection * speed);
        }

        public void StartAirEvadeDash()
        {
            _boss?.ApplyDashInvincibility();
            _airEvadeDashCooldownRemaining = airEvadeDashCooldown;
            FaceDirection(-_facingDirection);
            _boss?.BossAnimation?.SetMoving(false);
            SetHorizontalVelocity(_facingDirection * airEvadeDashSpeed);
        }

        public void StartAirAttackDash(float speed)
        {
            _boss?.ApplyDashInvincibility();
            _boss?.BossAnimation?.SetMoving(false);
            SetHorizontalVelocity(_facingDirection * speed);
        }

        public void SetHorizontalVelocity(float speed)
        {
            if (_boss == null || _boss.Rigidbody == null)
                return;

            Vector2 velocity = _boss.Rigidbody.linearVelocity;
            velocity.x = speed;
            _boss.Rigidbody.linearVelocity = velocity;
        }

        public void StopHorizontal()
        {
            SetHorizontalVelocity(0f);
            _boss?.BossAnimation?.SetMoving(false);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck != null
                ? groundCheck.position
                : (Vector2)transform.position + Vector2.down * 0.55f, groundCheckRadius);
        }
    }
}
