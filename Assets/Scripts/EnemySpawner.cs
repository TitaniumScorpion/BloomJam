using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public string swarmerPoolTag = "StandardSwarmer";
    public float spawnInterval = 3f; // How often a new wave spawns
    public int enemiesPerWave = 5;   // How many enemies spawn at once

    [Header("Spawn Locations")]
    [Tooltip("If empty, enemies will spawn in a random radius around this object.")]
    public Transform[] spawnPoints;
    public float spawnRadius = 20f;

    private float spawnTimer;

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

    private IEnumerator SpawnWaveRoutine()
    {
        for (int i = 0; i < enemiesPerWave; i++)
        {
            Vector3 spawnPos = GetSpawnPosition();
            
            // TODO: According to GDD, play a brief visual/spatial audio cue here 
            // Example: ObjectPooler.Instance.SpawnFromPool("SpawnTelegraph", spawnPos, Quaternion.identity);
            // yield return new WaitForSeconds(0.5f); // Wait for telegraph to finish

            ObjectPooler.Instance.SpawnFromPool(swarmerPoolTag, spawnPos, Quaternion.identity);
            
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