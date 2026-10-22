using System;
using System.Collections.Generic;
using System.Linq;
using LSW._02._Code.Common;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LSW._02._Code.Managers
{
    [DefaultExecutionOrder(-101)]
    public class ManagerHandler : MonoSingleton<ManagerHandler>
    {
        private Dictionary<Type, IManager> _managers = new Dictionary<Type, IManager>();
        
        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);
            
            _managers = GetComponentsInChildren<IManager>(true)
                .ToDictionary(manager => manager.GetType());
            InitManager();

            SceneManager.sceneLoaded += LoadScene;
        }

        private void InitManager()
        {
            List<IManager> managers =
                _managers.Values.OrderBy(GetManagerOrder).ToList();

            foreach (IManager manager in managers)
            {
                manager.Initialize(this);
            }
        }

        private int GetManagerOrder(IManager manager)
        {
            ManagerOrderAttribute attribute =
                Attribute.GetCustomAttribute(manager.GetType(), typeof(ManagerOrderAttribute)) 
                    as ManagerOrderAttribute;

            return attribute?.Order ?? 0;
        }

        
        public bool GetManager<T>(out T manager) where T : IManager
        {
            manager = default;
            IManager find = _managers.Values.FirstOrDefault(c => c is T);
            if (find is T findManager)
            {
                manager = findManager;
                return true;
            }
            return false;
        }

        private void LoadScene(Scene scene, LoadSceneMode _)
        {
            SceneType sceneType = (SceneType) scene.buildIndex;

            foreach (IManager manager in _managers.Values)
            {
                manager.LoadScene(sceneType);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            SceneManager.sceneLoaded -= LoadScene;
            foreach (IManager manager in _managers.Values)
            {
                manager.Reset();
            }
            _managers.Clear();
        }
    }
}