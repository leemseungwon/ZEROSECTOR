using System;
using LSW._02._Code.Sos;
using UnityEngine;

namespace LSW._02._Code.Entity.Players
{
    public class PlayerAnimation : EntityAnimation
    {
        private static readonly int AirAttackUpHash = Animator.StringToHash("AIRATTACKUP");
        private bool _isAirAttackActive;

        [Header("Animation Data List")]
        [field:SerializeField] public AnimationData JumpAnimationData { get; private set; }
        [field:SerializeField] public AnimationData FallAnimationData { get; private set; }
        [field:SerializeField] public AnimationData GroundedAnimationData { get; private set; }
        [field: SerializeField] public AnimationData AttackAnimationData { get; private set; }
        [field: SerializeField] public AnimationData JumpAttackAnimationData { get; private set; }
        [field: SerializeField] public AnimationData FallAttackAnimationData { get; private set; }
        [field:SerializeField] public AnimationData DashAnimationData { get; private set; }
        [field:SerializeField] public AnimationData DashAttackAnimationData { get; private set; }
        [field:SerializeField] public AnimationData ParryAnimationData { get; private set; }
        [field:SerializeField] public AnimationData ParryActiveAnimationData { get; private set; }
        [field:SerializeField] public AnimationData ParrySuccessAnimationData { get; private set; }
        [field:SerializeField] public AnimationData ParryFailAnimationData { get; private set; }
        
        public event Action OnAttackCast;
        public event Action OnAttackAnimationEnd;

        public void PlayDash(bool hitEnemy)
        {
            SetParam(hitEnemy ? DashAttackAnimationData : DashAnimationData);
        }

        public void PlayFall()
        {
            if (_isAirAttackActive)
                return;

            SetParam(FallAnimationData);
        }

        public void PlayAirAttack(bool attackingUpward)
        {
            _isAirAttackActive = true;
            Animator.SetBool(AirAttackUpHash, attackingUpward);
            SetParam(attackingUpward ? JumpAttackAnimationData : FallAttackAnimationData);
        }

        public void EndAirAttack(bool rising)
        {
            _isAirAttackActive = false;
            Animator.SetBool(AirAttackUpHash, rising);
        }

        public void PlayJump()
        {
            if (!_isAirAttackActive)
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
    }
}