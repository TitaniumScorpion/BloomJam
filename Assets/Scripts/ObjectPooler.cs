using System.Collections.Generic;
using UnityEngine;

public class ObjectPooler : MonoBehaviour
{
    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
        [Tooltip("If checked, instances from this pool will never physically collide with each other (e.g. rapid-fire bullets bumping into and deflecting one another).")]
        public bool ignoreSelfCollisions;
    }

    public static ObjectPooler Instance;

    public List<Pool> pools;
    private Dictionary<string, Queue<GameObject>> poolDictionary;

    private void Awake()
    {
        // Simple Singleton pattern so any script can access the ObjectPooler easily
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (Pool pool in pools)
        {
            if (pool.prefab == null)
            {
                Debug.LogError($"The prefab for pool '{pool.tag}' is missing! Please assign it in the ObjectPooler Inspector.");
                continue; // Skip this broken pool so it doesn't crash the whole script
            }

            Queue<GameObject> objectPool = new Queue<GameObject>();
            List<Collider> poolColliders = pool.ignoreSelfCollisions ? new List<Collider>() : null;

            for (int i = 0; i < pool.size; i++)
            {
                // Parent the pooled objects to the ObjectPooler to keep the Hierarchy clean
                GameObject obj = Instantiate(pool.prefab, transform);
                obj.SetActive(false);
                objectPool.Enqueue(obj);

                if (pool.ignoreSelfCollisions)
                    poolColliders.AddRange(obj.GetComponentsInChildren<Collider>(true));
            }

            if (pool.ignoreSelfCollisions)
            {
                // Since pooled instances are reused (never destroyed), ignoring each pair once here holds for the pool's whole lifetime
                for (int i = 0; i < poolColliders.Count; i++)
                    for (int j = i + 1; j < poolColliders.Count; j++)
                        Physics.IgnoreCollision(poolColliders[i], poolColliders[j], true);
            }

            poolDictionary.Add(pool.tag, objectPool);
        }
    }

    /// <summary>
    /// Deactivates every live instance in every pool. Called at zone transitions to clear
    /// leftover enemies, projectiles and effects in a single pass.
    ///
    /// Replaces the previous approach of running one FindObjectsByType scan per type, which
    /// scanned the whole scene repeatedly and only covered a hardcoded list — dashers, trail
    /// enemies, trail segments and sword waves were being missed. Pools added later are
    /// covered automatically.
    /// </summary>
    public void DeactivateAll()
    {
        if (poolDictionary == null) return;

        foreach (Queue<GameObject> pool in poolDictionary.Values)
            foreach (GameObject obj in pool)
                if (obj != null && obj.activeSelf)
                    obj.SetActive(false);
    }

    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning("Pool with tag " + tag + " doesn't exist.");
            return null;
        }

        GameObject objectToSpawn = poolDictionary[tag].Dequeue();

        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;

        objectToSpawn.SetActive(true); // Set active MUST be called after position/rotation so OnEnable fires with correct data

        poolDictionary[tag].Enqueue(objectToSpawn); // Put it back at the end of the queue for later reuse

        return objectToSpawn;
    }
}