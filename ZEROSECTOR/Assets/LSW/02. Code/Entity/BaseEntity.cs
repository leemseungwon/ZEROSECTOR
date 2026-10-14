using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LSW._02._Code.Common;
using LSW._02._Code.Sos.EntityStatDatas;
using UnityEngine;

namespace LSW._02._Code.Entity
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class BaseEntity : MonoBehaviour, IDamageable
    {
        [field:SerializeField] public EntityStatData StatData { get; private set; }
        public EntityAnimation Animation { get; private set; }
        
        protected Dictionary<Type, IEntityComponent> Components;
        
        public EntityStateMachine StateMachine { get; set; }

        public Rigidbody2D Rigidbody => _rigidbody;
        
        public int CurrentHp { get; protected set; }
        public int MaxHp { get; set; }
        public float CurrentMoveSpeed { get; private set; }
        public int CurrentAttackDamage { get; private set; }
        
        public bool IsStunned { get; private set; }
        public bool IsDead { get; protected set; }
        
        private Rigidbody2D _rigidbody;
        private float _baseMoveSpeed;
        private int _baseAttackDamage;
        private Coroutine _knockbackCoroutine;
        private Coroutine _stunCoroutine;
        private const float KnockbackDuration = 0.2f;
        
        public event Action<int, int> OnHealthChanged;
        public event Action OnDeathEvent;
        
        protected virtual void Awake()
        {
            Components = GetComponentsInChildren<IEntityComponent>(true)
                .ToDictionary(compo => compo.GetType());
            
            InitComponents();
            InitStat();

            if (GetCompo(out EntityAnimation anima))
            {
                Animation = anima;
            }

            _rigidbody = GetComponent<Rigidbody2D>();
        }
        
        protected virtual void InitComponents()
        {
            Components.Values.ToList().ForEach(component => component.Initialize(this));
        }
        
        public virtual void InitStat()
        {
            MaxHp = (int)StatData.initMaxHp;
            ChangeEntityStat(EntityStatType.AttackDamage, MathOperation.Set, StatData.initAttackDamage);
            ChangeEntityStat(EntityStatType.MoveSpeed, MathOperation.Set, StatData.initMoveSpeed);

            CurrentHp = MaxHp;
            IsDead = false;
        }
        
        public bool GetCompo<T>(out T component) where T : IEntityComponent
        {
            component = default;
            IEntityComponent findComponent = Components.Values.FirstOrDefault(c => c is T);
            if (findComponent is T findCompo)
            {
                component = findCompo;
                return true;
            }
            return false;
        }

        public virtual void ChangeEntityStat(EntityStatType type, MathOperation operation, float value)
        {
            switch (type)
            {
                case EntityStatType.MoveSpeed:
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
                    break;
                case EntityStatType.MaxHp:
                    int previousMaxHp = MaxHp;
                    MaxHp = operation switch
                    {
                        MathOperation.Add => (int)(MaxHp + value),
                        MathOperation.Subtract => (int)(MaxHp - value),
                        MathOperation.Multiply => (int)(MaxHp * value),
                        MathOperation.Divide => (int)(MaxHp / value),
                        MathOperation.Modulo => (int)(MaxHp % value),
                        MathOperation.Set => (int)value,
                        _ => MaxHp
                    };
                    if (MaxHp != previousMaxHp)
                    {
                        int healthIncrease = MaxHp - previousMaxHp;
                        if (healthIncrease > 0)
                            CurrentHp += healthIncrease;
                        CurrentHp = Mathf.Clamp(CurrentHp, 0, MaxHp);
                        OnHealthChanged?.Invoke(CurrentHp, MaxHp);
                    }
                    break;
                case EntityStatType.AttackDamage:
                    _baseAttackDamage = operation switch
                    {
                        MathOperation.Add => (int)(_baseAttackDamage + value),
                        MathOperation.Subtract => (int)(_baseAttackDamage - value),
                        MathOperation.Multiply => (int)(_baseAttackDamage * value),
                        MathOperation.Divide => (int)(_baseAttackDamage / value),
                        MathOperation.Modulo => (int)(_baseAttackDamage % value),
                        MathOperation.Set => (int)value,
                        _ => _baseAttackDamage
                    };
                    RefreshModifiedStats();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        internal void RefreshModifiedStats()
        {
            float moveSpeedMultiplier = 1f;
            float attackDamageMultiplier = 1f;

            if (GetCompo(out EntityBuffComponent buff))
            {
                moveSpeedMultiplier = buff.GetStatMultiplier(StatModifierType.MoveSpeed);
                attackDamageMultiplier = buff.GetStatMultiplier(StatModifierType.AttackDamage);
            }

            CurrentMoveSpeed = _baseMoveSpeed * moveSpeedMultiplier;
            CurrentAttackDamage = Mathf.Max(0,
                Mathf.RoundToInt(_baseAttackDamage * attackDamageMultiplier));
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

        public virtual void Heal(int amount)
        {
            if (amount <= 0)
                return;

            int previousHp = CurrentHp;
            CurrentHp = Mathf.Min(CurrentHp + amount, MaxHp);
            
            if (CurrentHp != previousHp)
                OnHealthChanged?.Invoke(CurrentHp, MaxHp);
        }
        
        public virtual bool TakeDamage(int finalDamage, Vector2 knockBackDirection, float knockBackPower, float stunDuration, 
            BaseEntity attacker)
        {
            if (CurrentHp <= 0)
                return false;

            if (GetCompo(out EntityBuffComponent buff))
            {
                finalDamage = buff.ModifyIncomingDamage(finalDamage);

                if (buff.TryConsumeShield(finalDamage, attacker))
                {
                    return true;
                }
            }

            if (finalDamage <= 0)
                return false;

            CurrentHp -= finalDamage;

            SetStun(stunDuration);
            Knockback(knockBackPower, knockBackDirection);

            OnHealthChanged?.Invoke(CurrentHp, MaxHp);
            return true;
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
        
        public void SetStun(float duration)
        {
            if (duration <= 0f || !isActiveAndEnabled || !gameObject.activeInHierarchy)
                return;

            if(_stunCoroutine != null)
                StopCoroutine(_stunCoroutine);
            
            if(Animation != null)
                Animation.SetParam(Animation.IsMoveAnimationData, false);
            _stunCoroutine = StartCoroutine(StunCoroutine(duration));
        }

        private IEnumerator StunCoroutine(float duration)
        {
            IsStunned = true;

            yield return new WaitForSeconds(duration);
            
            IsStunned = false;
            _stunCoroutine = null;
            
            if (TryGetComponent(out SpriteRenderer renderer))
            {
                renderer.color = Color.white;
            }
        }
        
        protected void ResetRuntimeState()
        {
            if (_stunCoroutine != null)
            {
                StopCoroutine(_stunCoroutine);
                _stunCoroutine = null;
            }

            if (_knockbackCoroutine != null)
            {
                StopCoroutine(_knockbackCoroutine);
                _knockbackCoroutine = null;
            }

            IsStunned = false;

            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector2.zero;
                _rigidbody.angularVelocity = 0f;
            }
        }
        
        protected virtual void OnDeath()
        {
            IsDead = true;
            OnDeathEvent?.Invoke();
            OnDeathEvent = null;
        }

        public virtual void OnDestroy()
        {
            if (StateMachine != null)
            {
                StateMachine.OnDestroy();
            }
            
            if(_stunCoroutine != null)
                StopCoroutine(_stunCoroutine);
            
            if(_knockbackCoroutine != null)
                StopCoroutine(_knockbackCoroutine);
        }
    }
}
