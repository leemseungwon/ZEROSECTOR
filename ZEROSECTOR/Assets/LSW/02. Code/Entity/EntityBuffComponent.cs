using System.Collections;
using System.Collections.Generic;
using LSW._02._Code.Common;
using UnityEngine;

namespace LSW._02._Code.Entity
{
    public class EntityBuffComponent : MonoBehaviour, IEntityComponent
    {
        private BaseEntity _owner;
        private readonly List<StatModifier> _moveSpeedModifiers = new();
        private float _invincibilityRemainingTime;

        public bool IsInvincible => _invincibilityRemainingTime > 0f;
        public float RemainingInvincibilityTime => _invincibilityRemainingTime;

        public void Initialize(BaseEntity owner)
        {
            _owner = owner;
        }

        public void ApplyInvincibility(float duration)
        {
            if (_owner == null || _owner.IsDead || duration <= 0f)
                return;

            _invincibilityRemainingTime = Mathf.Max(_invincibilityRemainingTime, duration);
        }

        public void ApplyInvincibility(string id, float duration)
        {
            ApplyInvincibility(duration);
        }

        public void RemoveInvincibility()
        {
            _invincibilityRemainingTime = 0f;
        }

        public void ApplyMoveSpeedMultiplier(string id, float multiplier, float duration)
        {
            if (_owner == null || multiplier <= 0f || duration <= 0f)
                return;

            RemoveMoveSpeedModifier(id);
            _moveSpeedModifiers.Add(new StatModifier(
                id, StatModifierType.MoveSpeed, multiplier, duration));
            _owner.RefreshModifiedStats();
        }

        internal float GetStatMultiplier(StatModifierType type)
        {
            if (type != StatModifierType.MoveSpeed)
                return 1f;

            float multiplier = 1f;
            foreach (StatModifier modifier in _moveSpeedModifiers)
                multiplier *= modifier.Multiplier;

            return multiplier;
        }

        public void ApplyConfusionAfterDelay(float delay, float duration, float radius,
            float attackInterval, LayerMask enemyLayer)
        {
            if (delay < 0f || duration <= 0f || radius <= 0f || attackInterval <= 0f)
                return;

            StartCoroutine(ConfusionCoroutine(delay, duration, radius, attackInterval, enemyLayer));
        }

        private IEnumerator ConfusionCoroutine(float delay, float duration, float radius,
            float attackInterval, LayerMask enemyLayer)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (_owner == null || _owner.IsDead)
                    yield break;

                BaseEntity target = FindNearestEnemy(_owner.transform.position, radius, enemyLayer);
                if (target != null)
                    Tools.AttackTarget(_owner, target);

                yield return new WaitForSeconds(attackInterval);
                elapsed += attackInterval;
            }
        }

        private BaseEntity FindNearestEnemy(Vector2 position, float radius, LayerMask layer)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(position, radius, layer);
            BaseEntity closest = null;
            float closestDistance = float.MaxValue;

            foreach (Collider2D hit in hits)
            {
                BaseEntity entity = hit.GetComponentInParent<BaseEntity>();
                if (entity == null || entity == _owner || entity.IsDead)
                    continue;

                float distance = Vector2.SqrMagnitude((Vector2)entity.transform.position - position);
                if (distance >= closestDistance)
                    continue;

                closestDistance = distance;
                closest = entity;
            }

            return closest;
        }

        private void RemoveMoveSpeedModifier(string id)
        {
            for (int i = _moveSpeedModifiers.Count - 1; i >= 0; i--)
            {
                if (_moveSpeedModifiers[i].Id == id)
                    _moveSpeedModifiers.RemoveAt(i);
            }
        }

        private void Update()
        {
            if (_invincibilityRemainingTime > 0f)
            {
                _invincibilityRemainingTime -= Time.deltaTime;
                if (_invincibilityRemainingTime < 0f)
                    _invincibilityRemainingTime = 0f;
            }

            bool changed = false;
            for (int i = _moveSpeedModifiers.Count - 1; i >= 0; i--)
            {
                _moveSpeedModifiers[i].RemainingTime -= Time.deltaTime;
                if (_moveSpeedModifiers[i].RemainingTime > 0f)
                    continue;

                _moveSpeedModifiers.RemoveAt(i);
                changed = true;
            }

            if (changed)
                _owner?.RefreshModifiedStats();
        }

        public void Reset()
        {
            StopAllCoroutines();
            _moveSpeedModifiers.Clear();
            _invincibilityRemainingTime = 0f;
            _owner?.RefreshModifiedStats();
        }
    }
}
