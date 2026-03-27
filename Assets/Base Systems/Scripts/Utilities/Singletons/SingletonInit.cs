using UnityEngine;

namespace Base_Systems.Scripts.Utilities.Singletons
{
    public class SingletonInit<T> : SingletonBase where T : Component
    {
        public static T Instance { get; private set; } = null;
        
        protected override void Awake()
        {
            if (Instance == null)
                Instance = this as T;
            else
                Destroy(gameObject);
        }
    }
}