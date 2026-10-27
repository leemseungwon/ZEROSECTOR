using System.Collections.Generic;
using LSW._02._Code.Common;
using LSW._02._Code.Entity.Boss.Guard_Boss.Components;
using LSW._02._Code.Entity.Boss.Guard_Boss.States;
using LSW._02._Code.Entity.Players;
using LSW._02._Code.System;
using LSW._02._Code.System.HandlePlayerSystems;
using UnityEngine;

namespace LSW._02._Code.Entity.Boss.Guard_Boss
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class GuardBoss : BaseBoss
    {
        public const string IdleState = "GuardBossIdleState";
        public const string ChaseState = "GuardBossChaseState";
        public const string BasicAttackState = "GuardBossBasicAttackState";
        public const string ComboAttackState = "GuardBossComboAttackState";
        public const string HeavyAttackState = "GuardBossHeavyAttackState";
        public const string AirComboState = "GuardBossAirComboState";
        public const string AirDashAttackState = "GuardBossAirDashAttackState";
        public const string JumpState = "GuardBossJumpState";
        public const string AirborneState = "GuardBossAirborneState";
        public const string ApproachDashState = "GuardBossApproachDashState";
        public const string EvadeDashState = "GuardBossEvadeDashState";
        public const string AirEvadeDashState = "GuardBossAirEvadeDashState";
        public const string LandingState = "GuardBossLandingState";
        public const string HitState = "GuardBossHitState";
        public const string DeadState = "GuardBossDeadState";

        [Header("State Timing")]
        [SerializeField, Min(0f)] private float landingDuration = 0.35f;
        [SerializeField, Min(0f)] private float hitStunDuration = 0.12f;
        [SerializeField, Min(0f)] private float hitKnockback = 4f;
        [SerializeField, Min(0.1f)] private float deathFallbackDuration = 2f;

        [Header("Invincibility")]
        [SerializeField, Min(0f)] private float jumpInvincibilityDuration = 0.7f;
        [SerializeField, Min(0f)] private float dashInvincibilityDuration = 0.45f;
        [SerializeField, Min(0f)] private float hitInvincibilityDuration = 0.65f;

        private bool _statesInitialized;
        private bool _wasGrounded;
        private Vector3 _initialVisualScale = Vector3.one;

        public PlayerController TargetPlayer => _targetPlayer;
        public GuardBossMovementComponent Movement => _movement;
        public GuardBossCombatComponent Combat => _combat;
        public GuardBossAnimation BossAnimation => _bossAnimation;
        public bool IsGrounded => _movement != null && _movement.IsGrounded;
        public float LandingDuration => landingDuration;
        public float HitStunDuration => hitStunDuration;
        public float DeathFallbackDuration => deathFallbackDuration;
        
        private PlayerController _targetPlayer;
        private GuardBossMovementComponent _movement;
        private GuardBossCombatComponent _combat;
        private GuardBossAnimation _bossAnimation;
        private EntityBuffComponent _buffComponent;

        protected override void Awake()
        {
            base.Awake();
            if(SystemHandler.Instance.GetSystem(out HandlePlayerSystem playerSystem))
                _targetPlayer = playerSystem.Player;
            GetCompo(out _movement);
            GetCompo(out _combat);
            GetCompo(out _bossAnimation);
            GetCompo(out _buffComponent);

            if (Animation != null)
                _initialVisualScale = Animation.transform.localScale;
            if (_movement != null)
                _movement.SetInitialVisualScale(_initialVisualScale);

            Rigidbody.gravityScale = 0f;
            ResolveTarget();
            InitializeStates();
            _movement?.RefreshGrounded();
            _wasGrounded = IsGrounded;
        }

        protected virtual void Start()
        {
            if (StateMachine.CurrentState == null)
                StateMachine.Initialize(IdleState);
        }

        protected override void Update()
        {
            ResolveTarget();
            _movement?.TickCooldowns(Time.deltaTime);
            _combat?.TickCooldowns(Time.deltaTime);
            _movement?.RefreshGrounded();

            bool justLanded = !_wasGrounded && IsGrounded;
            _wasGrounded = IsGrounded;

            if (IsDead)
            {
                _movement?.StopHorizontal();
                return;
            }

            if (justLanded && StateMachine?.CurrentState?.stateName != JumpState)
                StateMachine?.ChangeState(LandingState);

            base.Update();
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            _movement?.ApplyGravity(Time.fixedDeltaTime);
        }

        public override void Initialize()
        {
            base.Initialize();
            _combat?.ResetCooldowns();
            _movement?.ResetCooldowns();
            InitializeStates();
        }

        public override void Spawn()
        {
            base.Spawn();
            IsDead = false;
            _buffComponent?.Reset();
            _combat?.ResetCooldowns();
            _movement?.ResetCooldowns();
            _movement?.RefreshGrounded();
            _wasGrounded = IsGrounded;
            gameObject.SetActive(true);
            InitializeStates();
            StateMachine.RestartState(IdleState);
        }

        public override void Despawn()
        {
            _movement?.StopHorizontal();
            ResetRuntimeState();
            _combat?.ResetCooldowns();
            _movement?.ResetCooldowns();
        }

        public override bool ReceiveHit(BaseEntity attacker)
        {
            if (!base.ReceiveHit(attacker))
                return false;

            if (IsDead)
            {
                StateMachine?.ChangeState(DeadState);
                return true;
            }

            _buffComponent?.ApplyInvincibility(hitInvincibilityDuration);

            Vector2 direction = attacker != null
                ? (Vector2)(transform.position - attacker.transform.position)
                : Vector2.right;
            if (Mathf.Abs(direction.x) < 0.1f)
                direction.x = Mathf.Sign(_initialVisualScale.x);

            Knockback(hitKnockback, direction);
            StateMachine?.ChangeState(HitState);
            return true;
        }

        public void ApplyJumpInvincibility()
        {
            _buffComponent?.ApplyInvincibility(jumpInvincibilityDuration);
        }

        public void ApplyDashInvincibility()
        {
            _buffComponent?.ApplyInvincibility(dashInvincibilityDuration);
        }

        public void ResolveTarget()
        {
            if (_targetPlayer != null)
                return;

            SystemHandler handler = SystemHandler.Instance;
            if (handler != null && handler.GetSystem(out HandlePlayerSystem playerSystem))
                _targetPlayer = playerSystem.Player;
        }

        public bool HasLivingTarget() => _targetPlayer != null && !_targetPlayer.IsDead;

        public float DistanceToTarget()
        {
            return HasLivingTarget()
                ? Vector2.Distance(transform.position, _targetPlayer.transform.position)
                : float.PositiveInfinity;
        }

        public void FaceTarget()
        {
            if (!HasLivingTarget())
                return;

            float direction = _targetPlayer.transform.position.x - transform.position.x;
            if (Mathf.Abs(direction) > 0.01f)
                _movement?.FaceDirection(Mathf.Sign(direction));
        }

        private void InitializeStates()
        {
            if (_statesInitialized)
                return;

            StateMachine = new EntityStateMachine(new List<StateData>
            {
                new() { stateName = IdleState, State = new GuardBossIdleState(this) },
                new() { stateName = ChaseState, State = new GuardBossChaseState(this) },
                new() { stateName = BasicAttackState, State = new GuardBossBasicAttackState(this) },
                new() { stateName = ComboAttackState, State = new GuardBossComboAttackState(this) },
                new() { stateName = HeavyAttackState, State = new GuardBossHeavyAttackState(this) },
                new() { stateName = AirComboState, State = new GuardBossAirComboState(this) },
                new() { stateName = AirDashAttackState, State = new GuardBossAirDashAttackState(this) },
                new() { stateName = JumpState, State = new GuardBossJumpState(this) },
                new() { stateName = AirborneState, State = new GuardBossAirborneState(this) },
                new() { stateName = ApproachDashState, State = new GuardBossApproachDashState(this) },
                new() { stateName = EvadeDashState, State = new GuardBossEvadeDashState(this) },
                new() { stateName = AirEvadeDashState, State = new GuardBossAirEvadeDashState(this) },
                new() { stateName = LandingState, State = new GuardBossLandingState(this) },
                new() { stateName = HitState, State = new GuardBossHitState(this) },
                new() { stateName = DeadState, State = new GuardBossDeadState(this) }
            });
            _statesInitialized = true;
        }
    }
}
