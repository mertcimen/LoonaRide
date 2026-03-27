using System;
using System.Collections.Generic;
using Base_Systems.Scripts.Managers;
using Base_Systems.Scripts.Utilities.Singletons;
using Fiber.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Base_Systems.Scripts.Utilities
{
    public class ObjectPooler : Singleton<ObjectPooler>
    {
        [Serializable]
        public class Pool
        {
            [Tooltip("Give a tag to the pool to call")]
            public string Tag;
            [Tooltip("The prefab to be pooled")]
            public GameObject Prefab;
            [Tooltip("The initial size (count) of the pool")]
            public int InitialSize = 10;
            [Tooltip("The maximum size (count) the pool can reach")]
            public int MaxSize = 10;
        }

        private class Pooled
        {
            public GameObject Prefab;
            public Queue<GameObject> InactiveQueue = new Queue<GameObject>();
            public List<GameObject> ActiveList = new List<GameObject>();

            public int CountAll;
            public int CountMax;
            public int CountActive => ActiveList.Count;
            public int CountInactive => InactiveQueue.Count;
        }

        [TableList]
        [SerializeField] private List<Pool> pools = new List<Pool>();
        private readonly Dictionary<string, Pooled> poolDictionary = new Dictionary<string, Pooled>();

        private void Awake() => Initialize();

        public ObjectPooler Initialize()
        {
            foreach (var pool in pools)
                AddToPool(pool.Tag, pool.Prefab, pool.InitialSize, pool.MaxSize);

            return this;
        }

        private void OnEnable() => LevelManager.OnLevelUnload += OnLevelUnload;
        private void OnDisable() => LevelManager.OnLevelUnload -= OnLevelUnload;

        private void OnLevelUnload() => DisableAllPooledObjects();

        /// <summary>Disable all active objects and return them to their pools</summary>
        private void DisableAllPooledObjects()
        {
            foreach (var kvp in poolDictionary)
            {
                var poolTag = kvp.Key;
                var pooled = kvp.Value;

                for (int i = pooled.ActiveList.Count - 1; i >= 0; i--)
                {
                    Release(pooled.ActiveList[i], poolTag);
                }
            }
        }

        // ------------------------------
        // SPAWN METHODS
        // ------------------------------

        /// <summary>
        /// Spawns an object from the pool at the given position and rotation
        /// </summary>
        public GameObject Spawn(string poolTag, Vector3 position, Quaternion? rotation = null, Transform parent = null, bool worldPosition = true)
        {
            if (!poolDictionary.TryGetValue(poolTag, out var pooled))
            {
                Debug.LogError($"[ObjectPooler] Pool with tag '{poolTag}' not found!");
                return null;
            }

            var obj = GetPooledObject(pooled, poolTag);
            if (obj == null) return null;

            // Transform setup
            if (parent != null)
                obj.transform.SetParent(parent, worldPosition);

            if (worldPosition)
                obj.transform.position = position;
            else
                obj.transform.localPosition = position;

            obj.transform.rotation = rotation ?? Quaternion.identity;
            obj.transform.localScale = Vector3.one;

            return obj;
        }

        // ------------------------------
        // CORE POOLING LOGIC
        // ------------------------------

        /// <summary>
        /// Returns an available pooled object or creates a new one if possible
        /// </summary>
        private GameObject GetPooledObject(Pooled pooled, string poolTag)
        {
            if (pooled.CountInactive > 0)
            {
                var obj = pooled.InactiveQueue.Dequeue();
                obj.SetActive(true);
                pooled.ActiveList.Add(obj);
                return obj;
            }

            if (pooled.CountAll < pooled.CountMax)
            {
                var obj = Instantiate(pooled.Prefab, transform);
                obj.SetActive(true);
                pooled.ActiveList.Add(obj);
                pooled.CountAll++;
                return obj;
            }

            Debug.LogError($"[ObjectPooler] Pool '{poolTag}' is full! Active: {pooled.CountActive}/{pooled.CountMax}");
            return null;
        }

        /// <summary>
        /// Release the object back to the pool
        /// </summary>
        public void Release(GameObject pooledGameObject, string poolTag)
        {
            if (!poolDictionary.TryGetValue(poolTag, out var pooled)) return;
            if (!pooled.ActiveList.Remove(pooledGameObject)) return;

            pooledGameObject.SetActive(false);
            pooledGameObject.transform.SetParent(transform);
            pooled.InactiveQueue.Enqueue(pooledGameObject);
        }

        /// <summary> Peek inactive objects without modifying the pool </summary>
        public GameObject[] Peek(string poolTag)
        {
            if (!poolDictionary.TryGetValue(poolTag, out var pooled)) return Array.Empty<GameObject>();
            return pooled.InactiveQueue.ToArray();
        }

        /// <summary>
        /// Creates a new pool with defined tag and object
        /// </summary>
        public void AddToPool(string poolTag, GameObject prefab, int count, int maxCount)
        {
            if (poolDictionary.ContainsKey(poolTag))
            {
                Debug.LogWarning($"[ObjectPooler] Pool tag '{poolTag}' already exists! Skipped.");
                return;
            }

            if (count > maxCount)
            {
                Debug.LogWarning($"[ObjectPooler] Max Count can't be smaller than Initial Count for '{poolTag}'");
                return;
            }

            var pooled = new Pooled { Prefab = prefab, CountAll = 0, CountMax = maxCount };

            for (int i = 0; i < count; i++)
            {
                var obj = Instantiate(prefab, transform);
                obj.SetActive(false);
                pooled.InactiveQueue.Enqueue(obj);
                pooled.CountAll++;
            }

            poolDictionary.Add(poolTag, pooled);
        }
    }
}
