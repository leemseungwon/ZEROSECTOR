using System;
using LSW._02._Code.Sos;
using UnityEngine;

namespace LSW._02._Code.Entity.Boss.Guard_Boss
{
    public class GuardBossAnimation : EntityAnimation
    {
        [Header("Locomotion")]
        [SerializeField] private AnimationData jumpPrepareAnimation;
        [SerializeField] private AnimationData jumpAnimation;
        [SerializeField] private AnimationData landingAnimation;

        [Header("Attacks")]
        [SerializeField] private AnimationData basicAttackAnimation;
        [SerializeField] private AnimationData comboAttackAnimation;
        [SerializeField] private AnimationData heavyChargeAnimation;
        [SerializeField] private AnimationData airComboAnimation;
        [SerializeField] private AnimationData dashAnimation;

        [Header("Reactions")]
        [SerializeField] private AnimationData hitAnimation;

        public event Action OnAttackCast;
        public event Action OnDead;

        public void SetMoving(bool moving)
        {
            if (IsMoveAnimationData != null)
                SetParam(IsMoveAnimationData, moving);
        }

        public void PlayJumpPrepare() => SetParam(jumpPrepareAnimation);
        public void PlayJump() => SetParam(jumpAnimation);
        public void PlayLanding() => SetParam(landingAnimation);
        public void PlayBasicAttack() => SetParam(basicAttackAnimation);
        public void PlayComboAttack() => SetParam(comboAttackAnimation);
        public void PlayHeavyCharge() => SetParam(heavyChargeAnimation);
        public void PlayAirCombo() => SetParam(airComboAnimation);
        public void PlayDash() => SetParam(dashAnimation);
        public void PlayHit() => SetParam(hitAnimation);
        public void PlayDead()
        {
            if (Animator != null && Animator.HasState(0, Animator.StringToHash("Guard Boss Dead")))
                Animator.Play("Guard Boss Dead", 0, 0f);
        }

        // Hook these up as animation events when the controller uses event-timed impacts.
        public void AttackCast() => OnAttackCast?.Invoke();
        public void DeadAnimationEnd() => OnDead?.Invoke();
    }
}
