using System;
using System.Collections.Generic;
using System.Linq;
using LSW._02._Code.Common;
using UnityEngine;

namespace LSW._02._Code.System
{
    [DefaultExecutionOrder(-100)]
    public class SystemHandler : MonoSingleton<SystemHandler>
    {
        private Dictionary<Type, ISystem> _systems = new Dictionary<Type, ISystem>();
        
        protected override void Awake()
        {
            base.Awake();
            
            _systems = GetComponentsInChildren<ISystem>(true)
                .ToDictionary(system => system.GetType());
            InitSystem();
        }

        private void InitSystem()
        {
            List<ISystem> systems =
                _systems.Values.OrderBy(GetSystemOrder).ToList();

            foreach (ISystem system in systems)
            {
                system.Initialize(this);
            }
        }

        private int GetSystemOrder(ISystem system)
        {
            SystemOrderAttribute attribute =
                Attribute.GetCustomAttribute(system.GetType(), typeof(SystemOrderAttribute)) 
                    as SystemOrderAttribute;

            return attribute?.Order ?? 0;
        }
        
        public bool GetSystem<T>(out T system) where T : ISystem
        {
            system = default;
            ISystem find = _systems.Values.FirstOrDefault(c => c is T);
            if (find is T findSystem)
            {
                system = findSystem;
                return true;
            }
            return false;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            foreach (ISystem system in _systems.Values)
            {
                system.Reset();
            }
            _systems.Clear();
        }
    }
}