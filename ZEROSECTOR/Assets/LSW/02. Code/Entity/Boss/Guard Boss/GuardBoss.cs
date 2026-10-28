using System;
using System.Collections.Generic;
using LSW._02._Code.Common;
using LSW._02._Code.Entity.Boss.Guard_Boss.Components;
using LSW._02._Code.Entity.Boss.Guard_Boss.States;
using LSW._02._Code.Entity.Enemy;
using LSW._02._Code.Entity.Players;
using LSW._02._Code.Sos.EntityStatDatas;
using LSW._02._Code.System;
using LSW._02._Code.System.HandlePlayerSystems;
using LSW._02._Code.System.PoolSystems;
using UnityEngine;
using Random = UnityEngine.Random;

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
        public const string VulnerableState = "GuardBossVulnerableState";

        [Header("State Timing")]
        [SerializeField, Min(0f)] private float landingDuration = 0.35f;
        [SerializeField, Min(0f)] private float hitStunDuration = 0.12f;
        [SerializeField, Min(0f)] private float hitKnockback = 4f;
        [SerializeField, Min(0.1f)] private float deathFallbackDuration = 2f;

        [Header("Invincibility")]
        [SerializeField, Min(0f)] private float invincibilityDuration = 1f;
        [SerializeField, Min(0f)] private float jumpInvincibilityDuration = 0.7f;
        [SerializeField, Min(0f)] private float dashInvincibilityDuration = 0.45f;

        [Header("Minion Spawning")]
        [SerializeField] private string[] minionPoolName =
        {
            "Enemy_MeleeSoldier",
            "Enemy_RangeSoldier"
        };
        [SerializeField] private Transform minionSpawnPoint;
        [SerializeField] private int maxMinionSpawnCount;
        [SerializeField] private Vector2 minionSpawnOffset = new(0.8f, 1.5f);
        [SerializeField, Min(0f)] private float minionSpawnCooldown = 6f;

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
        public bool IsVulnerable => _combat != null && _combat.IsVulnerable;
        public int CurrentPhase => _combat != null ? _combat.CurrentPhase : 1;
        public override float MaxInvincibilityDuration =>
            Mathf.Max(invincibilityDuration, Mathf.Max(jumpInvincibilityDuration, dashInvincibilityDuration));
        
        private PlayerController _targetPlayer;
        private GuardBossMovementComponent _movement;
        private GuardBossCombatComponent _combat;
        private GuardBossAnimation _bossAnimation;
        private EntityBuffComponent _buffComponent;
        private PoolSystem _poolSystem;
        private readonly List<BaseEnemy> _spawnedMinions = new();
        private float _minionSpawnCooldownRemaining;
        private bool _phaseThreeMinionSpawned;

        public event Action<int> OnPhaseChanged;
        public event Action OnBossDefeated;

        protected override void Awake()
        {
            base.Awake();
            if(SystemHandler.Instance.GetSystem(out HandlePlayerSystem playerSystem))
                _targetPlayer = playerSystem.Player;
            GetCompo(out _movement);
            GetCompo(out _combat);
            GetCompo(out _bossAnimation);
            GetCompo(out _buffComponent);
            if (SystemHandler.Instance != null)
                SystemHandler.Instance.GetSystem(out _poolSystem);

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
            _minionSpawnCooldownRemaining = Mathf.Max(0f,
                _minionSpawnCooldownRemaining - Time.deltaTime);
            _spawnedMinions.RemoveAll(minion => minion == null || !minion.gameObject.activeInHierarchy || minion.IsDead);
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
            if (StateMachine?.CurrentState?.stateName == ApproachDashState)
                _movement?.ConstrainApproachDashVelocity(Time.fixedDeltaTime);
            _movement?.ConstrainAirDashVelocity(Time.fixedDeltaTime);
            _movement?.StopHorizontalAtLedge();
            _movement?.ApplyGravity(Time.fixedDeltaTime);
        }

        public override void Initialize()
        {
            base.Initialize();
            _combat?.ResetCooldowns();
            _movement?.ResetCooldowns();
            DespawnMinions();
            _minionSpawnCooldownRemaining = 0f;
            _phaseThreeMinionSpawned = false;
            InitializeStates();
        }

        public override void Spawn()
        {
            base.Spawn();
            IsDead = false;
            _buffComponent?.Reset();
            _combat?.ResetCooldowns();
            _movement?.ResetCooldowns();
            DespawnMinions();
            _minionSpawnCooldownRemaining = 0f;
            _phaseThreeMinionSpawned = false;
            _movement?.RefreshGrounded();
            _wasGrounded = IsGrounded;
            gameObject.SetActive(true);
            ApplySpawnInvincibility();
            InitializeStates();
            StateMachine.RestartState(IdleState);
        }

        public override void ResetForRoom(Vector3 position)
        {
            base.ResetForRoom(position);
            ApplySpawnInvincibility();
        }

        public override void Despawn()
        {
            DespawnMinions();
            _movement?.StopHorizontal();
            ResetRuntimeState();
            _combat?.ResetCooldowns();
            _movement?.ResetCooldowns();
        }

        public override bool ReceiveHit(BaseEntity attacker)
        {
            if (_combat == null || !_combat.IsVulnerable)
                return false;

            // Boss damage is gated by vulnerability, so the regular hit-invincibility
            // timer must not override an open vulnerability window.
            _buffComponent?.RemoveInvincibility();
            if (!base.ReceiveHit(attacker))
                return false;

            if (IsDead)
            {
                DespawnMinions();
                OnBossDefeated?.Invoke();
                StateMachine?.ChangeState(DeadState);
                return true;
            }

            _combat?.NotifyCountLost(CurrentHealthCount);

            Vector2 direction = attacker != null
                ? (Vector2)(transform.position - attacker.transform.position)
                : Vector2.right;
            if (Mathf.Abs(direction.x) < 0.1f)
                direction.x = Mathf.Sign(_initialVisualScale.x);

            Knockback(hitKnockback, direction);
            StateMachine?.ChangeState(HitState);
            return true;
        }

        public void NotifyPhaseChanged(int phase)
        {
            OnPhaseChanged?.Invoke(phase);
            if (phase == 3 && !_phaseThreeMinionSpawned)
            {
                _phaseThreeMinionSpawned = true;
                TrySpawnMinion(3, true);
            }
        }

        public void SpawnMinionAfterSlam()
        {
            if (CurrentPhase == 1)
                TrySpawnMinion(1);
        }

        public bool TrySpawnMinion(int maxConcurrent, bool ignoreCooldown = false)
        {
            _spawnedMinions.RemoveAll(minion => minion == null || !minion.gameObject.activeInHierarchy || minion.IsDead);
            if (!ignoreCooldown && _minionSpawnCooldownRemaining > 0f ||
                _spawnedMinions.Count >= Mathf.Max(1, maxConcurrent))
                return false;

            if (_poolSystem == null && SystemHandler.Instance != null)
                SystemHandler.Instance.GetSystem(out _poolSystem);

            int randSpawnCount = Random.Range(1, maxConcurrent + 1);
            for (int i = 0; i < randSpawnCount; i++)
            {
                int randEnemyIdx = Random.Range(0, minionPoolName.Length);
                if (_poolSystem == null || string.IsNullOrWhiteSpace(minionPoolName[randEnemyIdx]) ||
                    !_poolSystem.SpawnPool(minionPoolName[randEnemyIdx], out BaseEnemy minion))
                    return false;
                
                Vector2 spawnPosition = minionSpawnPoint != null
                    ? minionSpawnPoint.position
                    : (Vector2)transform.position + minionSpawnOffset;
                spawnPosition.x += Random.Range(-1.5f, 1.5f);
                minion.ResetForRoom(spawnPosition);
                minion.ConfigurePatrol(false, 0f, 0f);
                _spawnedMinions.Add(minion);
            }
            _minionSpawnCooldownRemaining = minionSpawnCooldown;
            return true;
        }

        public void DespawnMinionsForEncounter()
        {
            DespawnMinions();
        }

        private void DespawnMinions()
        {
            foreach (BaseEnemy minion in _spawnedMinions)
            {
                if (minion == null)
                    continue;
                
                EnemyStatData statData = minion.StatData as EnemyStatData;
                if(statData == null)
                    continue;
                
                if (_poolSystem != null && _poolSystem.HasPool(statData.poolName))
                    _poolSystem.DespawnPool(statData.poolName, minion);
                else
                    minion.gameObject.SetActive(false);
            }

            _spawnedMinions.Clear();
        }

        public void ApplyJumpInvincibility()
        {
            _buffComponent?.ApplyInvincibility(jumpInvincibilityDuration);
        }

        private void ApplySpawnInvincibility()
        {
            if (invincibilityDuration > 0f)
                _buffComponent?.ApplyInvincibility(invincibilityDuration);
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
        public bool IsAttacking => StateMachine?.CurrentState?.stateName == BasicAttackState ||
                                   StateMachine?.CurrentState?.stateName == ComboAttackState ||
                                   StateMachine?.CurrentState?.stateName == HeavyAttackState ||
                                   StateMachine?.CurrentState?.stateName == AirComboState ||
                                   StateMachine?.CurrentState?.stateName == AirDashAttackState;

        public float DistanceToTarget()
        {
            return HasLivingTarget()
                ? Vector2.Distance(transform.position, _targetPlayer.transform.position)
                : float.PositiveInfinity;
        }

        public void FaceTarget()
        {
            if (IsAttacking || !HasLivingTarget())
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
                new() { stateName = DeadState, State = new GuardBossDeadState(this) },
                new() { stateName = VulnerableState, State = new GuardBossVulnerableState(this) }
            });
            _statesInitialized = true;
        }
    }
}
