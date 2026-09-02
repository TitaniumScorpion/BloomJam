using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class DasherEnemy : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public int maxHealth = 3;
    public int dashDamage = 1;

    [Header("Wandering")]
    public float wanderSpeed = 3f;
    public float minWanderHeight = 2f;
    public float maxWanderHeight = 6f;

    [Header("Detection Zones")]
    [Tooltip("Inner radius — player entering this stops the dasher and starts the countdown")]
    public float detectRadius = 6f;
    [Tooltip("Outer radius — player must exit this within prepareDuration to cancel the dash")]
    public float escapeRadius = 10f;

    [Header("Dash")]
    public float prepareDuration = 1.5f;
    public float dashSpeed = 22f;
    public float dashDuration = 0.5f;

    [Header("Materials")]
    public Renderer enemyRenderer;
    public Material triggeredMaterial;  // shown while preparing/dashing
    public Material flashMaterial;      // brief flash on hit
    public float flashDuration = 0.1f;

    private enum DasherState { Wandering, Preparing, Dashing }
    private DasherState state;

    private int currentHealth;
    private Rigidbody rb;
    private Transform playerTransform;
    private Vector3 wanderTarget;
    private float prepareTimer;
    private Vector3 dashDirection;
    private float dashTimer;
    private BulletTimeFreeze bulletTimeFreeze;
    private Material originalMaterial;
    private Material currentBaseMaterial;
    private bool isFlashing;
    private Coroutine flashCoroutine;
    private bool markedForBulletTimeDeath;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.freezeRotation = true;

        if (enemyRenderer != null)
            originalMaterial = enemyRenderer.sharedMaterial;
        currentBaseMaterial = originalMaterial;
    }

    private void OnEnable()
    {
        currentHealth = maxHealth;
        state = DasherState.Wandering;
        bulletTimeFreeze.Reset();
        prepareTimer = 0f;
        dashTimer = 0f;
        wanderTarget = PickWanderTarget();
        transform.position = new Vector3(transform.position.x, wanderTarget.y, transform.position.z);

        isFlashing = false;
        currentBaseMaterial = originalMaterial;
        markedForBulletTimeDeath = false;
        if (enemyRenderer != null && originalMaterial != null)
            enemyRenderer.sharedMaterial = originalMaterial;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        QuotaManager.OnZoneCleared += Despawn;
        QuotaManager.OnGameCompleted += Despawn;
    }

    private void OnDisable()
    {
        rb.linearVelocity = Vector3.zero;
        QuotaManager.OnZoneCleared -= Despawn;
        QuotaManager.OnGameCompleted -= Despawn;
    }

    private void Despawn() => gameObject.SetActive(false);

    private void Update()
    {
        if (KatanaWeapon.IsBulletTimeActive) return;
        if (playerTransform == null) return;

        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        switch (state)
        {
            case DasherState.Wandering:
                if (distToPlayer <= detectRadius)
                {
                    state = DasherState.Preparing;
                    prepareTimer = 0f;
                    rb.linearVelocity = Vector3.zero;
                    SetBaseMaterial(triggeredMaterial);
                }
                break;

            case DasherState.Preparing:
                // Face the player
                Vector3 toPlayer = playerTransform.position - transform.position;
                toPlayer.y = 0f;
                if (toPlayer != Vector3.zero)
                    transform.rotation = Quaternion.Slerp(transform.rotation,
                        Quaternion.LookRotation(toPlayer), Time.deltaTime * 8f);

                // Player escaped the outer zone — cancel
                if (distToPlayer > escapeRadius)
                {
                    state = DasherState.Wandering;
                    wanderTarget = PickWanderTarget();
                    SetBaseMaterial(originalMaterial);
                    break;
                }

                prepareTimer += Time.deltaTime;
                if (prepareTimer >= prepareDuration)
                {
                    // Reuse the already-flattened facing vector so the dash stays level
                    // instead of climbing/diving toward the player's exact head height
                    dashDirection = toPlayer.normalized;
                    dashTimer = 0f;
                    state = DasherState.Dashing;
                    // stays on triggeredMaterial
                }
                break;

            case DasherState.Dashing:
                dashTimer += Time.deltaTime;
                if (dashTimer >= dashDuration)
                {
                    rb.linearVelocity = Vector3.zero;

                    if (distToPlayer <= escapeRadius)
                    {
                        // Player still nearby — prepare another dash
                        state = DasherState.Preparing;
                        prepareTimer = 0f;
                        // stays on triggeredMaterial
                    }
                    else
                    {
                        state = DasherState.Wandering;
                        wanderTarget = PickWanderTarget();
                        SetBaseMaterial(originalMaterial);
                    }
                }
                break;
        }
    }

    private void FixedUpdate()
    {
        if (bulletTimeFreeze.Tick(rb)) return;

        switch (state)
        {
            case DasherState.Wandering:
                Vector3 toTarget = wanderTarget - transform.position;
                if (toTarget.magnitude < 1f)
                    wanderTarget = PickWanderTarget();
                else
                    rb.linearVelocity = Vector3.Lerp(rb.linearVelocity,
                        toTarget.normalized * wanderSpeed, Time.fixedDeltaTime * 3f);
                break;

            case DasherState.Preparing:
                rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 10f);
                break;

            case DasherState.Dashing:
                rb.linearVelocity = dashDirection * dashSpeed;
                break;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (state != DasherState.Dashing) return;
        if (collision.gameObject.CompareTag("Player")
            && collision.gameObject.TryGetComponent(out PlayerHealth health))
            health.TakeDamage(dashDamage);
    }

    private void SetBaseMaterial(Material mat)
    {
        currentBaseMaterial = mat;
        if (!isFlashing && enemyRenderer != null && mat != null)
            enemyRenderer.sharedMaterial = mat;
    }

    private Vector3 PickWanderTarget()
    {
        Vector2 circle = Random.insideUnitCircle * EnemySpawner.CurrentSpawnRadius * 0.75f;
        float height = Random.Range(minWanderHeight, maxWanderHeight);
        return EnemySpawner.CurrentArenaCenter + new Vector3(circle.x, height, circle.y);
    }

    public void TakeDamage(int damage)
    {
        if (markedForBulletTimeDeath) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            if (KatanaWeapon.IsBulletTimeActive)
            {
                markedForBulletTimeDeath = true;
                KatanaWeapon.RegisterBulletTimeDeath(gameObject);
                SetBaseMaterial(KatanaWeapon.BulletTimeMarkMaterial);
            }
            else
            {
                ForceDie();
            }
        }
        else if (enemyRenderer != null && flashMaterial != null && gameObject.activeInHierarchy)
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashRoutine());
        }
    }

    public void ForceDie()
    {
        EnemyEvents.ReportDeath();
        gameObject.SetActive(false);
    }

    private IEnumerator FlashRoutine()
    {
        isFlashing = true;
        enemyRenderer.sharedMaterial = flashMaterial;
        yield return new WaitForSeconds(flashDuration);
        if (enemyRenderer != null)
            enemyRenderer.sharedMaterial = currentBaseMaterial;
        isFlashing = false;
    }
}
