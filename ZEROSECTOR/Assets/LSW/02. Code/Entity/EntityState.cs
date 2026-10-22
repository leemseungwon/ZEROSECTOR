using System;
using UnityEngine;

namespace LSW._02._Code.Entity
{
    [Serializable]
    public abstract class EntityState
    {
        protected readonly BaseEntity Entity;
        protected readonly EntityAnimation Animation;
        
        protected Action EndAction;

        protected float StateTimer;

        protected EntityState(BaseEntity entity)
        {
            Entity = entity;
            Animation = entity.Animation;
        }

        public virtual void Enter(Action endAction = null)
        {
            StateTimer = 0f;
            if (endAction != null)
            {
                EndAction = endAction;
            }
        }

        public virtual void Exit()
        {
            EndAction?.Invoke();
            EndAction = null;
        }

        public virtual void Update()
        {
            StateTimer += Time.deltaTime;
        }

        public virtual void FixedUpdate() { }
        
        public virtual void Dispose() { }
    }
}