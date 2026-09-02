using System.Collections;
using UnityEngine;

/// <summary>
/// Shared flight AI for enemies that hunt the player through the air.
///
/// The movement models an enemy being *thrown* at the player rather than homing onto them:
/// it accelerates while roughly facing the player, and its turn rate falls toward zero as it
/// speeds up, so a fast pass commits to its trajectory and overshoots. Braking hard while
/// misaligned is what sets up the next swoop.
///
/// Field names here are unchanged from when they lived on StandardSwarmer/TrailEnemy, so
/// existing prefab values deserialize onto this class untouched.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public abstract class FlyingChaserEnemy : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public int maxHealth = 1;
    public int collisionDamage = 1;

    [Header("Movement")]
    [Tooltip("Top speed. Deliberately faster than the player so passes overshoot.")]
    public float moveSpeed = 22f;
    [Tooltip("Speed floor used while turning around after a missed pass.")]
    public float minMoveSpeed = 2f;
    public float acceleration = 14f;
    public float rotationSpeed = 6f;
    [Tooltip("Degrees of bank/roll applied when turning.")]
    public float tiltAmount = 30f;

    [Header("Wobble")]
    public float pitchWobbleSpeed = 8f;
    public float pitchWobbleAmount = 15f;

    [Header("Ground Avoidance")]
    [Tooltip("Height above the floor the enemy tries to maintain.")]
    public float hoverHeight = 1.5f;

    [Header("Hit Flash")]
    public Renderer enemyRenderer;
    public Material flashMaterial;
    public float flashDuration = 0.1f;

    protected Rigidbody rb;
    protected Transform playerTransform;
    protected int currentHealth;

    private float currentSpeed;
    private Quaternion baseRotation;
    private AudioSource moveAudioSource;
    private float moveSoundTimer;
    private Material originalMaterial;
    private Coroutine flashCoroutine;
    private bool markedForBulletTimeDeath;

    // Alignment above this counts as "swooping at the player" and accelerates
    private const float SwoopAlignmentThreshold = 0.6f;
    // Turn rate collapses to this at top speed, forcing commitment to the trajectory
    private const float CommittedTurnRate = 0.1f;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // They float/glide rather than fall

        // A dedicated source (rather than the shared AudioManager pool) so the buzz stops
        // the instant this enemy dies, instead of finishing as a ghost sound at its corpse
        moveAudioSource = gameObject.AddComponent<AudioSource>();
        moveAudioSource.spatialBlend = 1f;
        moveAudioSource.minDistance = 3f;
        moveAudioSource.maxDistance = 50f;
        moveAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        moveAudioSource.playOnAwake = false;

        if (enemyRenderer != null)
            originalMaterial = enemyRenderer.sharedMaterial;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    protected virtual void OnEnable()
    {
        // Pooled instances are reused, so every spawn has to reset run-time state
        currentHealth = maxHealth;
        currentSpeed = minMoveSpeed;
        baseRotation = transform.rotation;
        moveSoundTimer = Random.Range(0.5f, 1.5f); // Stagger so a whole pack does not chirp in unison
        markedForBulletTimeDeath = false;

        if (enemyRenderer != null && originalMaterial != null)
            enemyRenderer.sharedMaterial = originalMaterial;

        QuotaManager.OnZoneCleared += Despawn;
        QuotaManager.OnGameCompleted += Despawn;
    }

    protected virtual void OnDisable()
    {
        QuotaManager.OnZoneCleared -= Despawn;
        QuotaManager.OnGameCompleted -= Despawn;
        if (moveAudioSource != null) moveAudioSource.Stop();
    }

    /// <summary>Returns to the pool without counting as a kill.</summary>
    protected void Despawn() => gameObject.SetActive(false);

    /// <summary>Swaps to the bullet-time "marked for death" material. OnEnable restores the original.</summary>
    public void Mark(Material markedMaterial)
    {
        if (enemyRenderer != null && markedMaterial != null)
            enemyRenderer.sharedMaterial = markedMaterial;
    }

    protected virtual void FixedUpdate()
    {
        if (playerTransform == null) return;

        if (KatanaWeapon.IsBulletTimeActive)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        // Aim for the centre of the player's body rather than their feet
        Vector3 targetPosition = playerTransform.position + Vector3.up * 1.5f;
        Vector3 direction = (targetPosition - transform.position).normalized;

        // Derive facing from baseRotation, not transform.rotation — the cosmetic wobble is
        // folded into the latter and would otherwise corrupt the alignment maths
        Vector3 baseForward = baseRotation * Vector3.forward;
        Vector3 baseRight = baseRotation * Vector3.right;

        // Accelerate while pointed at the player; brake hard once the pass is missed
        float alignment = Vector3.Dot(baseForward, direction);
        currentSpeed += alignment > SwoopAlignmentThreshold
            ? acceleration * Time.fixedDeltaTime
            : -(acceleration * 3f) * Time.fixedDeltaTime;
        currentSpeed = Mathf.Clamp(currentSpeed, minMoveSpeed, moveSpeed);

        // Nimble when slow, near-unsteerable at top speed
        float speedPercent = (currentSpeed - minMoveSpeed) / (moveSpeed - minMoveSpeed);
        float currentRotationSpeed = Mathf.Lerp(rotationSpeed, CommittedTurnRate, speedPercent);

        baseRotation = Quaternion.Slerp(baseRotation, Quaternion.LookRotation(direction),
            Time.fixedDeltaTime * currentRotationSpeed);

        // Cosmetic bank + pitch wobble layered on top of the tracking rotation
        float turnAmount = Vector3.Dot(baseRight, direction);
        float wobble = Mathf.Sin(Time.time * pitchWobbleSpeed) * pitchWobbleAmount;
        Quaternion tiltRotation = Quaternion.Euler(wobble, 0f, -turnAmount * tiltAmount);

        Quaternion smoothedFinalRotation = Quaternion.Slerp(transform.rotation,
            baseRotation * tiltRotation, Time.fixedDeltaTime * 12f);
        rb.MoveRotation(smoothedFinalRotation);

        Vector3 targetVelocity = (smoothedFinalRotation * Vector3.forward) * currentSpeed;
        ApplyGroundAvoidance(ref targetVelocity);

        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * 10f);

        HandleMovementAudio();
    }

    /// <summary>
    /// A SphereCast (rather than a Raycast) acts like a cylinder, so the floor is detected even
    /// when only the edge of the model dips toward it.
    /// </summary>
    private void ApplyGroundAvoidance(ref Vector3 targetVelocity)
    {
        if (!Physics.SphereCast(transform.position, 0.5f, Vector3.down, out RaycastHit hit, hoverHeight * 1.5f))
            return;

        // Only push off static environment; a rigidbody hit is another enemy
        if (hit.rigidbody != null || hit.distance >= hoverHeight) return;

        targetVelocity.y = Mathf.Max(targetVelocity.y, 0f); // Erase downward intent
        targetVelocity.y += (hoverHeight - hit.distance) * 20f;

        // Kill existing downward momentum outright, or the enemy sinks into the floor
        // while the velocity Lerp is still catching up
        if (rb.linearVelocity.y < 0f)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
    }

    private void HandleMovementAudio()
    {
        moveSoundTimer -= Time.fixedDeltaTime;
        if (moveSoundTimer > 0f) return;

        if (AudioManager.Instance != null && AudioManager.Instance.enemyMoveSound != null)
        {
            moveAudioSource.clip = AudioManager.Instance.enemyMoveSound;
            moveAudioSource.volume = AudioManager.Instance.enemyMoveVolume;
            moveAudioSource.pitch = Random.Range(0.8f, 1.2f);
            moveAudioSource.Play();
        }
        moveSoundTimer = Random.Range(1f, 2.5f);
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player")
            && collision.gameObject.TryGetComponent(out PlayerHealth playerHealth))
            playerHealth.TakeDamage(collisionDamage);
    }

    public virtual void TakeDamage(int damage)
    {
        if (markedForBulletTimeDeath) return;

        currentHealth -= damage;

        if (currentHealth > 0)
        {
            // Only flash when the hit is survived — a dying enemy disappears anyway
            if (enemyRenderer != null && flashMaterial != null && gameObject.activeInHierarchy)
            {
                if (flashCoroutine != null) StopCoroutine(flashCoroutine);
                flashCoroutine = StartCoroutine(FlashRoutine());
            }
            return;
        }

        // During bullet time, lethal damage is banked rather than applied — the kill
        // resolves when bullet time ends (see KatanaWeapon.EndBulletTime)
        if (KatanaWeapon.IsBulletTimeActive)
        {
            markedForBulletTimeDeath = true;
            KatanaWeapon.RegisterBulletTimeDeath(gameObject);
            Mark(KatanaWeapon.BulletTimeMarkMaterial);
        }
        else
        {
            ForceDie();
        }
    }

    /// <summary>Kills the enemy immediately, counting toward the zone quota.</summary>
    public virtual void ForceDie()
    {
        EnemyEvents.ReportDeath();
        gameObject.SetActive(false);
    }

    private IEnumerator FlashRoutine()
    {
        enemyRenderer.sharedMaterial = flashMaterial;
        yield return new WaitForSeconds(flashDuration);
        if (enemyRenderer != null && originalMaterial != null)
            enemyRenderer.sharedMaterial = originalMaterial;
    }
}
