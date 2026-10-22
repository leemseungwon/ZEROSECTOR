using UnityEngine;

namespace LSW._02._Code.Sos
{
    [CreateAssetMenu(fileName = "Animation Hash", menuName = "So/Data/Animation/Animation Data", order = 0)]
    public class AnimationData : ScriptableObject
    {
        public string animationName;
        public int animationHash;
        
        private void OnValidate()
        {
            animationHash = Animator.StringToHash(animationName);
        }
    }
}