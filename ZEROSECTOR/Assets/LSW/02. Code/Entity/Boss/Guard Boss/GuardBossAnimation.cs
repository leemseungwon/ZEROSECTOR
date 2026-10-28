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

        [Header("Monochrome Encounter")]
        [SerializeField, Range(0f, 1f)] private float normalGray = 0.65f;
        [SerializeField, Range(0f, 1f)] private float vulnerableGray = 1f;
        [SerializeField, Min(1f)] private float finalPhaseSpeed = 1.25f;

        private SpriteRenderer _spriteRenderer;
        private Animator _animator;
        private float _baseAnimatorSpeed = 1f;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _animator = GetComponent<Animator>();
            if (_animator != null)
                _baseAnimatorSpeed = _animator.speed;
            SetVulnerable(false);
        }

        public event Action OnAttackCast;
        public event Action OnAttackAnimationEnd;
        public event Action OnDead;

        public void SetMoving(bool moving)
        {
            if (IsMoveAnimationData != null)
                SetParam(IsMoveAnimationData, moving);
        }

        public void PlayIdle()
        {
            SetMoving(false);
            if (Animator != null && Animator.HasState(0, Animator.StringToHash("Guard Boss Idle")))
                Animator.Play("Guard Boss Idle", 0, 0f);
        }

        public void PlayJumpPrepare() => SetParam(jumpPrepareAnimation);
        public void PlayJump() => SetParam(jumpAnimation);
        public void PlayFall()
        {
            if (Animator != null)
                Animator.SetTrigger("FALL");
        }
        public void PlayLanding()
        {
            // There is no separate landing state in this controller; landingAnimation
            // currently points at the FALL trigger, which can remain queued if it is
            // fired while already in the fall state. Clear it instead of restarting it.
            if (Animator == null)
                return;

            if (landingAnimation != null && landingAnimation.animationName != "FALL")
                SetParam(landingAnimation);
            else
                Animator.ResetTrigger("FALL");
        }
        public void PlayBasicAttack() => SetParam(basicAttackAnimation);
        public void PlayComboAttack() => SetParam(comboAttackAnimation);
        public void PlayHeavyCharge() => SetParam(heavyChargeAnimation);
        public void PlayAirCombo() => SetParam(airComboAnimation);
        public void PlayDash() => SetParam(dashAnimation);
        public void PlayHit() => SetParam(hitAnimation);

        public void SetVulnerable(bool vulnerable)
        {
            if (_spriteRenderer == null)
                return;

            float gray = vulnerable ? vulnerableGray : normalGray;
            _spriteRenderer.color = new Color(gray, gray, gray, _spriteRenderer.color.a);
        }

        public void SetPhase(int phase)
        {
            if (_animator != null)
                _animator.speed = _baseAnimatorSpeed * (phase >= 3 ? finalPhaseSpeed : 1f);
        }

        public void PlayDead()
        {
            if (Animator != null && Animator.HasState(0, Animator.StringToHash("Guard Boss Dead")))
                Animator.Play("Guard Boss Dead", 0, 0f);
        }

        // Hook these up as animation events when the controller uses event-timed impacts.
        public void AttackCast() => OnAttackCast?.Invoke();
        public void AttackAnimationEnd() => OnAttackAnimationEnd?.Invoke();
        public void DeadAnimationEnd() => OnDead?.Invoke();
    }
}
