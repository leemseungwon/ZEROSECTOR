using System;

namespace LSW._02._Code.Common
{
    [Serializable]
    public enum SceneType
    {
        Title
    }
    
    [Serializable]
    public enum MathOperation
    {
        Set, Add, Subtract, Multiply, Divide, Modulo
    }
    
    [Serializable]
    public enum EntityStatType
    {
        MoveSpeed,
        MaxHp,
        AttackDamage,
        JumpForce
    }
    
    public enum StatModifierType
    {
        MoveSpeed,
        AttackDamage
    }
    
    public enum WeaponType
    {
        Katana,
        Gun
    }
}
