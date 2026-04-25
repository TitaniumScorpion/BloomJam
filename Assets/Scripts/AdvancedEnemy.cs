using UnityEngine;

public class AdvancedEnemy : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 20;
    private int currentHealth;

    [Header("Artillery Settings")]
    [Tooltip("A BoxCollider defining the area where projectiles will randomly land.")]
    public BoxCollider targetZone;
    public string enemyProjectileTag = "EnemyProjectile";
    public Transform firePoint;
    public float fireInterval = 2f; // Decreased to fire faster
    public int projectilesPerShot = 2; // Number of projectiles fired at once
    public float projectileSpawnSpread = 1.5f; // Spread out the spawn points to prevent instant collisions
    public float projectileForwardForce = 25f; // Increased speed
    public float projectileUpwardArc = 12f; // Increased arc to match speed
    public float projectileScaleMultiplier = 2f; // Makes the projectile bigger

    [Header("Minion Spawning Settings")]
    public string swarmerTag = "StandardSwarmer";
    public Transform minionSpawnPoint;
    public float spawnInterval = 4f;

    private float fireTimer;
    private float spawnTimer;
    private Quaternion initialRotation;

    private void OnEnable()
    {
        currentHealth = maxHealth;
        
        // Stagger the initial timers so it doesn't shoot and spawn on the exact same frame it appears
        fireTimer = fireInterval;
        spawnTimer = spawnInterval * 0.5f;
        initialRotation = transform.rotation;
    }

    private void Update()
    {
        HandleRotation();

        // Handle Artillery Firing
        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0f)
        {
            ShootArtillery();
            fireTimer = fireInterval;
        }

        // Handle Minion Spawning
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnSwarmer();
            spawnTimer = spawnInterval;
        }
    }

    private void HandleRotation()
    {
        // A simple back-and-forth sweep animation using a sine wave
        float sweepAngle = Mathf.Sin(Time.time * 0.3f) * 45f; // Sweeps 45 degrees to each side
        transform.rotation = initialRotation * Quaternion.Euler(0, sweepAngle, 0);
    }

    private void ShootArtillery()
    {
        if (targetZone == null)
        {
            Debug.LogWarning("AdvancedEnemy is missing a Target Zone collider. Cannot shoot.", this);
            return;
        }

        Vector3 firePos = firePoint != null ? firePoint.position : transform.position + Vector3.up * 2f;
        
        Bounds zoneBounds = targetZone.bounds;

        // Fire multiple projectiles at once
        for (int i = 0; i < projectilesPerShot; i++)
        {
            // Add a random offset so projectiles don't spawn exactly inside each other
            Vector3 spawnOffset = Random.insideUnitSphere * projectileSpawnSpread;
            Vector3 actualFirePos = firePos + spawnOffset;

            // Get a random point within the specified zone bounds for each projectile
            Vector3 randomTargetPoint = new Vector3(Random.Range(zoneBounds.min.x, zoneBounds.max.x), zoneBounds.center.y, Random.Range(zoneBounds.min.z, zoneBounds.max.z));
    
            Vector3 directionToTarget = (randomTargetPoint - actualFirePos).normalized;
    
            GameObject proj = ObjectPooler.Instance.SpawnFromPool(enemyProjectileTag, actualFirePos, Quaternion.LookRotation(directionToTarget));
            
            if (proj != null)
            {
                proj.transform.localScale = Vector3.one * projectileScaleMultiplier;
    
                if (proj.TryGetComponent(out Rigidbody rb))
                {
                    // Add a burst of velocity: forward towards the target zone, plus an upward arc
                    Vector3 arcVelocity = (directionToTarget * projectileForwardForce) + (Vector3.up * projectileUpwardArc);
                    rb.linearVelocity = arcVelocity;
                }
            }
        }
    }

    private void SpawnSwarmer()
    {
        Vector3 spawnPos = minionSpawnPoint != null ? minionSpawnPoint.position : transform.position + transform.forward * 2f;
        ObjectPooler.Instance.SpawnFromPool(swarmerTag, spawnPos, Quaternion.identity);
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            StandardSwarmer.ReportDeath(); // Tells the Quota Manager we died
            gameObject.SetActive(false);
        }
    }
}