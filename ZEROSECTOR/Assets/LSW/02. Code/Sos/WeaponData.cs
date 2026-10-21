using LSW._02._Code.Common;
using UnityEngine;

namespace LSW._02._Code.Sos
{
    [CreateAssetMenu(fileName = "Weapon Data", menuName = "So/Data/Weapon Data", order = 1)]
    public class WeaponData : ScriptableObject
    {
        public WeaponType weaponType;
        [Min(0f)] public float attackRadius = 0.5f;
        [Min(0f)] public float range = 30f;
        [Min(0f)] public float attackCooldown = 0.3f;
        public float selfKnockbackPower;
    }
}
