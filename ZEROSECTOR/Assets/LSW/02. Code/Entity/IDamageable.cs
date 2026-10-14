using UnityEngine;

namespace LSW._02._Code.Entity
{
    public interface IDamageable
    {
        bool TakeDamage(int finalDamage, Vector2 knockBackDirection, float knockBackPower, float stunDuration,
            BaseEntity attacker);
    }
}
