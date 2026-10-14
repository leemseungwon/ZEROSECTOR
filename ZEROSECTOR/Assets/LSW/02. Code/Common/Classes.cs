using System;
using LSW._02._Code.Entity;

namespace LSW._02._Code.Common
{
    [Serializable]
    public class TimedMultiplier
    {
        public string Id;
        public float Multiplier;
        public float RemainingTime;

        public TimedMultiplier(string id, float multiplier, float duration)
        {
            Id = id;
            Multiplier = multiplier;
            RemainingTime = duration;
        }
    }
    
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

    public class DamageOverTime
    {
        public string Id;
        public BaseEntity Source;
        public BaseEntity Target;
        public int Damage;
        public float TickInterval;
        public float RemainingTime;
        public float TickTimer;
    }

    public class ShieldData
    {
        public readonly float ReflectionMultiplier;
        public int RemainingUses;

        public bool ChainReflect;
        public float ChainReflectRadius;

        public bool ReflectHeal;
        public int ReflectHealAmount;

        public ShieldData(
            float reflectionMultiplier,
            int remainingUses)
        {
            ReflectionMultiplier = reflectionMultiplier;
            RemainingUses = remainingUses;
        }
    }
    
    public struct ShieldReflectionData
    {
        public int Damage;
        public BaseEntity Attacker;

        public bool ChainReflect;
        public float ChainReflectRadius;

        public bool ReflectHeal;
        public int ReflectHealAmount;
    }
}