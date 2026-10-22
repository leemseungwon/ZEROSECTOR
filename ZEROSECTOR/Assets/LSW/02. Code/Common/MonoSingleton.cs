using System;
using UnityEngine;

namespace LSW._02._Code.Common
{
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<T>();

                    if (_instance == null)
                    {
                        var go = new GameObject(typeof(T).ToString());
                        _instance = go.AddComponent<T>();
                    }
                }

                return _instance;
            }
        }

        protected virtual void Awake()
        {
            try
            {
                if (_instance == null)
                {
                    _instance = this as T;
                }
                else if (_instance != this)
                {
                    Destroy(gameObject);
                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        protected MonoSingleton() { }

        protected MonoSingleton(bool shouldCreateNewInstance)
        {
            if (shouldCreateNewInstance)
            {
                _instance = null;
            }
        }
    }
}