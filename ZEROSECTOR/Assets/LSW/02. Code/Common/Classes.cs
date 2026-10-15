using System;
namespace LSW._02._Code.Common
{
    [Serializable]
    public class StatModifier
    {
        public string Id;
        public StatModifierType Type;
        public float Multiplier;
        public float RemainingTime;

        public StatModifier(string id, 
            StatModifierType type, float multiplier, float duration)
        {
            Id = id;
            Type = type;
            Multiplier = multiplier;
            RemainingTime = duration;
        }
    }
}