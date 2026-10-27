using LSW._02._Code.Sos.EntityStatDatas;
using LSW._02._Code.System.PoolSystems;
using UnityEngine;

namespace LSW._02._Code.Entity.Boss
{
    public class BaseBoss : BaseEntity, IPoolable
    {
        public int CurrentHealthCount { get; set; }

        protected int MaxHealthCount => BossStatData != null
            ? Mathf.Max(1, BossStatData.healthCount)
            : StatData is BossStatData statData ? Mathf.Max(1, statData.healthCount) : 1;
        
        private BossStatData BossStatData => StatData as BossStatData;

        public override void InitStat()
        {
            base.InitStat();
            CurrentHealthCount = MaxHealthCount;
        }

        public virtual void Initialize()
        {
            CurrentHealthCount = MaxHealthCount;
        }

        public virtual void Spawn()
        {
            CurrentHealthCount = MaxHealthCount;
        }
        
        public override bool ReceiveHit(BaseEntity attacker)
        {
            if (IsDead || (GetCompo(out EntityBuffComponent buff) && buff.IsInvincible))
                return false;

            CurrentHealthCount = Mathf.Max(0, CurrentHealthCount - 1);
            if (CurrentHealthCount <= 0)
                return base.ReceiveHit(attacker);
            return true;
        }

        public virtual void Despawn()
        {
            
        }
        
    }
}
