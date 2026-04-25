using UnityEngine;

public class AdvancedEnemy : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 20;
    private int currentHealth;

    [Header("Artillery Settings")]
    public string enemyProjectileTag = "EnemyProjectile";
    public Transform firePoint;
    public float fireInterval = 3f;
    public float projectileForwardForce = 15f;
    public float projectileUpwardArc = 8f; // Gives the projectile a nice dodgeable curve

    [Header("Minion Spawning Settings")]
    public string swarmerTag = "StandardSwarmer";
    public Transform minionSpawnPoint;
    public float spawnInterval = 4f;

    private Transform playerTransform;
    private float fireTimer;
    private float spawnTimer;

    private void Awake()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    private void OnEnable()
    {
        currentHealth = maxHealth;
        
        // Stagger the initial timers so it doesn't shoot and spawn on the exact same frame it appears
        fireTimer = fireInterval;
        spawnTimer = spawnInterval * 0.5f; 
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // Stand still, but slowly rotate to face the player (locking the Y axis so it doesn't tilt)
        Vector3 lookTarget = new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z);
        transform.LookAt(lookTarget);

        // Handle Artillery Firing
        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0f)
        {
            ShootAtPlayer();
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

    private void ShootAtPlayer()
    {
        Vector3 firePos = firePoint != null ? firePoint.position : transform.position + Vector3.up * 2f;
        Vector3 directionToPlayer = (playerTransform.position - firePos).normalized;

        GameObject proj = ObjectPooler.Instance.SpawnFromPool(enemyProjectileTag, firePos, Quaternion.LookRotation(directionToPlayer));
        
        if (proj != null && proj.TryGetComponent(out Rigidbody rb))
        {
            // Add a burst of velocity: forward towards the player, plus an upward arc to make it fall via gravity
            Vector3 arcVelocity = (directionToPlayer * projectileForwardForce) + (Vector3.up * projectileUpwardArc);
            rb.linearVelocity = arcVelocity;
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