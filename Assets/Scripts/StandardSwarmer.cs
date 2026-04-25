using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody))]
public class StandardSwarmer : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 1;
    private int currentHealth;
    public float moveSpeed = 22f; // Increased to be much faster than the player so they can overshoot
    public float minMoveSpeed = 2f; // Slower speed used when turning or dodged
    public float acceleration = 14f; // High acceleration to simulate being "thrown"
    public float rotationSpeed = 6f; // Smooth turning when slow
    public float tiltAmount = 30f; // Degrees to bank/roll when turning
    [Header("Wobble Settings")]
    public float pitchWobbleSpeed = 8f; // How fast they bob up and down
    public float pitchWobbleAmount = 15f; // Degrees they tilt up and down
    public int collisionDamage = 1;
    
    [Header("Ground Avoidance")]
    public float hoverHeight = 1.5f; // How high they try to stay above the floor

    private Transform playerTransform;
    private Rigidbody rb;
    private float currentSpeed;
    private Quaternion baseRotation;

    // Event broadcasted whenever any standard swarmer dies
    public static event Action OnEnemyDied;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // Find the player once when the object is instantiated to save performance
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        
        rb.useGravity = false; // Disable gravity so they float/glide in the air
    }

    private void OnEnable()
    {
        // Because we are Object Pooling, we must reset the health every time it spawns
        currentHealth = maxHealth;
        currentSpeed = minMoveSpeed; // Start at minimum speed when spawned
        baseRotation = transform.rotation; // Reset base tracking rotation
        
        QuotaManager.OnZoneCleared += Despawn;
        QuotaManager.OnGameCompleted += Despawn;
    }

    private void OnDisable()
    {
        QuotaManager.OnZoneCleared -= Despawn;
        QuotaManager.OnGameCompleted -= Despawn;
    }

    private void Despawn()
    {
        gameObject.SetActive(false); // Return to pool without triggering death events
    }

    private void FixedUpdate()
    {
        if (playerTransform == null) return;

        // Aim for the center of the player's body rather than their feet
        Vector3 targetPosition = playerTransform.position + Vector3.up * 1.5f;
        Vector3 direction = (targetPosition - transform.position).normalized;
        
        // Use baseRotation for forward/right vectors so the cosmetic wobble doesn't break alignment logic
        Vector3 baseForward = baseRotation * Vector3.forward;
        Vector3 baseRight = baseRotation * Vector3.right;
        
        // 1. Calculate Alignment (1 = perfectly facing player, <= 0 = facing away)
        float alignment = Vector3.Dot(baseForward, direction);
        
        // 2. Modulate speed: Accelerate when closely aligned, brake hard if they miss/overshoot
        if (alignment > 0.6f)
        {
            // Swooping / Thrown at the player
            currentSpeed += acceleration * Time.fixedDeltaTime;
        }
        else
        {
            // Missed the player or turning around - hit the brakes to reset the charge
            currentSpeed -= (acceleration * 3f) * Time.fixedDeltaTime; 
        }
        currentSpeed = Mathf.Clamp(currentSpeed, minMoveSpeed, moveSpeed);

        // 3. Calculate dynamic rotation: Nimble when slow, but completely stiff when moving fast
        float speedPercent = (currentSpeed - minMoveSpeed) / (moveSpeed - minMoveSpeed);
        float currentRotationSpeed = Mathf.Lerp(rotationSpeed, 0.1f, speedPercent); // 0.1f forces them to fully commit to the trajectory

        // 4. Smoothly update the stiff base tracking rotation
        Quaternion targetLook = Quaternion.LookRotation(direction);
        baseRotation = Quaternion.Slerp(baseRotation, targetLook, Time.fixedDeltaTime * currentRotationSpeed);

        // 5. Calculate visual tilt (banking) and pitch wobble
        float turnAmount = Vector3.Dot(baseRight, direction);
        float wobble = Mathf.Sin(Time.time * pitchWobbleSpeed) * pitchWobbleAmount;
        Quaternion tiltRotation = Quaternion.Euler(wobble, 0f, -turnAmount * tiltAmount);
        
        // 6. Combine and smooth the final rotation so the wobble feels organic
        Quaternion smoothedFinalRotation = Quaternion.Slerp(transform.rotation, baseRotation * tiltRotation, Time.fixedDeltaTime * 12f);
        rb.MoveRotation(smoothedFinalRotation);
        
        // 7. Calculate target velocity based on the wavy flight path
        Vector3 targetVelocity = (smoothedFinalRotation * Vector3.forward) * currentSpeed;

        // 8. Ground Avoidance: Shoot a thick spherecast down to prevent them from scraping the floor
        // A SphereCast acts like a cylinder, detecting the ground even if the edges of the model dip
        if (Physics.SphereCast(transform.position, 0.5f, Vector3.down, out RaycastHit hit, hoverHeight * 1.5f))
        {
            // If we hit static environment (no rigidbody) that is too close
            if (hit.rigidbody == null && hit.distance < hoverHeight)
            {
                // 1. Completely erase any downward intent so they don't fight the push
                targetVelocity.y = Mathf.Max(targetVelocity.y, 0f);
                
                // 2. Push them up with a much stronger force
                float upwardPush = (hoverHeight - hit.distance) * 20f;
                targetVelocity.y += upwardPush;

                // 3. Instantly stop current downward momentum so they don't plunge 
                // into the ground while waiting for the smooth Lerp to catch up
                if (rb.linearVelocity.y < 0f)
                {
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                }
            }
        }
        
        // 9. Apply velocity smoothly
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * 10f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") && collision.gameObject.TryGetComponent(out PlayerHealth playerHealth))
        {
            playerHealth.TakeDamage(collisionDamage);
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // TODO: Play death sound, spawn ink/neon particles, and broadcast an event for the Quota Manager
        ReportDeath();

        // Deactivate the game object to return it to the Object Pool
        gameObject.SetActive(false);
    }

    // Helper method so other enemies (like the AdvancedEnemy) can count towards the zone quotas
    public static void ReportDeath()
    {
        OnEnemyDied?.Invoke();
    }
}