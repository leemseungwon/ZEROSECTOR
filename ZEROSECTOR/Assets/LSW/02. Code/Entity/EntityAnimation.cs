using System.Collections.Generic;
using LSW._02._Code.Sos;
using UnityEngine;

namespace LSW._02._Code.Entity
{
    [RequireComponent(typeof(Animator))]
    public class EntityAnimation : MonoBehaviour, IEntityComponent
    {
        [field:SerializeField] public AnimationData IsMoveAnimationData { get; private set; }
        
        private readonly HashSet<int> _validParamHashes = new HashSet<int>();
        
        public Animator Animator { get; private set; }
        
        public void Initialize(BaseEntity owner)
        {
            Animator = GetComponent<Animator>();
            CacheAnimatorParameters();
        }

        public void Reset() { }

        public void CacheAnimatorParameters()
        {
            Animator = GetComponent<Animator>();

            if (Animator == null)
            {
                return;
            }

            _validParamHashes.Clear();

            foreach (AnimatorControllerParameter param in Animator.parameters)
            {
                _validParamHashes.Add(param.nameHash);
            }
        }
        
        public void PlayAnimation(AnimationData data)
        {
            if(!CheckValidAnimData(data))
                return;
            
            Animator.StopPlayback();
            Animator.Play(data.animationName);
        }
        
        public void PlayClip(AnimationData data)
        {
            if (!CheckValidAnimData(data))
                return;

            Animator.Play(data.animationHash, 0, 0f);
        }

        public void SetParam(AnimationData data, float value, float lerpTime = 0f)
        {
            if(!CheckValidAnimData(data))
                return;
            
            Animator.SetFloat(Animator.StringToHash(data.animationName), value, lerpTime, Time.deltaTime);
        }

        public void SetParam(AnimationData data, bool value)
        {
            if(!CheckValidAnimData(data))
                return;
            Animator.SetBool(Animator.StringToHash(data.animationName), value);
        }

        public void SetParam(AnimationData data, int value)
        {
            if(!CheckValidAnimData(data))
                return;
            Animator.SetInteger(Animator.StringToHash(data.animationName), value);
        }
        
        public void SetParam(AnimationData data)
        {
            if(!CheckValidAnimData(data))
                return;
            Animator.SetTrigger(Animator.StringToHash(data.animationName));
        }
        
        private bool CheckValidAnimData(AnimationData data)
        {
            if(!data)
                return false;

            bool isValid = _validParamHashes.Contains(Animator.StringToHash(data.animationName));
            
            if(!isValid)
                Debug.LogError($"AnimationData is not valid. {data.animationName}");
            
            return isValid;
        }
    }
}