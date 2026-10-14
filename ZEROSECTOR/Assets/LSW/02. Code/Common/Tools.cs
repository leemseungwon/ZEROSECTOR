using LSW._02._Code.Entity;
using LSW._02._Code.Sos;
using UnityEngine;

public class Tools
{
    public static bool IsSameLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    public static bool ReceiveDamageToTarget(BaseEntity receiver, BaseEntity target, AttackData attackData)
    {
        if(receiver == null || target == null || attackData == null)
        {
            Debug.LogWarning("Receiver, Target, or AttackData is null.");
            return false;
        }

        if (receiver == target)
            return false;

        int finalDamage = receiver.CurrentAttackDamage + attackData.attackDamage;
        Vector2 knockBackDir = ((Vector2)target.transform.position - (Vector2)receiver.transform.position).normalized;
        return target.TakeDamage(finalDamage, knockBackDir, attackData.knockbackPower, attackData.stunDuration, receiver);
    }
}