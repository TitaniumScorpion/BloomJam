using System.Collections;
using System.Collections.Generic;
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
    [Tooltip("Specific spawn points strictly for Advanced Enemies (e.g. outside the arena).")]
    public Transform[] advancedSpawnPoints;
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

        if (AudioManager.Instance != null && AudioManager.Instance.enemySpawnTelegraphSound != null)
        {
            AudioManager.Instance.PlaySoundAtLocation(AudioManager.Instance.enemySpawnTelegraphSound, waveBasePosition, AudioManager.Instance.enemySpawnTelegraphVolume, 1f);
        }

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
        // Spawn the elite independently around the edge of the arena
        Vector3 spawnPos = GetAdvancedSpawnPosition();
        
        // Make the boss face the center of the arena
        Vector3 directionToCenter = transform.position - spawnPos;
        directionToCenter.y = 0; // Keep the rotation perfectly level
        Quaternion spawnRot = directionToCenter != Vector3.zero ? Quaternion.LookRotation(directionToCenter) : Quaternion.identity;
        
        ObjectPooler.Instance.SpawnFromPool(advancedEnemyPoolTag, spawnPos, spawnRot);
    }

    private Vector3 GetAdvancedSpawnPosition()
    {
        // If you assigned specific spawn points for the boss in the inspector, pick one!
        if (advancedSpawnPoints != null && advancedSpawnPoints.Length > 0)
        {
            List<Transform> availablePoints = new List<Transform>();
            AdvancedEnemy[] activeElites = FindObjectsOfType<AdvancedEnemy>(); // Only finds currently active/spawned bosses

            foreach (Transform point in advancedSpawnPoints)
            {
                bool isOccupied = false;
                foreach (AdvancedEnemy elite in activeElites)
                {
                    // Calculate horizontal distance only, ignoring the fact that they start deep underground
                    Vector2 pointPos2D = new Vector2(point.position.x, point.position.z);
                    Vector2 elitePos2D = new Vector2(elite.transform.position.x, elite.transform.position.z);
                    
                    if (Vector2.Distance(pointPos2D, elitePos2D) < 10f) // 10 units safe zone radius
                    {
                        isOccupied = true;
                        break;
                    }
                }

                if (!isOccupied) availablePoints.Add(point);
            }

            // Pick from the safe available points, or fallback to ANY point if the arena is entirely full
            Transform selectedPoint = availablePoints.Count > 0 
                ? availablePoints[Random.Range(0, availablePoints.Count)] 
                : advancedSpawnPoints[Random.Range(0, advancedSpawnPoints.Length)];
            
            // Start deep below the chosen point
            return new Vector3(selectedPoint.position.x, selectedPoint.position.y - 15f, selectedPoint.position.z);
        }

        // Pick a random direction from the center
        Vector2 randomDir = Random.insideUnitCircle;
        if (randomDir == Vector2.zero) randomDir = Vector2.right; // Safety check
        
        // Push the spawn point strictly to the outer perimeter (spawnRadius + extra padding)
        Vector2 edgePoint = randomDir.normalized * (spawnRadius + 8f); 
        
        // Start deep below the arena floor
        float startHeight = -15f; 
        return transform.position + new Vector3(edgePoint.x, startHeight, edgePoint.y);
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