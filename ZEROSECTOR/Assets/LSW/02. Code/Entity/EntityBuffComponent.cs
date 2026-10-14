
using System.Collections;
using System.Collections.Generic;
using LSW._02._Code.Common;
using UnityEngine;

namespace LSW._02._Code.Entity
{
    public class EntityBuffComponent : MonoBehaviour, IEntityComponent
    {
        private BaseEntity _owner;

        private readonly List<TimedMultiplier> _incomingDamageModifiers = new();
        private readonly List<TimedMultiplier> _outgoingDamageModifiers = new();
        private readonly List<TimedMultiplier> _attackCooldownModifiers = new();

        private readonly List<StatModifier> _statModifiers = new();
        private readonly List<DamageOverTime> _damageOverTimes = new();
        
        public bool IsHealthBarRevealed { get; private set; }
        public bool HasShield => _shield != null && _shield.RemainingUses > 0;
        
        private ShieldData _shield;
        
        public void Initialize(BaseEntity owner)
        {
            if (_owner != null)
                return;

            _owner = owner;

            if (_owner == null)
                _owner = GetComponent<BaseEntity>();
        }

        private void Awake()
        {
            if (_owner == null)
            {
                BaseEntity owner = GetComponent<BaseEntity>();

                if (owner != null)
                {
                    Initialize(owner);
                }
            }
        }

        private void Update()
        {
            UpdateTimedMultipliers(_incomingDamageModifiers);
            UpdateTimedMultipliers(_outgoingDamageModifiers);
            UpdateTimedMultipliers(_attackCooldownModifiers);

            UpdateStatModifiers();
            UpdateDamageOverTime();
        }

        #region Damage Modifier

        public int ModifyIncomingDamage(int damage)
        {
            if (damage <= 0)
                return 0;

            float multiplier = GetMultiplier(_incomingDamageModifiers);

            return Mathf.Max(0, Mathf.RoundToInt(damage * multiplier));
        }

        public int ModifyOutgoingDamage(int damage)
        {
            if (damage <= 0)
                return 0;

            float multiplier = GetMultiplier(_outgoingDamageModifiers);

            return Mathf.Max(0, Mathf.RoundToInt(damage * multiplier));
        }

        public float GetAttackCooldownMultiplier()
        {
            return GetMultiplier(_attackCooldownModifiers);
        }

        public void ApplyIncomingDamageMultiplier(string id,
            float multiplier, float duration)
        {
            AddOrReplaceMultiplier(_incomingDamageModifiers, id,
                multiplier, duration);
        }

        public void ApplyOutgoingDamageMultiplier(string id, float multiplier, 
            float duration)
        {
            AddOrReplaceMultiplier(_outgoingDamageModifiers, id,
                multiplier, duration);
        }

        public void ApplyAttackCooldownMultiplier(string id, float multiplier, 
            float duration)
        {
            AddOrReplaceMultiplier(_attackCooldownModifiers, id,
                multiplier, duration);
        }

        #endregion

        #region Stat Modifier

        public void ApplyMoveSpeedMultiplier(string id, float multiplier, float duration)
        {
            ApplyStatModifier(id, StatModifierType.MoveSpeed, multiplier,
                duration);
        }

        public void ApplyAttackDamageMultiplier(string id, float multiplier,
            float duration)
        {
            ApplyStatModifier(id, StatModifierType.AttackDamage, 
                multiplier, duration);
        }

        private void ApplyStatModifier(string id,
            StatModifierType type, float multiplier, float duration)
        {
            if (_owner == null)
                return;

            if (multiplier <= 0f || duration <= 0f)
                return;

            RemoveStatModifier(id);

            StatModifier modifier = new StatModifier(id, type, multiplier,
                duration);

            _statModifiers.Add(modifier);
            _owner.RefreshModifiedStats();
        }

        private void RemoveStatModifier(string id)
        {
            for (int i = _statModifiers.Count - 1; i >= 0; i--)
            {
                if (_statModifiers[i].Id != id)
                    continue;

                _statModifiers.RemoveAt(i);
            }

            _owner.RefreshModifiedStats();
        }

        internal float GetStatMultiplier(StatModifierType type)
        {
            float result = 1f;
            foreach (StatModifier modifier in _statModifiers)
            {
                if (modifier.Type == type)
                    result *= modifier.Multiplier;
            }

            return result;
        }

        private void UpdateStatModifiers()
        {
            for (int i = _statModifiers.Count - 1; i >= 0; i--)
            {
                StatModifier modifier = _statModifiers[i];

                modifier.RemainingTime -= Time.deltaTime;

                if (modifier.RemainingTime > 0f)
                    continue;

                _statModifiers.RemoveAt(i);
            }

            _owner.RefreshModifiedStats();
        }

        #endregion

        #region Action Disable

        public void ApplyActionDisable(float duration)
        {
            if (_owner == null)
                return;

            if (duration <= 0f)
                return;
            
            _owner.SetStun(duration);

            if (_owner.TryGetComponent(out SpriteRenderer renderer))
            {
                renderer.color = Color.gray;
            }
        }

        #endregion

        #region Damage Over Time

        public void ApplyDamageOverTime(string id, BaseEntity source, int damage, 
            float tickInterval, float duration)
        {
            if (_owner == null)
                return;

            if (damage <= 0 || tickInterval <= 0f || duration <= 0f)
                return;

            RemoveDamageOverTime(id);

            _damageOverTimes.Add(new DamageOverTime
                {
                    Id = id,
                    Source = source,
                    Target = _owner,
                    Damage = damage,
                    TickInterval = tickInterval,
                    RemainingTime = duration,
                    TickTimer = tickInterval
                }
            );
        }

        private void RemoveDamageOverTime(string id)
        {
            for (int i = _damageOverTimes.Count - 1; i >= 0; i--)
            {
                if (_damageOverTimes[i].Id == id)
                    _damageOverTimes.RemoveAt(i);
            }
        }

        private void UpdateDamageOverTime()
        {
            for (int i = _damageOverTimes.Count - 1; i >= 0; i--)
            {
                DamageOverTime dot = _damageOverTimes[i];

                if (dot.Target == null || dot.Target.IsDead)
                {
                    _damageOverTimes.RemoveAt(i);
                    continue;
                }

                dot.RemainingTime -= Time.deltaTime;
                dot.TickTimer -= Time.deltaTime;

                while (dot.TickTimer <= 0f && dot.RemainingTime > 0f)
                {
                    dot.TickTimer += dot.TickInterval;

                    Vector2 direction = Vector2.zero;

                    if (dot.Source != null)
                    {
                        direction =
                            (dot.Target.transform.position -
                             dot.Source.transform.position).normalized;
                    }

                    dot.Target.TakeDamage(
                        dot.Damage,
                        direction,
                        0f,
                        0f,
                        dot.Source
                    );
                }

                if (dot.RemainingTime <= 0f)
                {
                    _damageOverTimes.RemoveAt(i);
                }
            }
        }

        #endregion

        #region Damage Field

        public void CreateDamageField(Vector2 center, float radius, float duration, float tickInterval,
            int damage, LayerMask targetLayer)
        {
            if (duration <= 0f || radius <= 0f || tickInterval <= 0f || damage <= 0)
                return;

            StartCoroutine(DamageFieldCoroutine(
                    center, radius, duration, tickInterval, damage, targetLayer
                    )
            );
        }

        private IEnumerator DamageFieldCoroutine(Vector2 center, float radius, 
            float duration, float tickInterval, int damage, LayerMask targetLayer)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius,
                        targetLayer);

                HashSet<BaseEntity> damagedEntities = new();

                foreach (Collider2D hit in hits)
                {
                    BaseEntity entity = hit.GetComponentInParent<BaseEntity>();

                    if (entity == null || entity == _owner || entity.IsDead ||
                        !damagedEntities.Add(entity))
                    {
                        continue;
                    }

                    Vector2 direction = (entity.transform.position -
                                         _owner.transform.position).normalized;

                    entity.TakeDamage(damage, direction,
                        0f, 0f, _owner);
                }

                yield return new WaitForSeconds(tickInterval);

                elapsed += tickInterval;
            }
        }

        #endregion

        #region Confusion

        public void ApplyConfusionAfterDelay(float delay, float duration, float radius,
            float attackInterval, LayerMask enemyLayer)
        {
            StartCoroutine(ConfusionCoroutine(delay, 
                    duration, radius, attackInterval, enemyLayer));
        }

        private IEnumerator ConfusionCoroutine(float delay, float duration,
            float radius, float attackInterval, LayerMask enemyLayer)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (_owner == null || _owner.IsDead)
                    yield break;

                BaseEntity target = FindNearestEnemy(
                        _owner.transform.position, radius, enemyLayer);

                if (target != null)
                {
                    int damage = Mathf.Max(1, _owner.CurrentAttackDamage);

                    Vector2 direction = (target.transform.position -
                                         _owner.transform.position).normalized;

                    target.TakeDamage(damage,
                        direction, 0f, 0f, _owner);
                }

                yield return new WaitForSeconds(attackInterval);

                elapsed += attackInterval;
            }
        }

        private BaseEntity FindNearestEnemy(Vector2 position,
            float radius, LayerMask layer)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(
                    position, radius, layer);

            BaseEntity closest = null;
            float closestDistance = float.MaxValue;

            foreach (Collider2D hit in hits)
            {
                BaseEntity entity = hit.GetComponentInParent<BaseEntity>();

                if (entity == null || entity == _owner || entity.IsDead)
                    continue;

                float distance = Vector2.SqrMagnitude(
                        (Vector2)entity.transform.position - position);

                if (distance >= closestDistance)
                    continue;

                closestDistance = distance;
                closest = entity;
            }

            return closest;
        }

        #endregion
        
        #region Shield
        
        public void SetShield(float reflectionMultiplier, int remainingUses = 1)
        {
            if (_owner == null)
                return;

            if (remainingUses <= 0)
                return;

            _shield = new ShieldData(
                reflectionMultiplier, remainingUses);
        }
        
        public void EnhanceShield(bool chainReflect = false, float chainReflectRadius = 0f,
            bool reflectHeal = false, int reflectHealAmount = 0)
        {
            if (_shield == null)
                return;

            if (chainReflect)
            {
                _shield.ChainReflect = true;
                _shield.ChainReflectRadius = Mathf.Max(
                    _shield.ChainReflectRadius, chainReflectRadius);
            }

            if (reflectHeal)
            {
                _shield.ReflectHeal = true;
                _shield.ReflectHealAmount = Mathf.Max(
                    _shield.ReflectHealAmount, reflectHealAmount);
            }
        }
        
        public bool TryConsumeShield(int incomingDamage, BaseEntity attacker)
        {
            if (_owner == null)
                return false;

            if (_shield == null)
                return false;

            if (_shield.RemainingUses <= 0)
            {
                _shield = null;
                return false;
            }

            if (incomingDamage <= 0)
                return false;
            
            _shield.RemainingUses--;

            ShieldReflectionData reflectionData = new ShieldReflectionData
            {
                Damage = Mathf.RoundToInt(
                    incomingDamage * _shield.ReflectionMultiplier),

                Attacker = attacker,

                ChainReflect = _shield.ChainReflect,
                ChainReflectRadius = _shield.ChainReflectRadius,

                ReflectHeal = _shield.ReflectHeal,
                ReflectHealAmount = _shield.ReflectHealAmount
            };
            
            if (reflectionData.ReflectHeal)
            {
                _owner.Heal(reflectionData.ReflectHealAmount);
            }
            if (_shield.RemainingUses <= 0)
            {
                _shield = null;
            }
            RemoveShield();

            return true;
        }
        
        public void RemoveShield()
        {
            _shield = null;
        }

        #endregion

        #region Health Bar

        public void RevealHealthBar()
        {
            IsHealthBarRevealed = true;
        }

        #endregion

        #region Utility

        private void AddOrReplaceMultiplier(List<TimedMultiplier> list,
            string id, float multiplier, float duration)
        {
            if (multiplier <= 0f || duration <= 0f)
                return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].Id == id)
                    list.RemoveAt(i);
            }

            list.Add(new TimedMultiplier(
                        id,
                        multiplier,
                        duration)
                    );
        }

        private float GetMultiplier(List<TimedMultiplier> list)
        {
            float result = 1f;

            foreach (TimedMultiplier modifier in list)
            {
                result *= modifier.Multiplier;
            }

            return result;
        }

        private void UpdateTimedMultipliers(List<TimedMultiplier> list)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                list[i].RemainingTime -= Time.deltaTime;

                if (list[i].RemainingTime <= 0f)
                    list.RemoveAt(i);
            }
        }

        #endregion

        public void Reset()
        {
            StopAllCoroutines();

            _statModifiers.Clear();
            if (_owner != null)
                _owner.RefreshModifiedStats();
            _incomingDamageModifiers.Clear();
            _outgoingDamageModifiers.Clear();
            _attackCooldownModifiers.Clear();
            _damageOverTimes.Clear();

            _shield = null;
            IsHealthBarRevealed = false;
        }
    }
}
