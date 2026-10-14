using System;
using LSW._02._Code.Sos;
using UnityEngine;

namespace LSW._02._Code.Entity.Players
{
    public class PlayerAnimation : EntityAnimation
    {
        [Header("Animation Data List")]
        [field:SerializeField] public AnimationData JumpAnimationData { get; private set; }
        
        [field: SerializeField] public AnimationData AttackAnimationData { get; private set; }
        
        public event Action OnAttackCast;
        public event Action OnAttackAnimationEnd;
        
        public void AttackCast()
        {    
            OnAttackCast?.Invoke();
        }

        public void AttackAnimationEnd()
        {
            OnAttackAnimationEnd?.Invoke();
        }
    }
}