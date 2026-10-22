using System;
using LSW._02._Code.Sos;
using UnityEngine;

namespace LSW._02._Code.Entity.Players
{
    public class PlayerAnimation : EntityAnimation
    {
        [Header("Animation Data List")]
        [field:SerializeField] public AnimationData JumpAnimationData { get; private set; }
        [field:SerializeField] public AnimationData FallAnimationData { get; private set; }
        [field:SerializeField] public AnimationData GroundedAnimationData { get; private set; }
        [field: SerializeField] public AnimationData AttackAnimationData { get; private set; }
        [field:SerializeField] public AnimationData DashAnimationData { get; private set; }
        [field:SerializeField] public AnimationData DashAttackAnimationData { get; private set; }
        [field:SerializeField] public AnimationData ParryAnimationData { get; private set; }
        [field:SerializeField] public AnimationData ParryActiveAnimationData { get; private set; }
        [field:SerializeField] public AnimationData ParrySuccessAnimationData { get; private set; }
        [field:SerializeField] public AnimationData ParryFailAnimationData { get; private set; }
        [field:SerializeField] public AnimationData ReadyGunAnimationData { get; private set; }
        [field:SerializeField] public AnimationData ShootGunAnimationData { get; private set; }
        
        public event Action OnAttackCast;
        public event Action OnAttackAnimationEnd;
        public event Action OnGunCast;
        public event Action OnGunAnimationEnd;

        public void PlayDash(bool hitEnemy)
        {
            SetParam(hitEnemy ? DashAttackAnimationData : DashAnimationData);
        }

        public void PlayFall()
        {
            SetParam(FallAnimationData);
        }

        public void PlayJump()
        {
            SetParam(JumpAnimationData);
        }

        public void SetGrounded(bool grounded)
        {
            SetParam(GroundedAnimationData, grounded);
        }

        public void PlayParry(bool isParrying)
        {
            if (isParrying)
                SetParam(ParryAnimationData);

            SetParam(ParryActiveAnimationData, isParrying);
        }

        public void PlayParryFail()
        {
            SetParam(ParryFailAnimationData);
        }
        
        public void AttackCast()
        {    
            OnAttackCast?.Invoke();
        }

        public void AttackAnimationEnd()
        {
            OnAttackAnimationEnd?.Invoke();
        }

        public void GunCast()
        {
            OnGunCast?.Invoke();
        }

        public void GunAnimationEnd()
        {
            OnGunAnimationEnd?.Invoke();
        }
    }
}