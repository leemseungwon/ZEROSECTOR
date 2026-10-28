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
        [SerializeField] private LayerMask airDashCollisionLayers = (1 << 0) | (1 << 6);
        [SerializeField, Min(0f)] private float ledgeCheckForward = 0.08f;
        [SerializeField, Min(0.01f)] private float ledgeProbeDistance = 0.35f;
        [SerializeField, Range(0f, 80f)] private float maxWalkableSlopeAngle = 55f;

        [Header("Run & Jump")]
        [SerializeField, Min(0f)] private float gravity = 22f;
        [SerializeField, Min(0f)] private float maxFallSpeed = 22f;
        [SerializeField, Min(0f)] private float jumpForce = 11f;
        [SerializeField, Min(0f)] private float jumpPreparation = 0.35f;
        [SerializeField, Min(0f)] private float jumpCooldown = 3f;
        [SerializeField, Min(0f)] private float jumpTargetHeightThreshold = 0.35f;
        [SerializeField, Min(0f)] private float elevatedPlatformHeightThreshold = 0.35f;
        [SerializeField, Min(0f)] private float jumpReachSafetyMargin = 0.2f;
        [SerializeField, Min(0f)] private float jumpAttackLiftDuration = 0.12f;

        [Header("Chase")]
        [SerializeField, Min(0f)] private float chasePositionCorrection = 18f;

        [Header("Approach Dash")]
        [SerializeField, Min(0f)] private float approachDashTriggerDistance = 5f;
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
        private bool _isAirDashing;
        private bool _airDashBlockedVertically;
        private bool _airDashWallReported;
        private bool _approachDashWallReported;
        private bool _jumpInProgress;
        private bool _gravitySuspended;
        private bool _holdVerticalAfterJumpAttackLift;
        private float _jumpAttackLiftRemaining;
        private float _groundMoveSpeed;
        private Vector2 _groundNormal = Vector2.up;
        private readonly RaycastHit2D[] _airDashHitBuffer = new RaycastHit2D[8];
        private readonly RaycastHit2D[] _groundHitBuffer = new RaycastHit2D[8];

        public bool IsGrounded { get; private set; }
        public float FacingDirection => _facingDirection;
        public float VerticalVelocity => _boss != null && _boss.Rigidbody != null
            ? _boss.Rigidbody.linearVelocity.y
            : 0f;
        public float JumpPreparation => jumpPreparation;
        public float JumpTargetHeightThreshold => jumpTargetHeightThreshold;
        public float ElevatedPlatformHeightThreshold => elevatedPlatformHeightThreshold;
        public float MaxJumpReachHeight => gravity > 0f
            ? Mathf.Max(0f, jumpForce * jumpForce / (2f * gravity) - jumpReachSafetyMargin)
            : float.PositiveInfinity;
        public float ApproachDashTriggerDistance => approachDashTriggerDistance;
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

        public bool CanJumpToHeight(float height)
        {
            return height > 0f && height <= MaxJumpReachHeight;
        }

        public float HeightDifferenceTo(Transform target)
        {
            if (target == null)
                return 0f;

            if (_bodyCollider == null && _boss != null)
                _bodyCollider = _boss.GetComponent<Collider2D>();

            Collider2D targetCollider = target.GetComponent<Collider2D>();
            if (targetCollider == null)
                targetCollider = target.GetComponentInChildren<Collider2D>();

            if (_bodyCollider != null && targetCollider != null)
                return targetCollider.bounds.min.y - _bodyCollider.bounds.min.y;

            return target.position.y - transform.position.y;
        }

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
            SuspendGravity(false);
            _jumpCooldownRemaining = 0f;
            _approachDashCooldownRemaining = 0f;
            _evadeDashCooldownRemaining = 0f;
            _airEvadeDashCooldownRemaining = 0f;
            _jumpInProgress = false;
            _groundMoveSpeed = 0f;
            _groundNormal = Vector2.up;
        }

        public void RefreshGrounded()
        {
            if (_boss == null)
            {
                IsGrounded = false;
                return;
            }

            if (_boss.Rigidbody != null && _jumpInProgress && _boss.Rigidbody.linearVelocity.y > 0.01f)
            {
                IsGrounded = false;
                return;
            }

            if (_bodyCollider == null)
                _bodyCollider = _boss.GetComponent<Collider2D>();

            if (_bodyCollider == null)
            {
                Vector2 feetPosition = groundCheck != null
                    ? (Vector2)groundCheck.position
                    : (Vector2)transform.position + Vector2.down * 0.55f;
                IsGrounded = Physics2D.OverlapCircle(feetPosition, groundCheckRadius, groundLayers) != null;
                _groundNormal = Vector2.up;
                return;
            }

            var filter = new ContactFilter2D();
            filter.SetLayerMask(groundLayers);
            filter.useTriggers = false;
            float minNormalY = Mathf.Cos(maxWalkableSlopeAngle * Mathf.Deg2Rad);
            float castDistance = Mathf.Max(groundCheckRadius + groundProbeDistance + 0.03f, 0.05f);
            int hitCount = _bodyCollider.Cast(Vector2.down, filter, _groundHitBuffer, castDistance);
            RaycastHit2D groundHit = default;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit2D hit = _groundHitBuffer[i];
                if (hit.collider == null || hit.collider.isTrigger || hit.normal.y < minNormalY ||
                    hit.collider.transform.IsChildOf(_boss.transform))
                    continue;

                if (groundHit.collider == null || hit.distance < groundHit.distance)
                    groundHit = hit;
            }

            float groundedDistance = Mathf.Max(0.025f, groundProbeDistance);
            float groundedThreshold = IsGrounded ? groundedDistance + 0.03f : groundedDistance;
            IsGrounded = groundHit.collider != null && groundHit.distance <= groundedThreshold;
            _groundNormal = IsGrounded ? groundHit.normal : Vector2.up;
            if (IsGrounded && _boss.Rigidbody.linearVelocity.y <= 0.01f)
                _jumpInProgress = false;
        }

        public void ApplyGravity(float deltaTime)
        {
            if (_boss == null || _boss.Rigidbody == null || _boss.IsDead)
                return;

            Vector2 velocity = _boss.Rigidbody.linearVelocity;
            if (_gravitySuspended)
            {
                if (_holdVerticalAfterJumpAttackLift)
                {
                    if (_jumpAttackLiftRemaining > 0f)
                        _jumpAttackLiftRemaining = Mathf.Max(0f, _jumpAttackLiftRemaining - deltaTime);
                    else
                        velocity.y = 0f;
                }

                _boss.Rigidbody.linearVelocity = velocity;
                return;
            }

            if (_isAirDashing && _airDashBlockedVertically)
            {
                velocity.y = 0f;
                _boss.Rigidbody.linearVelocity = velocity;
                return;
            }

            if (IsGrounded)
            {
                velocity.y = 0f;
                if (Mathf.Abs(_groundMoveSpeed) > 0.01f)
                {
                    Vector2 tangent = new Vector2(_groundNormal.y, -_groundNormal.x).normalized;
                    if (tangent.x * _groundMoveSpeed < 0f)
                        tangent = -tangent;
                    velocity = tangent * Mathf.Abs(_groundMoveSpeed);
                }
            }
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
            float direction = Mathf.Abs(deltaX) > 0.01f ? Mathf.Sign(deltaX) : _facingDirection;
            FaceDirection(direction);

            float playerSpeedX = _boss.TargetPlayer.Rigidbody != null
                ? _boss.TargetPlayer.Rigidbody.linearVelocity.x
                : 0f;
            float positionError = deltaX - direction * stoppingDistance;
            float speed = Mathf.Clamp(
                playerSpeedX + positionError * chasePositionCorrection,
                -_boss.CurrentMoveSpeed,
                _boss.CurrentMoveSpeed);

            // Chase without backing away when the player moves toward or past the boss.
            if (speed * direction < 0f)
                speed = 0f;

            if (IsGrounded && Mathf.Abs(speed) > 0.01f &&
                !HasGroundAhead(Mathf.Sign(speed), Mathf.Abs(speed) * Time.fixedDeltaTime))
            {
                speed = 0f;
            }

            _groundMoveSpeed = IsGrounded ? speed : 0f;
            Vector2 velocity = _boss.Rigidbody.linearVelocity;
            velocity.x = speed;
            _boss.Rigidbody.linearVelocity = velocity;
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

        public void Jump()
        {
            if (_boss == null || _boss.Rigidbody == null)
                return;

            _jumpInProgress = true;
            _groundMoveSpeed = 0f;
            _boss.ApplyJumpInvincibility();
            Vector2 velocity = _boss.Rigidbody.linearVelocity;
            velocity.y = jumpForce;
            _boss.Rigidbody.linearVelocity = velocity;
        }

        public void SuspendGravity(bool suspended)
        {
            if (!suspended && _gravitySuspended && _holdVerticalAfterJumpAttackLift &&
                _jumpAttackLiftRemaining > 0f && _boss != null && _boss.Rigidbody != null)
            {
                Vector2 velocity = _boss.Rigidbody.linearVelocity;
                velocity.y = 0f;
                _boss.Rigidbody.linearVelocity = velocity;
            }

            _gravitySuspended = suspended;
            _holdVerticalAfterJumpAttackLift = false;
            _jumpAttackLiftRemaining = 0f;
        }

        public void BeginJumpAttackLift()
        {
            if (_boss == null || _boss.Rigidbody == null)
                return;

            _gravitySuspended = true;
            _holdVerticalAfterJumpAttackLift = true;
            _jumpAttackLiftRemaining = jumpAttackLiftDuration;

            Vector2 velocity = _boss.Rigidbody.linearVelocity;
            if (velocity.y < 0f)
                velocity.y = 0f;
            _boss.Rigidbody.linearVelocity = velocity;
        }

        public void StartJump()
        {
            _jumpCooldownRemaining = jumpCooldown;
        }

        public void StartApproachDash()
        {
            _approachDashWallReported = false;
            _boss?.ApplyDashInvincibility();
            _approachDashCooldownRemaining = approachDashCooldown;
            _boss?.BossAnimation?.SetMoving(false);
            float direction = _boss != null && _boss.HasLivingTarget()
                ? Mathf.Sign(_boss.TargetPlayer.transform.position.x - _boss.transform.position.x)
                : _facingDirection;
            if (Mathf.Approximately(direction, 0f))
                direction = _facingDirection;
            FaceDirection(direction);
            SetHorizontalVelocity(direction * approachDashSpeed);
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
            _boss?.BossAnimation?.SetMoving(false);

            Vector2 direction = _boss != null && _boss.TargetPlayer != null
                ? (Vector2)_boss.transform.position - (Vector2)_boss.TargetPlayer.transform.position
                : new Vector2(-_facingDirection, 1f);
            if (direction.sqrMagnitude < 0.0001f)
                direction = new Vector2(-_facingDirection, 0f);

            if (Mathf.Abs(direction.x) > 0.01f)
                FaceDirection(Mathf.Sign(direction.x));
            BeginAirDash(direction, airEvadeDashSpeed);
        }

        public void StartAirAttackDash(float speed)
        {
            _boss?.ApplyDashInvincibility();
            _boss?.BossAnimation?.SetMoving(false);

            Vector2 direction = _boss != null && _boss.TargetPlayer != null
                ? (Vector2)_boss.TargetPlayer.transform.position - (Vector2)_boss.transform.position
                : new Vector2(_facingDirection, 0f);
            if (direction.sqrMagnitude < 0.0001f)
                direction = new Vector2(_facingDirection, 0f);

            if (Mathf.Abs(direction.x) > 0.01f)
                FaceDirection(Mathf.Sign(direction.x));
            BeginAirDash(direction, speed);
        }

        private void BeginAirDash(Vector2 direction, float speed)
        {
            if (_boss == null || _boss.Rigidbody == null)
                return;

            _isAirDashing = true;
            _airDashWallReported = false;
            _airDashBlockedVertically = false;
            Vector2 velocity = direction.normalized * Mathf.Max(0f, speed);
            _boss.Rigidbody.linearVelocity = velocity;
        }

        public void ConstrainAirDashVelocity(float deltaTime)
        {
            if (!_isAirDashing || _boss == null || _boss.Rigidbody == null)
                return;

            Vector2 velocity = _boss.Rigidbody.linearVelocity;
            float probePadding = 0.02f;
            if (Mathf.Abs(velocity.x) > 0.01f && IsBlockedAlongAxis(
                    Vector2.right * Mathf.Sign(velocity.x), Mathf.Abs(velocity.x) * deltaTime + probePadding))
            {
                velocity.x = 0f;
                if (!_airDashWallReported && IsBlockedByWall(
                        Vector2.right * Mathf.Sign(_boss.Rigidbody.linearVelocity.x), probePadding + 0.05f))
                {
                    _airDashWallReported = true;
                    _boss.Combat?.OnDashHitWall();
                }
            }

            if (Mathf.Abs(velocity.y) > 0.01f && IsBlockedAlongAxis(
                    Vector2.up * Mathf.Sign(velocity.y), Mathf.Abs(velocity.y) * deltaTime + probePadding))
            {
                velocity.y = 0f;
                _airDashBlockedVertically = true;
            }

            _boss.Rigidbody.linearVelocity = velocity;
        }

        public void ConstrainApproachDashVelocity(float deltaTime)
        {
            if (_boss == null || _boss.Rigidbody == null)
                return;

            Vector2 velocity = _boss.Rigidbody.linearVelocity;
            if (Mathf.Abs(velocity.x) <= 0.01f || !IsBlockedAlongAxis(
                    Vector2.right * Mathf.Sign(velocity.x), Mathf.Abs(velocity.x) * deltaTime + 0.02f))
                return;

            if (!_approachDashWallReported && IsBlockedByWall(
                    Vector2.right * Mathf.Sign(velocity.x), 0.08f))
            {
                _approachDashWallReported = true;
                _boss.Combat?.OnDashHitWall();
            }

            velocity.x = 0f;
            _boss.Rigidbody.linearVelocity = velocity;
        }

        private bool IsBlockedByWall(Vector2 direction, float distance)
        {
            if (_bodyCollider == null && _boss != null)
                _bodyCollider = _boss.GetComponent<Collider2D>();
            if (_bodyCollider == null)
                return false;

            var filter = new ContactFilter2D();
            filter.SetLayerMask(airDashCollisionLayers);
            filter.useTriggers = false;
            int hitCount = _bodyCollider.Cast(direction, filter, _airDashHitBuffer, Mathf.Max(0.03f, distance));
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit2D hit = _airDashHitBuffer[i];
                if (hit.collider == null || hit.collider.transform.IsChildOf(_boss.transform))
                    continue;

                if (Vector2.Dot(hit.normal, direction) < -0.01f && Mathf.Abs(hit.normal.x) >= 0.7f)
                    return true;
            }

            return false;
        }

        private bool IsBlockedAlongAxis(Vector2 direction, float distance)
        {
            if (_bodyCollider == null && _boss != null)
                _bodyCollider = _boss.GetComponent<Collider2D>();
            if (_bodyCollider == null)
                return false;

            var filter = new ContactFilter2D();
            filter.SetLayerMask(airDashCollisionLayers);
            filter.useTriggers = false;

            int hitCount = _bodyCollider.Cast(direction, filter, _airDashHitBuffer, distance);
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hitCollider = _airDashHitBuffer[i].collider;
                if (hitCollider == null || hitCollider.transform.IsChildOf(_boss.transform))
                    continue;

                if (Vector2.Dot(_airDashHitBuffer[i].normal, direction) < -0.01f)
                    return true;
            }

            return false;
        }

        public void EndAirDash()
        {
            if (!_isAirDashing)
                return;

            _isAirDashing = false;
            _airDashBlockedVertically = false;
            _airDashWallReported = false;
            if (_boss == null || _boss.Rigidbody == null)
                return;

            _boss.Rigidbody.linearVelocity = Vector2.zero;
        }

        public void SetHorizontalVelocity(float speed)
        {
            if (_boss == null || _boss.Rigidbody == null)
                return;

            _groundMoveSpeed = 0f;

            if (IsGrounded && Mathf.Abs(speed) > 0.01f &&
                !HasGroundAhead(Mathf.Sign(speed), Mathf.Abs(speed) * Time.fixedDeltaTime))
                speed = 0f;

            Vector2 velocity = _boss.Rigidbody.linearVelocity;
            velocity.x = speed;
            _boss.Rigidbody.linearVelocity = velocity;
        }

        public void StopHorizontalAtLedge()
        {
            if (_boss == null || _boss.Rigidbody == null || !IsGrounded || _isAirDashing)
                return;

            float horizontalSpeed = _boss.Rigidbody.linearVelocity.x;
            if (Mathf.Abs(horizontalSpeed) <= 0.01f)
                return;

            if (!HasGroundAhead(Mathf.Sign(horizontalSpeed),
                    Mathf.Abs(horizontalSpeed) * Time.fixedDeltaTime))
            {
                StopHorizontal();
            }
        }

        private bool HasGroundAhead(float direction, float additionalDistance)
        {
            if (_bodyCollider == null && _boss != null)
                _bodyCollider = _boss.GetComponent<Collider2D>();

            Bounds bounds = _bodyCollider != null
                ? _bodyCollider.bounds
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
                   !hit.collider.transform.IsChildOf(_boss.transform);
        }

        public void StopHorizontal()
        {
            _groundMoveSpeed = 0f;
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
