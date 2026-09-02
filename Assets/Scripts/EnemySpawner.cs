using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    // Static arena info read by DasherEnemy to pick wander targets
    public static Vector3 CurrentArenaCenter;
    public static float CurrentSpawnRadius;

    [Header("Spawner Drone")]
    public SpawnerDrone dronePrefab;
    [Tooltip("Empty Transform at the arena center — drone orbits this point")]
    public Transform arenaCenter;
    [Tooltip("Seconds between each new drone entering the arena")]
    public float droneSpawnInterval = 10f;
    [Tooltip("Maximum simultaneous alive drones. 0 = unlimited")]
    public int maxDrones = 0;

    [Header("Dasher Enemy")]
    public string dasherPoolTag = "DasherEnemy";
    [Tooltip("Seconds between each new Dasher spawning. 0 = disabled")]
    public float dasherSpawnInterval = 8f;

    [Header("Advanced Enemy")]
    public string advancedEnemyPoolTag = "AdvancedEnemy";
    public float advancedSpawnInterval = 20f;
    public int maxAdvancedEnemiesToSpawn = 0;
    public Transform[] advancedSpawnPoints;
    public float spawnRadius = 20f;

    private List<SpawnerDrone> spawnedDrones = new List<SpawnerDrone>();
    private float droneSpawnTimer;
    private float dasherSpawnTimer;
    private bool isSpawningActive;
    private float advancedSpawnTimer;
    private int advancedEnemiesSpawned;

    private void OnEnable()
    {
        isSpawningActive = true;
        droneSpawnTimer = 0f; // first drone appears immediately
        dasherSpawnTimer = dasherSpawnInterval;
        advancedSpawnTimer = advancedSpawnInterval;
        advancedEnemiesSpawned = 0;
        spawnedDrones.Clear();

        // Publish arena info so DasherEnemy can pick wander targets
        CurrentArenaCenter = arenaCenter != null ? arenaCenter.position : transform.position;
        CurrentSpawnRadius = spawnRadius;

        QuotaManager.OnZoneCleared += StopSpawning;
        QuotaManager.OnGameCompleted += StopSpawning;

        SpawnDrone();
        droneSpawnTimer = droneSpawnInterval; // next drone after a full interval
    }

    private void OnDisable()
    {
        isSpawningActive = false;
        CancelInvoke();

        QuotaManager.OnZoneCleared -= StopSpawning;
        QuotaManager.OnGameCompleted -= StopSpawning;

        foreach (SpawnerDrone drone in spawnedDrones)
            if (drone != null) drone.gameObject.SetActive(false);
        spawnedDrones.Clear();
    }

    private void Update()
    {
        if (Time.timeScale == 0f || !isSpawningActive) return;

        // Drone accumulation timer
        droneSpawnTimer -= Time.deltaTime;
        if (droneSpawnTimer <= 0f)
        {
            droneSpawnTimer = droneSpawnInterval;

            bool underCap = true;
            if (maxDrones > 0)
            {
                int alive = 0;
                foreach (SpawnerDrone d in spawnedDrones)
                    if (d != null && d.gameObject.activeSelf) alive++;
                underCap = alive < maxDrones;
            }

            if (underCap) SpawnDrone();
        }

        // Dasher accumulation timer
        if (dasherSpawnInterval > 0f)
        {
            dasherSpawnTimer -= Time.deltaTime;
            if (dasherSpawnTimer <= 0f)
            {
                dasherSpawnTimer = dasherSpawnInterval;
                SpawnDasher();
            }
        }

        // Advanced enemy timer
        if (advancedEnemiesSpawned < maxAdvancedEnemiesToSpawn)
        {
            advancedSpawnTimer -= Time.deltaTime;
            if (advancedSpawnTimer <= 0f)
            {
                SpawnAdvancedEnemy();
                advancedSpawnTimer = advancedSpawnInterval;
                advancedEnemiesSpawned++;
            }
        }
    }

    private void SpawnDrone()
    {
        if (!isSpawningActive || dronePrefab == null) return;

        SpawnerDrone drone = Instantiate(dronePrefab);
        drone.arenaCenter = arenaCenter != null ? arenaCenter : transform;
        spawnedDrones.Add(drone);
        drone.gameObject.SetActive(true);
    }

    private void StopSpawning()
    {
        isSpawningActive = false;
        CancelInvoke();

        foreach (SpawnerDrone drone in spawnedDrones)
            if (drone != null) drone.gameObject.SetActive(false);
        spawnedDrones.Clear();
    }

    private void SpawnDasher()
    {
        if (!isSpawningActive || string.IsNullOrEmpty(dasherPoolTag)) return;

        Vector2 circle = Random.insideUnitCircle * spawnRadius * 0.75f;
        float height = Random.Range(2f, 6f);
        Vector3 center = arenaCenter != null ? arenaCenter.position : transform.position;
        Vector3 spawnPos = center + new Vector3(circle.x, height, circle.y);

        ObjectPooler.Instance.SpawnFromPool(dasherPoolTag, spawnPos, Quaternion.identity);
    }

    private void SpawnAdvancedEnemy()
    {
        Vector3 spawnPos = GetAdvancedSpawnPosition();

        Vector3 center = arenaCenter != null ? arenaCenter.position : transform.position;
        Vector3 dir = center - spawnPos;
        dir.y = 0f;
        Quaternion spawnRot = dir != Vector3.zero ? Quaternion.LookRotation(dir) : Quaternion.identity;

        ObjectPooler.Instance.SpawnFromPool(advancedEnemyPoolTag, spawnPos, spawnRot);
    }

    private Vector3 GetAdvancedSpawnPosition()
    {
        if (advancedSpawnPoints != null && advancedSpawnPoints.Length > 0)
        {
            List<Transform> available = new List<Transform>();
            AdvancedEnemy[] active = FindObjectsByType<AdvancedEnemy>(FindObjectsSortMode.None);

            foreach (Transform pt in advancedSpawnPoints)
            {
                bool occupied = false;
                foreach (AdvancedEnemy ae in active)
                {
                    Vector2 p2 = new Vector2(pt.position.x, pt.position.z);
                    Vector2 e2 = new Vector2(ae.transform.position.x, ae.transform.position.z);
                    if (Vector2.Distance(p2, e2) < 10f) { occupied = true; break; }
                }
                if (!occupied) available.Add(pt);
            }

            Transform chosen = available.Count > 0
                ? available[Random.Range(0, available.Count)]
                : advancedSpawnPoints[Random.Range(0, advancedSpawnPoints.Length)];

            return new Vector3(chosen.position.x, chosen.position.y - 15f, chosen.position.z);
        }

        Vector3 c = arenaCenter != null ? arenaCenter.position : transform.position;
        Vector2 d2 = Random.insideUnitCircle.normalized;
        return c + new Vector3(d2.x * (spawnRadius + 8f), -15f, d2.y * (spawnRadius + 8f));
    }
}
