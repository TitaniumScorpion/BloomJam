using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner Drone")]
    public SpawnerDrone dronePrefab;
    [Tooltip("Empty Transform at the arena center — drone orbits this point")]
    public Transform arenaCenter;
    public float droneRespawnDelay = 10f;

    [Header("Advanced Enemy")]
    public string advancedEnemyPoolTag = "AdvancedEnemy";
    public float advancedSpawnInterval = 20f;
    public int maxAdvancedEnemiesToSpawn = 0;
    public Transform[] advancedSpawnPoints;
    public float spawnRadius = 20f;

    private SpawnerDrone activeDrone;
    private bool isSpawningActive;
    private bool droneRespawnPending;
    private float advancedSpawnTimer;
    private int advancedEnemiesSpawned;

    private void OnEnable()
    {
        isSpawningActive = true;
        droneRespawnPending = false;
        advancedSpawnTimer = advancedSpawnInterval;
        advancedEnemiesSpawned = 0;

        QuotaManager.OnZoneCleared += StopSpawning;
        QuotaManager.OnGameCompleted += StopSpawning;

        SpawnDrone();
    }

    private void OnDisable()
    {
        isSpawningActive = false;
        CancelInvoke();

        QuotaManager.OnZoneCleared -= StopSpawning;
        QuotaManager.OnGameCompleted -= StopSpawning;

        if (activeDrone != null && activeDrone.gameObject.activeSelf)
            activeDrone.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Time.timeScale == 0f || !isSpawningActive) return;

        // Detect when drone is shot down → schedule a replacement
        if (activeDrone != null && !activeDrone.gameObject.activeSelf && !droneRespawnPending)
        {
            droneRespawnPending = true;
            Invoke(nameof(SpawnDrone), droneRespawnDelay);
        }

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
        droneRespawnPending = false;
        if (!isSpawningActive || dronePrefab == null) return;

        if (activeDrone == null)
            activeDrone = Instantiate(dronePrefab);

        activeDrone.arenaCenter = arenaCenter != null ? arenaCenter : transform;
        activeDrone.gameObject.SetActive(true);
    }

    private void StopSpawning()
    {
        isSpawningActive = false;
        CancelInvoke();

        if (activeDrone != null)
            activeDrone.gameObject.SetActive(false);
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
            AdvancedEnemy[] active = FindObjectsOfType<AdvancedEnemy>();

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
        Vector2 dir = Random.insideUnitCircle.normalized;
        return c + new Vector3(dir.x * (spawnRadius + 8f), -15f, dir.y * (spawnRadius + 8f));
    }
}
