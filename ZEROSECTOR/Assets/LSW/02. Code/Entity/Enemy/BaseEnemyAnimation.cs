using System;
using LSW._02._Code.Sos;
using UnityEngine;

namespace LSW._02._Code.Entity.Enemy
{
    public class BaseEnemyAnimation : EntityAnimation
    {
        [field:SerializeField] public AnimationData AttackAnimationData { get; private set; }
        [field:SerializeField] public AnimationData DeadAnimationData { get; private set; }

        public event Action OnAttackCast;
        public event Action OnDead;

        public void AttackCast()
        {
            OnAttackCast?.Invoke();
        }

        public void DeadAnimationEnd()
        {
            OnDead?.Invoke();
        }

    }
}