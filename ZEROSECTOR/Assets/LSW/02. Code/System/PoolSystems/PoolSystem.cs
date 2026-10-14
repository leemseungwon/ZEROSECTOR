using System;
using System.Collections.Generic;
using LSW._02._Code.Common;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LSW._02._Code.System.PoolSystems
{
    [SystemOrder(-20)]
    public class PoolSystem : MonoBehaviour, ISystem
    {
        [SerializeField] private List<PoolingData> pool = new List<PoolingData>();

        private readonly Dictionary<string, List<IPoolable>> _pooledObjects = new Dictionary<string, List<IPoolable>>();
        private readonly Dictionary<string, Func<GameObject>> _runtimePoolFactories = new();

        public void Initialize(SystemHandler systemHandler)
        {
            _pooledObjects.Clear();
            _runtimePoolFactories.Clear();
            for (int i = 0; i < pool.Count; i++)
            {
                string poolName = pool[i].poolName;
                if (!_pooledObjects.ContainsKey(poolName))
                {
                    _pooledObjects.Add(poolName, InitializePool(poolName));
                }
                else
                {
                    Debug.LogError($"Pool {pool[i].poolName} already exists!");
                }
            }
        }

        private List<IPoolable> InitializePool(string poolName)
        {
            List<IPoolable> spawnedPoolList = new List<IPoolable>();

            PoolingData data = pool.Find(p => p.poolName == poolName);

            if (data.poolObject == null)
            {
                return spawnedPoolList;
            }
            
            for (int i = 0; i < data.initialPoolSize; i++)
            {
                IPoolable poolable = CreateNewPoolable(data.poolObject);
                if (poolable != null)
                {
                    spawnedPoolList.Add(poolable);
                }
            }
            return spawnedPoolList;
        }

        private IPoolable CreateNewPoolable(Object prefab)
        {
            GameObject spawnedPool = Instantiate(prefab) as GameObject;
            if (spawnedPool == null)
                return null;

            return InitializePoolable(spawnedPool);
        }

        private IPoolable InitializePoolable(GameObject spawnedPool)
        {
            if (spawnedPool == null)
                return null;

            IPoolable poolable = spawnedPool.GetComponent<IPoolable>();
            if (poolable != null)
            {
                try
                {
                    poolable.Initialize();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"IPoolable.Initialize threw: {e}");
                }
                
                spawnedPool.SetActive(false);
                spawnedPool.gameObject.transform.SetParent(gameObject.transform);
            }
            else
            {
                Debug.LogWarning($"Created pool object {spawnedPool.name} does not implement IPoolable");
            }
            return poolable;
        }

        public void RegisterRuntimePool(string poolName, Func<GameObject> factory, int initialPoolSize = 1)
        {
            if (string.IsNullOrEmpty(poolName) || factory == null || _pooledObjects.ContainsKey(poolName))
                return;

            List<IPoolable> instances = new List<IPoolable>();
            _pooledObjects.Add(poolName, instances);
            _runtimePoolFactories.Add(poolName, factory);

            for (int i = 0; i < Mathf.Max(0, initialPoolSize); i++)
            {
                IPoolable poolable = InitializePoolable(factory());
                if (poolable != null)
                    instances.Add(poolable);
            }
        }

        public bool SpawnPool<T>(string poolName, out T pooledObject) where T : IPoolable
        {
            pooledObject = default;

            if (!_pooledObjects.TryGetValue(poolName, out List<IPoolable> poolableList))
                return false;

            IPoolable target = null;

            for (int i = 0; i < poolableList.Count; i++)
            {
                if (poolableList[i] is Component comp && !comp.gameObject.activeSelf)
                {
                    target = poolableList[i];
                    break;
                }
            }

            if (target == null)
            {
                var data = pool.Find(p => p.poolName == poolName);
                if (data.poolObject != null)
                {
                    target = CreateNewPoolable(data.poolObject);
                    if (target != null)
                        poolableList.Add(target);
                }
                else if (_runtimePoolFactories.TryGetValue(poolName, out Func<GameObject> factory))
                {
                    target = InitializePoolable(factory());
                    if (target != null)
                        poolableList.Add(target);
                }
            }

            if (target != null && target is T castedTarget)
            {
                pooledObject = castedTarget;

                if (target is Component component)
                {
                    component.gameObject.SetActive(true);
                }
                
                pooledObject.Spawn();
                return true;
            }

            Debug.LogError("Failed to spawn pool object");
            return false;
        }

        public bool HasPool(string poolName)
        {
            return !string.IsNullOrEmpty(poolName) && _pooledObjects.ContainsKey(poolName);
        }

        public void DespawnPool(string poolName, IPoolable poolable)
        {
            if (!_pooledObjects.TryGetValue(poolName, out List<IPoolable> poolableList)) return;

            if (poolableList.Contains(poolable))
            {
                if (poolable is Component component)
                {
                    Rigidbody rb = component.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        if (!rb.isKinematic)
                        {
                            try { rb.linearVelocity = Vector3.zero; } catch (Exception e) { Debug.LogWarning(e); }
                            try { rb.angularVelocity = Vector3.zero; } catch (Exception e) { Debug.LogWarning(e); }
                        }

                        rb.isKinematic = true;
                    }

                    component.gameObject.SetActive(false);
                    component.transform.position = Vector3.zero;
                    component.transform.rotation = Quaternion.identity;
                    
                    component.gameObject.transform.SetParent(gameObject.transform);
                }
                poolable.Despawn();
            }
        }

        public void Reset()
        {
            foreach (var kvp in _pooledObjects)
            {
                foreach (var poolable in kvp.Value)
                {
                    Component component = poolable as Component;

                    if (component != null)
                    {
                        Destroy(component.gameObject);
                    }
                }
            }
            _pooledObjects.Clear();
            _runtimePoolFactories.Clear();
        }
    }
}
