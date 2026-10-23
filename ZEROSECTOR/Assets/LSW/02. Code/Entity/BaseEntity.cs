using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LSW._02._Code.Common;
using LSW._02._Code.Entity.Players.Components;
using LSW._02._Code.Sos.EntityStatDatas;
using UnityEngine;

namespace LSW._02._Code.Entity
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class BaseEntity : MonoBehaviour
    {
        [field: SerializeField] public EntityStatData StatData { get; private set; }
        public EntityAnimation Animation { get; private set; }

        protected Dictionary<Type, IEntityComponent> Components;
        public EntityStateMachine StateMachine { get; set; }
        public EntityBuffComponent BuffComponent { get; set; }
        public Rigidbody2D Rigidbody => _rigidbody;
        public float CurrentMoveSpeed { get; private set; }
        public bool IsDead { get; protected set; }

        private Rigidbody2D _rigidbody;
        private float _baseMoveSpeed;
        private Coroutine _knockbackCoroutine;
        private const float KnockbackDuration = 0.05f;

        public event Action<BaseEntity> OnDeathEvent;
        public event Action<BaseEntity> OnKilledBy;
        public event Action<BaseEntity> OnEnemyKilled;

        protected virtual void Awake()
        {
            Components = GetComponentsInChildren<IEntityComponent>(true)
                .ToDictionary(component => component.GetType());

            InitComponents();
            InitStat();

            if (GetCompo(out EntityAnimation animation))
                Animation = animation;

            _rigidbody = GetComponent<Rigidbody2D>();
            if (GetCompo(out EntityBuffComponent buff))
                BuffComponent = buff;
        }

        protected virtual void InitComponents()
        {
            Components.Values.ToList().ForEach(component => component.Initialize(this));
        }

        public virtual void InitStat()
        {
            ChangeEntityStat(EntityStatType.MoveSpeed, MathOperation.Set, StatData.initMoveSpeed);
            IsDead = false;
        }

        public bool GetCompo<T>(out T component) where T : IEntityComponent
        {
            component = default;
            IEntityComponent found = Components.Values.FirstOrDefault(item => item is T);
            if (found is T typedComponent)
            {
                component = typedComponent;
                return true;
            }

            return false;
        }

        public virtual void ChangeEntityStat(EntityStatType type, MathOperation operation, float value)
        {
            if (type != EntityStatType.MoveSpeed)
                throw new ArgumentOutOfRangeException(nameof(type), type, null);

            _baseMoveSpeed = operation switch
            {
                MathOperation.Add => _baseMoveSpeed + value,
                MathOperation.Subtract => _baseMoveSpeed - value,
                MathOperation.Multiply => _baseMoveSpeed * value,
                MathOperation.Divide => _baseMoveSpeed / value,
                MathOperation.Modulo => _baseMoveSpeed % value,
                MathOperation.Set => value,
                _ => _baseMoveSpeed
            };
            RefreshModifiedStats();
        }

        internal void RefreshModifiedStats()
        {
            float moveSpeedMultiplier = GetCompo(out EntityBuffComponent buff)
                ? buff.GetStatMultiplier(StatModifierType.MoveSpeed)
                : 1f;
            CurrentMoveSpeed = _baseMoveSpeed * moveSpeedMultiplier;
        }

        protected virtual void Update()
        {
            if (StateMachine is { CurrentState: not null })
                StateMachine.Update();
        }

        protected virtual void FixedUpdate()
        {
            if (StateMachine is { CurrentState: not null })
                StateMachine.FixedUpdate();
        }
        
        public virtual void Knockback(float knockBackPower, Vector2 direction)
        {
            if (Rigidbody == null || knockBackPower <= 0f)
                return;

            direction.Normalize();

            if (_knockbackCoroutine != null)
                StopCoroutine(_knockbackCoroutine);

            _knockbackCoroutine = StartCoroutine(KnockbackCoroutine(direction, knockBackPower));
        }

        private IEnumerator KnockbackCoroutine(Vector2 direction, float knockbackPower)
        {
            direction = direction.normalized;

            Vector2 startPosition = Rigidbody.position;

            Vector2 targetPosition =
                startPosition + direction * knockbackPower;

            float elapsed = 0f;

            while (elapsed < KnockbackDuration)
            {
                elapsed += Time.fixedDeltaTime;

                float t = Mathf.Clamp01(elapsed / KnockbackDuration);

                Rigidbody.MovePosition(
                    Vector2.Lerp(startPosition, targetPosition, t)
                );

                yield return new WaitForFixedUpdate();
            }

            Rigidbody.MovePosition(targetPosition);
            Rigidbody.linearVelocity = Vector2.zero;
            
            _knockbackCoroutine = null;
        }

        public virtual bool ReceiveHit(BaseEntity attacker)
        {
            if (IsDead)
                return false;

            if (GetCompo(out EntityBuffComponent buff) && buff.IsInvincible)
                return false;

            OnDeath(attacker);
            return true;
        }

        protected void ResetRuntimeState()
        {
            if (Rigidbody != null)
            {
                Rigidbody.linearVelocity = Vector2.zero;
                Rigidbody.angularVelocity = 0f;
            }
        }

        protected virtual void OnDeath(BaseEntity attacker)
        {
            if (IsDead)
                return;

            IsDead = true;
            OnDeathEvent?.Invoke(this);
            OnKilledBy?.Invoke(attacker);
            attacker?.OnEnemyKilled?.Invoke(this);
        }

        public virtual void ResetForRoom(Vector3 position)
        {
            ResetRuntimeState();
            transform.position = position;
            if (Rigidbody != null)
                Rigidbody.position = position;
            IsDead = false;

            if (GetCompo(out EntityBuffComponent buff))
                buff.Reset();
            if (GetCompo(out PlayerCombatComponent combat))
                combat.ResetForRoom();
        }

        public virtual void OnDestroy()
        {
            StateMachine?.OnDestroy();
        }
    }
}
