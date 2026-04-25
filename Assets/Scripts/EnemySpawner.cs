using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public string swarmerPoolTag = "StandardSwarmer";
    public float spawnInterval = 3f; // How often a new wave spawns
    public int enemiesPerWave = 5;   // How many enemies spawn at once

    [Header("Advanced Escalation Settings")]
    public string advancedEnemyPoolTag = "AdvancedEnemy";
    [Range(0f, 1f)] public float advancedEnemySpawnChance = 0.2f; // 20% chance to spawn an advanced enemy
    
    [Tooltip("How fast waves spawn in Zone 3 (Index 2)")]
    public float zone3SpawnInterval = 1.5f;
    [Tooltip("How many enemies spawn per wave in Zone 3")]
    public int zone3EnemiesPerWave = 8;

    [Header("Spawn Locations")]
    [Tooltip("If empty, enemies will spawn in a random radius around this object.")]
    public Transform[] spawnPoints;
    public float spawnRadius = 20f;

    private float spawnTimer;
    private bool canSpawnAdvanced = false;

    private void OnEnable()
    {
        QuotaManager.OnZoneAdvanced += HandleZoneAdvanced;
    }

    private void OnDisable()
    {
        QuotaManager.OnZoneAdvanced -= HandleZoneAdvanced;
    }

    private void Start()
    {
        // Start the timer
        spawnTimer = spawnInterval;
    }

    private void Update()
    {
        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            StartCoroutine(SpawnWaveRoutine());
            spawnTimer = spawnInterval; // Reset timer
        }
    }

    private void HandleZoneAdvanced(int zoneIndex)
    {
        if (zoneIndex == 1) // Advanced to Zone 2
        {
            canSpawnAdvanced = true;
        }
        else if (zoneIndex == 2) // Advanced to Zone 3
        {
            canSpawnAdvanced = true;
            spawnInterval = zone3SpawnInterval;
            enemiesPerWave = zone3EnemiesPerWave;
        }
    }

    private IEnumerator SpawnWaveRoutine()
    {
        for (int i = 0; i < enemiesPerWave; i++)
        {
            Vector3 spawnPos = GetSpawnPosition();
            
            // TODO: According to GDD, play a brief visual/spatial audio cue here 
            // Example: ObjectPooler.Instance.SpawnFromPool("SpawnTelegraph", spawnPos, Quaternion.identity);
            // yield return new WaitForSeconds(0.5f); // Wait for telegraph to finish

            string tagToSpawn = swarmerPoolTag;
            // If we are allowed to spawn advanced enemies, roll a random chance
            if (canSpawnAdvanced && Random.value <= advancedEnemySpawnChance)
            {
                tagToSpawn = advancedEnemyPoolTag;
            }

            ObjectPooler.Instance.SpawnFromPool(tagToSpawn, spawnPos, Quaternion.identity);
            
            // Small delay between individual spawns so they don't perfectly overlap
            yield return new WaitForSeconds(0.1f);
        }
    }

    private Vector3 GetSpawnPosition()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int randomIndex = Random.Range(0, spawnPoints.Length);
            return spawnPoints[randomIndex].position;
        }

        // Fallback: Random point in a flat circle around the spawner (Zone 1 style)
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        return transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
    }
}