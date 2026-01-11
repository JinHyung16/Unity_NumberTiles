using UnityEngine;

namespace NTGame
{
    public class SceneSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                _instance = FindExistingInstance();
                if (_instance != null)
                    return _instance;

                var go = new GameObject(typeof(T).Name);
                _instance = go.AddComponent<T>();
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                return;
            }

            if (_instance == this)
                return;

            Destroy(gameObject);
        }

        static T FindExistingInstance()
        {
            var found = Object.FindObjectsOfType<T>(true);
            if (found == null || found.Length == 0)
                return null;
            return found[0];
        }
    }
}

