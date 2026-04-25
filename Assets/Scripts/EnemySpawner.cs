using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public string swarmerPoolTag = "StandardSwarmer";
    public float spawnInterval = 6f; // Increased time to decrease spawn rate
    public int enemiesPerWave = 7;   // Increased to spawn 7 enemies at once

    [Header("Telegraph Settings")]
    public string telegraphPoolTag = "SpawnTelegraph";
    public float telegraphDuration = 3f; // Time the warning effect stays before enemies appear

    [Header("Advanced Escalation Settings")]
    public string advancedEnemyPoolTag = "AdvancedEnemy";
    public float advancedSpawnInterval = 20f; // Exactly 20 seconds between advanced enemies
    
    // Zone 3 Specifics for standard swarms
    [Header("Zone 3 Settings")]
    [Tooltip("How fast waves spawn in Zone 3 (Index 2)")]
    public float zone3SpawnInterval = 1.5f;
    [Tooltip("How many enemies spawn per wave in Zone 3")]
    public int zone3EnemiesPerWave = 8;

    [Header("Spawn Locations")]
    [Tooltip("If empty, enemies will spawn in a random radius around this object.")]
    public Transform[] spawnPoints;
    public float spawnRadius = 20f;

    private float spawnTimer;
    private bool isSpawningActive = true;
    private float advancedSpawnTimer;
    private int maxAdvancedEnemiesToSpawn = 0;
    private int advancedEnemiesSpawned = 0;

    private void OnEnable()
    {
        QuotaManager.OnZoneCleared += StopSpawning;
    }

    private void OnDisable()
    {
        QuotaManager.OnZoneCleared -= StopSpawning;
    }

    private void Start()
    {
        // Start the timer
        spawnTimer = spawnInterval;
        advancedSpawnTimer = advancedSpawnInterval; // Start the 20s countdown for elites
        
        // Setup spawner based on the persistent current zone index
        ApplyZoneSettings(QuotaManager.currentZoneIndex);
    }

    private void Update()
    {
        if (!isSpawningActive) return;

        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            StartCoroutine(SpawnWaveRoutine());
            spawnTimer = spawnInterval; // Reset timer
        }

        // Separate timer logic for Advanced Enemies
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

    private void ApplyZoneSettings(int zoneIndex)
    {
        if (zoneIndex == 1) // Advanced to Zone 2
        {
            maxAdvancedEnemiesToSpawn = 4;
        }
        else if (zoneIndex >= 2) // Advanced to Zone 3
        {
            maxAdvancedEnemiesToSpawn = 8;
            spawnInterval = zone3SpawnInterval;
            enemiesPerWave = zone3EnemiesPerWave;
        }
    }

    private void StopSpawning()
    {
        isSpawningActive = false;
    }

    private IEnumerator SpawnWaveRoutine()
    {
        // Pick ONE spawn location for the entire swarm
        Vector3 waveBasePosition = GetSpawnPosition();

        // Spawn the telegraph particle/object
        GameObject telegraph = ObjectPooler.Instance.SpawnFromPool(telegraphPoolTag, waveBasePosition, Quaternion.identity);

        // Wait for the telegraph duration before actually spawning the enemies
        yield return new WaitForSeconds(telegraphDuration);

        // Deactivate the telegraph object (returning it to the pool)
        if (telegraph != null)
        {
            telegraph.SetActive(false);
        }

        // If the zone was cleared while we were waiting, abort the spawn
        if (!isSpawningActive) yield break;

        for (int i = 0; i < enemiesPerWave; i++)
        {
            // Add a small random offset so the swarm doesn't spawn exactly inside each other
            Vector3 randomOffset = new Vector3(Random.Range(-2f, 2f), Random.Range(0f, 2f), Random.Range(-2f, 2f));
            Vector3 spawnPos = waveBasePosition + randomOffset;

            ObjectPooler.Instance.SpawnFromPool(swarmerPoolTag, spawnPos, Quaternion.identity);
            
            // Small delay between individual spawns so they don't perfectly overlap
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void SpawnAdvancedEnemy()
    {
        // Grab a spawn position and spawn the elite independently of the swarms
        Vector3 spawnPos = GetSpawnPosition();
        ObjectPooler.Instance.SpawnFromPool(advancedEnemyPoolTag, spawnPos, Quaternion.identity);
    }

    private Vector3 GetSpawnPosition()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int randomIndex = Random.Range(0, spawnPoints.Length);
            return spawnPoints[randomIndex].position;
        }

        // Fallback: Random point ON THE EDGE of a circle around the spawner (Zone 1 style)
        Vector2 randomDir = Random.insideUnitCircle;
        if (randomDir == Vector2.zero) randomDir = Vector2.right; // Safety check
        
        Vector2 edgePoint = randomDir.normalized * spawnRadius; // .normalized pushes it to the outer boundary
        
        float randomHeight = Random.Range(2f, 6f); // Give them a randomized floating spawn height
        return transform.position + new Vector3(edgePoint.x, randomHeight, edgePoint.y);
    }
}