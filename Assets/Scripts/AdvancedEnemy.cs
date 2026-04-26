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

    [Header("Rise Sequence Settings")]
    public float riseDuration = 4f;
    public float riseSpeed = 5f;
    public float rotationSpeed = 90f; // How fast the boss spins in degrees per second
    private bool isRising;
    private float riseTimer;

    private float fireTimer;
    private float spawnTimer;
    private float moveSoundTimer;
    private AudioSource moveAudioSource;

    private void Awake()
    {
        // Set up dedicated AudioSource for movement sounds to prevent ghost sounds when they die
        moveAudioSource = gameObject.AddComponent<AudioSource>();
        moveAudioSource.spatialBlend = 1f;
        moveAudioSource.minDistance = 15f; // Stays at 100% volume for a much larger radius
        moveAudioSource.maxDistance = 150f; // Can be heard clearly across the entire arena
        moveAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        moveAudioSource.playOnAwake = false;

        // Automatically find the Target Zone in the scene since Prefabs can't hold scene references
        if (targetZone == null)
        {
            GameObject zoneObj = GameObject.Find("TargetZone");
            if (zoneObj != null)
            {
                targetZone = zoneObj.GetComponent<BoxCollider>();
            }
            else
            {
                Debug.LogWarning("Could not find a GameObject named 'TargetZone' in the scene! Advanced Enemy artillery will fail.", this);
            }
        }
    }

    private void OnEnable()
    {
        currentHealth = maxHealth;
        
        // Enter the rising state as soon as it spawns
        isRising = true;
        riseTimer = riseDuration;
        moveSoundTimer = UnityEngine.Random.Range(0.5f, 1.5f);
        
        QuotaManager.OnZoneCleared += Despawn;
        QuotaManager.OnGameCompleted += Despawn;
    }

    private void OnDisable()
    {
        QuotaManager.OnZoneCleared -= Despawn;
        QuotaManager.OnGameCompleted -= Despawn;
        if (moveAudioSource != null) moveAudioSource.Stop(); // Stop sound immediately on death/despawn
    }

    private void Despawn()
    {
        gameObject.SetActive(false); // Return to pool without triggering death events
    }

    private void Update()
    {
        HandleRotation();

        // Handle Movement/Presence Sound
        moveSoundTimer -= Time.deltaTime;
        if (moveSoundTimer <= 0f)
        {
            if (AudioManager.Instance != null && AudioManager.Instance.eliteMoveSound != null)
            {
                moveAudioSource.clip = AudioManager.Instance.eliteMoveSound;
                moveAudioSource.volume = AudioManager.Instance.eliteMoveVolume;
                moveAudioSource.pitch = UnityEngine.Random.Range(0.8f, 1.1f);
                moveAudioSource.Play();
            }
            moveSoundTimer = UnityEngine.Random.Range(1.5f, 3f);
        }

        if (isRising)
        {
            // Space.World ensures it goes straight up regardless of its sweeping rotation
            transform.Translate(Vector3.up * riseSpeed * Time.deltaTime, Space.World);
            riseTimer -= Time.deltaTime;
            
            if (riseTimer <= 0f)
            {
                isRising = false;
                // Start the attack timers exactly when the rising sequence finishes
                fireTimer = fireInterval;
                spawnTimer = spawnInterval * 0.5f;
            }
            
            return; // Skip shooting and spawning while rising
        }

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
        // Constantly rotate the boss around its Y axis
        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.World);
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

        if (AudioManager.Instance != null && AudioManager.Instance.eliteShootSound != null)
        {
            // spatialBlend: 0.3f makes it 70% 2D (always audible globally) and 30% 3D (slight panning so you know which direction it fired from)
            AudioManager.Instance.PlaySoundAtLocation(AudioManager.Instance.eliteShootSound, firePos, AudioManager.Instance.eliteShootVolume, Random.Range(0.8f, 1.1f), 64, 0.3f);
        }

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