using UnityEngine;

namespace LSW._02._Code.Sos.EntityStatDatas
{
    [CreateAssetMenu(fileName = "PlayerStatData", menuName = "So/Data/Stat Data/Player Stat Data", order = 0)]
    public class PlayerStatData : EntityStatData
    {
        public float initJumpForce;
        public float postDashInvincibilityDuration = 0.2f;
        public float postParryInvincibilityDuration = 0.5f;
    }
}