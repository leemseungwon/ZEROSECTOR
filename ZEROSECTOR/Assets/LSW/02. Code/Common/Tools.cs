using LSW._02._Code.Entity;
using LSW._02._Code.Sos;
using UnityEngine;

public class Tools
{
    public static bool IsSameLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    public static bool AttackTarget(BaseEntity attacker, BaseEntity target)
    {
        if (attacker == null || target == null || attacker == target)
            return false;

        return target.ReceiveHit(attacker);
    }
}