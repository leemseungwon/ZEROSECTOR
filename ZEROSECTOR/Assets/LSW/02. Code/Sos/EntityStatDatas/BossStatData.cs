using UnityEngine;

namespace LSW._02._Code.Sos.EntityStatDatas
{
    [CreateAssetMenu(fileName = "BossStatData", menuName = "So/Data/Stat Data/Boss Stat Data", order = 0)]
    public class BossStatData : EnemyStatData
    {
        public int healthCount;
    }
}